using DSI.Conectores.Abstracoes.Base;
using DSI.Conectores.Abstracoes.Enums;
using DSI.Conectores.Abstracoes.Modelos;
using MySql.Data.MySqlClient;
using System.Data;
using System.Text;

namespace DSI.Conectores.MySql;

/// <summary>
/// Conector para MySQL/MariaDB
/// </summary>
public class ConectorMySql : ConectorBase
{
    public override string Nome => "MySQL/MariaDB";

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
        return new MySqlConnection(stringConexao);
    }

    protected override string ObterVersaoServidor(IDbConnection conexao)
    {
        if (conexao is MySqlConnection mysqlConn)
        {
            return mysqlConn.ServerVersion;
        }
        return "Desconhecida";
    }

    public override async Task<IEnumerable<InfoTabela>> DescobrirTabelasAsync(string stringConexao)
    {
        var tabelas = new List<InfoTabela>();

        using var conexao = new MySqlConnection(stringConexao);
        await conexao.OpenAsync();

        var sql = @"
            SELECT 
                TABLE_NAME,
                TABLE_SCHEMA,
                TABLE_TYPE,
                TABLE_ROWS
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = DATABASE()
            ORDER BY TABLE_NAME";

        using var comando = new MySqlCommand(sql, conexao);
        using var reader = await comando.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            tabelas.Add(new InfoTabela
            {
                Nome = reader.GetString("TABLE_NAME"),
                Schema = reader.GetString("TABLE_SCHEMA"),
                Tipo = reader.GetString("TABLE_TYPE"),
                QuantidadeLinhas = reader.IsDBNull(reader.GetOrdinal("TABLE_ROWS")) 
                    ? null 
                    : reader.GetInt64("TABLE_ROWS")
            });
        }

        return tabelas;
    }

    public override async Task<InfoTabela> DescobrirSchemaTabelaAsync(string stringConexao, string nomeTabela)
    {
        using var conexao = new MySqlConnection(stringConexao);
        await conexao.OpenAsync();

        var tabela = new InfoTabela { Nome = nomeTabela };

        var sql = @"
            SELECT 
                COLUMN_NAME,
                DATA_TYPE,
                IS_NULLABLE,
                COLUMN_KEY,
                EXTRA,
                CHARACTER_MAXIMUM_LENGTH,
                NUMERIC_PRECISION,
                NUMERIC_SCALE,
                COLUMN_DEFAULT
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = @tableName
            ORDER BY ORDINAL_POSITION";

        using var comando = new MySqlCommand(sql, conexao);
        comando.Parameters.AddWithValue("@tableName", nomeTabela);

        using var reader = await comando.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var coluna = new InfoColuna
            {
                Nome = reader.GetString("COLUMN_NAME"),
                TipoDados = reader.GetString("DATA_TYPE"),
                TipoNet = MapearTipoNet(reader.GetString("DATA_TYPE")),
                AceitaNulo = reader.GetString("IS_NULLABLE") == "YES",
                EhChavePrimaria = reader.GetString("COLUMN_KEY") == "PRI",
                EhIdentity = reader.GetString("EXTRA").Contains("auto_increment"),
                Tamanho = reader.IsDBNull(reader.GetOrdinal("CHARACTER_MAXIMUM_LENGTH")) 
                    ? null 
                    : Convert.ToInt32(reader.GetInt64("CHARACTER_MAXIMUM_LENGTH")),
                Precisao = reader.IsDBNull(reader.GetOrdinal("NUMERIC_PRECISION"))
                    ? null
                    : Convert.ToInt32(reader.GetInt64("NUMERIC_PRECISION")),
                Escala = reader.IsDBNull(reader.GetOrdinal("NUMERIC_SCALE"))
                    ? null
                    : Convert.ToInt32(reader.GetInt64("NUMERIC_SCALE")),
                ValorPadrao = reader.IsDBNull(reader.GetOrdinal("COLUMN_DEFAULT"))
                    ? null
                    : reader.GetString("COLUMN_DEFAULT")
            };

            tabela.Colunas.Add(coluna);
        }

        return tabela;
    }

    public override async Task<int> InserirEmLoteAsync(IDbConnection conexao, string tabela, DataTable dados)
    {
        if (conexao is not MySqlConnection mysqlConn)
            throw new InvalidOperationException("Conexão deve ser MySqlConnection");

        // MySQL não tem bulk insert nativo via ADO.NET como SQL Server
        // Vamos usar INSERT com múltiplos valores
        if (dados.Rows.Count == 0)
            return 0;

        var colunas = dados.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
        var sql = ConstruirInsertEmLote(tabela, colunas, dados.Rows.Count);

        using var comando = new MySqlCommand(sql, mysqlConn);
        
        int paramIndex = 0;
        foreach (DataRow row in dados.Rows)
        {
            foreach (var coluna in colunas)
            {
                comando.Parameters.AddWithValue($"@p{paramIndex++}", row[coluna] ?? DBNull.Value);
            }
        }

        return await comando.ExecuteNonQueryAsync();
    }

    private string ConstruirInsertEmLote(string tabela, List<string> colunas, int quantidadeLinhas)
    {
        var sb = new StringBuilder();
        sb.Append($"INSERT INTO `{tabela}` (");
        sb.Append(string.Join(", ", colunas.Select(c => $"`{c}`")));
        sb.Append(") VALUES ");

        var valores = new List<string>();
        int paramIndex = 0;
        for (int i = 0; i < quantidadeLinhas; i++)
        {
            var parametros = colunas.Select(_ => $"@p{paramIndex++}");
            valores.Add($"({string.Join(", ", parametros)})");
        }

        sb.Append(string.Join(", ", valores));
        return sb.ToString();
    }

    public override string? ConstruirComandoUpsert(string tabela, IEnumerable<string> colunas, IEnumerable<string> colunasChave)
    {
        var listaColunas = colunas.ToList();
        var listaChaves = colunasChave.ToList();

        if (!listaColunas.Any() || !listaChaves.Any())
            return null;

        // MySQL usa INSERT ... ON DUPLICATE KEY UPDATE
        var sb = new StringBuilder();
        sb.Append($"INSERT INTO `{tabela}` (");
        sb.Append(string.Join(", ", listaColunas.Select(c => $"`{c}`")));
        sb.Append(") VALUES (");
        sb.Append(string.Join(", ", listaColunas.Select(c => $"@{c}")));
        sb.Append(") ON DUPLICATE KEY UPDATE ");

        var updates = listaColunas.Where(c => !listaChaves.Contains(c))
            .Select(c => $"`{c}` = VALUES(`{c}`)");

        sb.Append(string.Join(", ", updates));

        return sb.ToString();
    }

    private string MapearTipoNet(string tipoMySql)
    {
        return tipoMySql.ToLower() switch
        {
            "int" or "integer" or "mediumint" => "System.Int32",
            "bigint" => "System.Int64",
            "smallint" => "System.Int16",
            "tinyint" => "System.Byte",
            "decimal" or "numeric" => "System.Decimal",
            "float" => "System.Single",
            "double" => "System.Double",
            "bit" or "boolean" or "bool" => "System.Boolean",
            "char" or "varchar" or "text" or "mediumtext" or "longtext" => "System.String",
            "date" or "datetime" or "timestamp" => "System.DateTime",
            "time" => "System.TimeSpan",
            "blob" or "mediumblob" or "longblob" or "binary" or "varbinary" => "System.Byte[]",
            _ => "System.Object"
        };
    }
}
