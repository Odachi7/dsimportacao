using DSI.Conectores.Abstracoes.Base;
using DSI.Conectores.Abstracoes.Enums;
using DSI.Conectores.Abstracoes.Modelos;
using Npgsql;
using System.Data;
using System.Text;

namespace DSI.Conectores.PostgreSql;

/// <summary>
/// Conector para PostgreSQL
/// </summary>
public class ConectorPostgreSql : ConectorBase
{
    public override string Nome => "PostgreSQL";

    public override CapacidadesConector Capacidades =>
        CapacidadesConector.Transacoes |
        CapacidadesConector.UpsertNativo |
        CapacidadesConector.BulkInsert |
        CapacidadesConector.Streaming |
        CapacidadesConector.DescobertaSchema |
        CapacidadesConector.ConsultasParametrizadas |
        CapacidadesConector.MultipleResultSets;

    public override IDbConnection CriarConexao(string stringConexao)
    {
        return new NpgsqlConnection(stringConexao);
    }

    protected override string ObterVersaoServidor(IDbConnection conexao)
    {
        if (conexao is NpgsqlConnection pgConn)
        {
            return pgConn.ServerVersion;
        }
        return "Desconhecida";
    }

    public override async Task<IEnumerable<InfoTabela>> DescobrirTabelasAsync(string stringConexao)
    {
        var tabelas = new List<InfoTabela>();

        using var conexao = new NpgsqlConnection(stringConexao);
        await conexao.OpenAsync();

        var sql = @"
            SELECT 
                table_name,
                table_schema,
                table_type,
                (xpath('/row/cnt/text()', 
                    query_to_xml(format('select count(*) as cnt from %I.%I', table_schema, table_name), 
                    false, true, '')))[1]::text::bigint as row_count
            FROM information_schema.tables
            WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
            ORDER BY table_schema, table_name";

        using var comando = new NpgsqlCommand(sql, conexao);
        using var reader = await comando.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            tabelas.Add(new InfoTabela
            {
                Nome = reader.GetString(0),
                Schema = reader.GetString(1),
                Tipo = reader.GetString(2),
                QuantidadeLinhas = reader.IsDBNull(3) ? null : reader.GetInt64(3)
            });
        }

        return tabelas;
    }

    public override async Task<InfoTabela> DescobrirSchemaTabelaAsync(string stringConexao, string nomeTabela)
    {
        using var conexao = new NpgsqlConnection(stringConexao);
        await conexao.OpenAsync();

        var tabela = new InfoTabela { Nome = nomeTabela };

        // Descobre colunas
        var sql = @"
            SELECT 
                c.column_name,
                c.data_type,
                c.is_nullable,
                c.column_default,
                c.character_maximum_length,
                c.numeric_precision,
                c.numeric_scale,
                CASE WHEN pk.column_name IS NOT NULL THEN true ELSE false END as is_primary_key,
                CASE WHEN c.column_default LIKE 'nextval%' THEN true ELSE false END as is_identity
            FROM information_schema.columns c
            LEFT JOIN (
                SELECT ku.column_name
                FROM information_schema.table_constraints tc
                JOIN information_schema.key_column_usage ku 
                    ON tc.constraint_name = ku.constraint_name
                WHERE tc.constraint_type = 'PRIMARY KEY'
                    AND tc.table_name = @tableName
            ) pk ON c.column_name = pk.column_name
            WHERE c.table_name = @tableName
            ORDER BY c.ordinal_position";

        using var comando = new NpgsqlCommand(sql, conexao);
        comando.Parameters.AddWithValue("@tableName", nomeTabela);

        using var reader = await comando.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var coluna = new InfoColuna
            {
                Nome = reader.GetString(0),
                TipoDados = reader.GetString(1),
                TipoNet = MapearTipoNet(reader.GetString(1)),
                AceitaNulo = reader.GetString(2) == "YES",
                ValorPadrao = reader.IsDBNull(3) ? null : reader.GetString(3),
                Tamanho = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                Precisao = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                Escala = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                EhChavePrimaria = reader.GetBoolean(7),
                EhIdentity = reader.GetBoolean(8)
            };

            tabela.Colunas.Add(coluna);
        }

        return tabela;
    }

    public override async Task<int> InserirEmLoteAsync(IDbConnection conexao, string tabela, DataTable dados)
    {
        if (conexao is not NpgsqlConnection pgConn)
            throw new InvalidOperationException("Conexão deve ser NpgsqlConnection");

        if (dados.Rows.Count == 0)
            return 0;

        var colunas = dados.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
        
        // PostgreSQL suporta COPY para inserção ultra-rápida
        var copyCommand = $"COPY {tabela} ({string.Join(", ", colunas.Select(c => $"\"{c}\""))}) FROM STDIN (FORMAT BINARY)";

        using var writer = await pgConn.BeginBinaryImportAsync(copyCommand);
        
        foreach (DataRow row in dados.Rows)
        {
            await writer.StartRowAsync();
            
            foreach (var coluna in colunas)
            {
                var valor = row[coluna];
                
                if (valor == DBNull.Value || valor == null)
                {
                    await writer.WriteNullAsync();
                }
                else
                {
                    await writer.WriteAsync(valor);
                }
            }
        }

        await writer.CompleteAsync();
        return dados.Rows.Count;
    }

    public override string? ConstruirComandoUpsert(string tabela, IEnumerable<string> colunas, IEnumerable<string> colunasChave)
    {
        var listaColunas = colunas.ToList();
        var listaChaves = colunasChave.ToList();

        if (!listaColunas.Any() || !listaChaves.Any())
            return null;

        // PostgreSQL usa INSERT ... ON CONFLICT DO UPDATE
        var sb = new StringBuilder();
        sb.Append($"INSERT INTO \"{tabela}\" (");
        sb.Append(string.Join(", ", listaColunas.Select(c => $"\"{c}\"")));
        sb.Append(") VALUES (");
        sb.Append(string.Join(", ", listaColunas.Select(c => $"@{c}")));
        sb.Append(") ON CONFLICT (");
        sb.Append(string.Join(", ", listaChaves.Select(c => $"\"{c}\"")));
        sb.Append(") DO UPDATE SET ");

        var updates = listaColunas.Where(c => !listaChaves.Contains(c))
            .Select(c => $"\"{c}\" = EXCLUDED.\"{c}\"");

        sb.Append(string.Join(", ", updates));

        return sb.ToString();
    }

    private string MapearTipoNet(string tipoPostgres)
    {
        return tipoPostgres.ToLower() switch
        {
            "smallint" or "int2" => "System.Int16",
            "integer" or "int" or "int4" => "System.Int32",
            "bigint" or "int8" => "System.Int64",
            "decimal" or "numeric" => "System.Decimal",
            "real" or "float4" => "System.Single",
            "double precision" or "float8" => "System.Double",
            "money" => "System.Decimal",
            "boolean" or "bool" => "System.Boolean",
            "character" or "char" or "character varying" or "varchar" or "text" => "System.String",
            "date" => "System.DateTime",
            "timestamp" or "timestamp without time zone" or "timestamp with time zone" or "timestamptz" => "System.DateTime",
            "time" or "time without time zone" or "time with time zone" or "timetz" => "System.TimeSpan",
            "interval" => "System.TimeSpan",
            "uuid" => "System.Guid",
            "bytea" => "System.Byte[]",
            "json" or "jsonb" => "System.String",
            "xml" => "System.String",
            "array" => "System.Array",
            _ => "System.Object"
        };
    }
}
