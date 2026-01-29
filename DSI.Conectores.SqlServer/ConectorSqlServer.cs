using DSI.Conectores.Abstracoes.Base;
using DSI.Conectores.Abstracoes.Enums;
using DSI.Conectores.Abstracoes.Modelos;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;

namespace DSI.Conectores.SqlServer;

/// <summary>
/// Conector para Microsoft SQL Server
/// </summary>
public class ConectorSqlServer : ConectorBase
{
    public override string Nome => "SQL Server";

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
        return new SqlConnection(stringConexao);
    }

    protected override string ObterVersaoServidor(IDbConnection conexao)
    {
        if (conexao is SqlConnection sqlConn)
        {
            return sqlConn.ServerVersion;
        }
        return "Desconhecida";
    }

    public override async Task<IEnumerable<InfoTabela>> DescobrirTabelasAsync(string stringConexao)
    {
        var tabelas = new List<InfoTabela>();

        using var conexao = new SqlConnection(stringConexao);
        await conexao.OpenAsync();

        var sql = @"
            SELECT 
                t.TABLE_NAME,
                t.TABLE_SCHEMA,
                t.TABLE_TYPE,
                p.rows as row_count
            FROM INFORMATION_SCHEMA.TABLES t
            LEFT JOIN sys.tables st ON t.TABLE_NAME = st.name
            LEFT JOIN sys.partitions p ON st.object_id = p.object_id
            WHERE t.TABLE_SCHEMA != 'sys' 
              AND p.index_id IN (0,1)
            ORDER BY t.TABLE_SCHEMA, t.TABLE_NAME";

        using var comando = new SqlCommand(sql, conexao);
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
        using var conexao = new SqlConnection(stringConexao);
        await conexao.OpenAsync();

        var tabela = new InfoTabela { Nome = nomeTabela };

        var sql = @"
            SELECT 
                c.COLUMN_NAME,
                c.DATA_TYPE,
                c.IS_NULLABLE,
                c.COLUMN_DEFAULT,
                c.CHARACTER_MAXIMUM_LENGTH,
                c.NUMERIC_PRECISION,
                c.NUMERIC_SCALE,
                CASE 
                    WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 
                    ELSE 0 
                END as IS_PRIMARY_KEY,
                COLUMNPROPERTY(OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') as IS_IDENTITY
            FROM INFORMATION_SCHEMA.COLUMNS c
            LEFT JOIN (
                SELECT ku.COLUMN_NAME
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku 
                    ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                    AND tc.TABLE_NAME = @tableName
            ) pk ON c.COLUMN_NAME = pk.COLUMN_NAME
            WHERE c.TABLE_NAME = @tableName
            ORDER BY c.ORDINAL_POSITION";

        using var comando = new SqlCommand(sql, conexao);
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
                Precisao = reader.IsDBNull(5) ? null : Convert.ToInt32(reader.GetByte(5)),
                Escala = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                EhChavePrimaria = reader.GetInt32(7) == 1,
                EhIdentity = reader.GetInt32(8) == 1
            };

            tabela.Colunas.Add(coluna);
        }

        return tabela;
    }

    public override async Task<int> InserirEmLoteAsync(IDbConnection conexao, string tabela, DataTable dados)
    {
        if (conexao is not SqlConnection sqlConn)
            throw new InvalidOperationException("Conexão deve ser SqlConnection");

        if (dados.Rows.Count == 0)
            return 0;

        // SQL Server tem SqlBulkCopy otimizado
        using var bulkCopy = new SqlBulkCopy(sqlConn);
        bulkCopy.DestinationTableName = tabela;
        bulkCopy.BatchSize = 1000;
        bulkCopy.BulkCopyTimeout = 300; // 5 minutos

        // Mapeia colunas
        foreach (DataColumn coluna in dados.Columns)
        {
            bulkCopy.ColumnMappings.Add(coluna.ColumnName, coluna.ColumnName);
        }

        await bulkCopy.WriteToServerAsync(dados);
        return dados.Rows.Count;
    }

    public override string? ConstruirComandoUpsert(string tabela, IEnumerable<string> colunas, IEnumerable<string> colunasChave)
    {
        var listaColunas = colunas.ToList();
        var listaChaves = colunasChave.ToList();

        if (!listaColunas.Any() || !listaChaves.Any())
            return null;

        // SQL Server usa MERGE
        var sb = new StringBuilder();
        
        // MERGE statement
        sb.AppendLine($"MERGE INTO [{tabela}] AS target");
        sb.AppendLine("USING (VALUES (");
        sb.Append("  ");
        sb.Append(string.Join(", ", listaColunas.Select(c => $"@{c}")));
        sb.AppendLine(")) AS source (");
        sb.Append("  ");
        sb.Append(string.Join(", ", listaColunas.Select(c => $"[{c}]")));
        sb.AppendLine(")");
        
        // ON clause (chaves primárias)
        sb.Append("ON ");
        var onConditions = listaChaves.Select(k => $"target.[{k}] = source.[{k}]");
        sb.AppendLine(string.Join(" AND ", onConditions));
        
        // WHEN MATCHED (UPDATE)
        var colunasParaAtualizar = listaColunas.Where(c => !listaChaves.Contains(c)).ToList();
        if (colunasParaAtualizar.Any())
        {
            sb.AppendLine("WHEN MATCHED THEN");
            sb.Append("  UPDATE SET ");
            var updates = colunasParaAtualizar.Select(c => $"target.[{c}] = source.[{c}]");
            sb.AppendLine(string.Join(", ", updates));
        }
        
        // WHEN NOT MATCHED (INSERT)
        sb.AppendLine("WHEN NOT MATCHED THEN");
        sb.Append("  INSERT (");
        sb.Append(string.Join(", ", listaColunas.Select(c => $"[{c}]")));
        sb.AppendLine(")");
        sb.Append("  VALUES (");
        sb.Append(string.Join(", ", listaColunas.Select(c => $"source.[{c}]")));
        sb.AppendLine(");");

        return sb.ToString();
    }

    private string MapearTipoNet(string tipoSqlServer)
    {
        return tipoSqlServer.ToLower() switch
        {
            "tinyint" => "System.Byte",
            "smallint" => "System.Int16",
            "int" => "System.Int32",
            "bigint" => "System.Int64",
            "decimal" or "numeric" or "money" or "smallmoney" => "System.Decimal",
            "float" => "System.Double",
            "real" => "System.Single",
            "bit" => "System.Boolean",
            "char" or "varchar" or "text" or "nchar" or "nvarchar" or "ntext" => "System.String",
            "date" or "datetime" or "datetime2" or "smalldatetime" => "System.DateTime",
            "datetimeoffset" => "System.DateTimeOffset",
            "time" => "System.TimeSpan",
            "binary" or "varbinary" or "image" or "timestamp" or "rowversion" => "System.Byte[]",
            "uniqueidentifier" => "System.Guid",
            "xml" => "System.String",
            "sql_variant" => "System.Object",
            _ => "System.Object"
        };
    }
}
