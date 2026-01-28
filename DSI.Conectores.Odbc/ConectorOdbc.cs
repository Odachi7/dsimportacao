using DSI.Conectores.Abstracoes.Base;
using DSI.Conectores.Abstracoes.Enums;
using DSI.Conectores.Abstracoes.Modelos;
using System.Data;
using System.Data.Odbc;
using System.Text;

namespace DSI.Conectores.Odbc;

/// <summary>
/// Conector universal via ODBC - suporta qualquer banco com driver ODBC
/// </summary>
public class ConectorOdbc : ConectorBase
{
    public override string Nome => "ODBC Universal";

    public override CapacidadesConector Capacidades =>
        CapacidadesConector.Transacoes |
        CapacidadesConector.Streaming |
        CapacidadesConector.DescobertaSchema |
        CapacidadesConector.ConsultasParametrizadas;
        // UPSERT e BulkInsert dependem do driver específico, não incluídos no universal

    public override IDbConnection CriarConexao(string stringConexao)
    {
        return new OdbcConnection(stringConexao);
    }

    protected override string ObterVersaoServidor(IDbConnection conexao)
    {
        if (conexao is OdbcConnection odbcConn)
        {
            try
            {
                return odbcConn.ServerVersion;
            }
            catch
            {
                return "Versão não disponível via ODBC";
            }
        }
        return "Desconhecida";
    }

    public override async Task<IEnumerable<InfoTabela>> DescobrirTabelasAsync(string stringConexao)
    {
        var tabelas = new List<InfoTabela>();

        using var conexao = new OdbcConnection(stringConexao);
        await conexao.OpenAsync();

        // Usa metadata do ODBC para descobrir tabelas
        var schema = await Task.Run(() => conexao.GetSchema("Tables"));

        foreach (DataRow row in schema.Rows)
        {
            var tipoTabela = row["TABLE_TYPE"]?.ToString() ?? "";
            
            // Filtra apenas tabelas e views
            if (tipoTabela == "TABLE" || tipoTabela == "VIEW")
            {
                tabelas.Add(new InfoTabela
                {
                    Nome = row["TABLE_NAME"]?.ToString() ?? "",
                    Schema = row["TABLE_SCHEM"]?.ToString(),
                    Tipo = tipoTabela
                });
            }
        }

        return tabelas;
    }

    public override async Task<InfoTabela> DescobrirSchemaTabelaAsync(string stringConexao, string nomeTabela)
    {
        using var conexao = new OdbcConnection(stringConexao);
        await conexao.OpenAsync();

        var tabela = new InfoTabela { Nome = nomeTabela };

        // Usa metadata do ODBC para descobrir colunas
        var restrictions = new string?[] { null, null, nomeTabela, null };
        var schema = await Task.Run(() => conexao.GetSchema("Columns", restrictions));

        foreach (DataRow row in schema.Rows)
        {
            var coluna = new InfoColuna
            {
                Nome = row["COLUMN_NAME"]?.ToString() ?? "",
                TipoDados = row["TYPE_NAME"]?.ToString() ?? "",
                TipoNet = MapearTipoNet(row["DATA_TYPE"]?.ToString() ?? ""),
                AceitaNulo = row["IS_NULLABLE"]?.ToString() == "YES",
                Tamanho = row["COLUMN_SIZE"] != DBNull.Value 
                    ? Convert.ToInt32(row["COLUMN_SIZE"]) 
                    : null,
                Precisao = row["DECIMAL_DIGITS"] != DBNull.Value 
                    ? Convert.ToInt32(row["DECIMAL_DIGITS"]) 
                    : null,
                ValorPadrao = row["COLUMN_DEF"]?.ToString()
            };

            tabela.Colunas.Add(coluna);
        }

        // Descobre chaves primárias
        var pkRestrictions = new string?[] { null, null, nomeTabela };
        var pkSchema = await Task.Run(() => conexao.GetSchema("PrimaryKeys", pkRestrictions));

        var chavesPrimarias = new HashSet<string>();
        foreach (DataRow row in pkSchema.Rows)
        {
            chavesPrimarias.Add(row["COLUMN_NAME"]?.ToString() ?? "");
        }

        foreach (var coluna in tabela.Colunas)
        {
            coluna.EhChavePrimaria = chavesPrimarias.Contains(coluna.Nome);
        }

        return tabela;
    }

    public override async Task<int> InserirEmLoteAsync(IDbConnection conexao, string tabela, DataTable dados)
    {
        // ODBC genérico não tem bulk insert otimizado
        // Usa INSERT individual em transação
        if (conexao is not OdbcConnection odbcConn)
        {
            throw new InvalidOperationException("Conexão deve ser OdbcConnection");
        }

        if (dados.Rows.Count == 0)
            return 0;

        var colunas = dados.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
        var sql = ConstruirInsert(tabela, colunas);

        using var transacao = odbcConn.BeginTransaction();
        try
        {
            int totalInserido = 0;
            using var comando = new OdbcCommand(sql, odbcConn, transacao);

            // Prepara parâmetros
            foreach (var coluna in colunas)
            {
                comando.Parameters.Add($"@{coluna}", OdbcType.VarChar);
            }

            foreach (DataRow row in dados.Rows)
            {
                for (int i = 0; i < colunas.Count; i++)
                {
                    comando.Parameters[i].Value = row[colunas[i]] ?? DBNull.Value;
                }

                totalInserido += await comando.ExecuteNonQueryAsync();
            }

            transacao.Commit();
            return totalInserido;
        }
        catch
        {
            transacao.Rollback();
            throw;
        }
    }

    private string ConstruirInsert(string tabela, List<string> colunas)
    {
        var sb = new StringBuilder();
        sb.Append($"INSERT INTO {tabela} (");
        sb.Append(string.Join(", ", colunas));
        sb.Append(") VALUES (");
        sb.Append(string.Join(", ", colunas.Select(c => $"@{c}")));
        sb.Append(")");
        return sb.ToString();
    }

    public override string? ConstruirComandoUpsert(string tabela, IEnumerable<string> colunas, IEnumerable<string> colunasChave)
    {
        // UPSERT não é padronizado em ODBC - cada banco tem sua sintaxe
        // Retorna null indicando que não é suportado universalmente
        return null;
    }

    private string MapearTipoNet(string tipoOdbc)
    {
        // Tipos ODBC SQL_ constants
        return tipoOdbc switch
        {
            "4" or "-5" => "System.Int32", // SQL_INTEGER, SQL_BIGINT
            "5" => "System.Int16", // SQL_SMALLINT
            "-6" => "System.Byte", // SQL_TINYINT
            "2" or "3" => "System.Decimal", // SQL_NUMERIC, SQL_DECIMAL
            "6" or "7" or "8" => "System.Double", // SQL_FLOAT, SQL_REAL, SQL_DOUBLE
            "-7" => "System.Boolean", // SQL_BIT
            "1" or "12" or "-1" or "-8" or "-9" => "System.String", // SQL_CHAR, SQL_VARCHAR, SQL_LONGVARCHAR, SQL_WCHAR, SQL_WVARCHAR
            "9" or "10" or "11" or "93" => "System.DateTime", // SQL_TYPE_DATE, SQL_TYPE_TIME, SQL_TYPE_TIMESTAMP
            "-2" or "-3" or "-4" => "System.Byte[]", // SQL_BINARY, SQL_VARBINARY, SQL_LONGVARBINARY
            _ => "System.Object"
        };
    }
}
