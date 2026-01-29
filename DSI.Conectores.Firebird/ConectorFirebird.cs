using DSI.Conectores.Abstracoes.Base;
using DSI.Conectores.Abstracoes.Enums;
using DSI.Conectores.Abstracoes.Modelos;
using FirebirdSql.Data.FirebirdClient;
using System.Data;
using System.Text;

namespace DSI.Conectores.Firebird;

/// <summary>
/// Conector para Firebird
/// </summary>
public class ConectorFirebird : ConectorBase
{
    public override string Nome => "Firebird";

    public override CapacidadesConector Capacidades =>
        CapacidadesConector.Transacoes |
        CapacidadesConector.Streaming |
        CapacidadesConector.DescobertaSchema |
        CapacidadesConector.ConsultasParametrizadas |
        CapacidadesConector.MultipleResultSets;
        // Firebird não tem UPSERT nativo nem BulkInsert otimizado

    public override IDbConnection CriarConexao(string stringConexao)
    {
        return new FbConnection(stringConexao);
    }

    protected override string ObterVersaoServidor(IDbConnection conexao)
    {
        if (conexao is FbConnection fbConn)
        {
            return fbConn.ServerVersion;
        }
        return "Desconhecida";
    }

    public override async Task<IEnumerable<InfoTabela>> DescobrirTabelasAsync(string stringConexao)
    {
        var tabelas = new List<InfoTabela>();

        using var conexao = new FbConnection(stringConexao);
        await conexao.OpenAsync();

        var sql = @"
            SELECT 
                RDB$RELATION_NAME as TABLE_NAME,
                RDB$RELATION_TYPE as TABLE_TYPE
            FROM RDB$RELATIONS
            WHERE RDB$SYSTEM_FLAG = 0
              AND RDB$VIEW_BLR IS NULL
            ORDER BY RDB$RELATION_NAME";

        using var comando = new FbCommand(sql, conexao);
        using var reader = await comando.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var nomeTabela = reader.GetString(0).Trim();
            
            tabelas.Add(new InfoTabela
            {
                Nome = nomeTabela,
                Tipo = "TABLE"
            });
        }

        return tabelas;
    }

    public override async Task<InfoTabela> DescobrirSchemaTabelaAsync(string stringConexao, string nomeTabela)
    {
        using var conexao = new FbConnection(stringConexao);
        await conexao.OpenAsync();

        var tabela = new InfoTabela { Nome = nomeTabela };

        var sql = @"
            SELECT 
                rf.RDB$FIELD_NAME as COLUMN_NAME,
                f.RDB$FIELD_TYPE as FIELD_TYPE,
                f.RDB$FIELD_SUB_TYPE as FIELD_SUBTYPE,
                f.RDB$FIELD_LENGTH as FIELD_LENGTH,
                f.RDB$FIELD_PRECISION as FIELD_PRECISION,
                f.RDB$FIELD_SCALE as FIELD_SCALE,
                rf.RDB$NULL_FLAG as NULL_FLAG,
                rf.RDB$DEFAULT_VALUE as DEFAULT_VALUE
            FROM RDB$RELATION_FIELDS rf
            JOIN RDB$FIELDS f ON rf.RDB$FIELD_SOURCE = f.RDB$FIELD_NAME
            WHERE rf.RDB$RELATION_NAME = @tableName
            ORDER BY rf.RDB$FIELD_POSITION";

        using var comando = new FbCommand(sql, conexao);
        comando.Parameters.AddWithValue("@tableName", nomeTabela.ToUpper());

        using var reader = await comando.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var nomeColuna = reader.GetString(0).Trim();
            var tipoFirebird = reader.GetInt16(1);
            var subTipo = reader.IsDBNull(2) ? (short)0 : reader.GetInt16(2);
            var tamanho = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3);
            var precisao = reader.IsDBNull(4) ? (int?)null : reader.GetInt16(4);
            var escala = reader.IsDBNull(5) ? (int?)null : reader.GetInt16(5);
            var nullFlag = reader.IsDBNull(6) ? 0 : reader.GetInt16(6);

            var coluna = new InfoColuna
            {
                Nome = nomeColuna,
                TipoDados = ObterNomeTipoFirebird(tipoFirebird, subTipo),
                TipoNet = MapearTipoNet(tipoFirebird, subTipo),
                AceitaNulo = nullFlag == 0,
                Tamanho = tamanho,
                Precisao = precisao,
                Escala = escala.HasValue ? Math.Abs(escala.Value) : null,
                ValorPadrao = reader.IsDBNull(7) ? null : reader.GetString(7)
            };

            tabela.Colunas.Add(coluna);
        }

        // Descobre chaves primárias
        await DescobrirChavesPrimariasAsync(conexao, nomeTabela, tabela);

        return tabela;
    }

    private async Task DescobrirChavesPrimariasAsync(FbConnection conexao, string nomeTabela, InfoTabela tabela)
    {
        var sqlPk = @"
            SELECT 
                s.RDB$FIELD_NAME as COLUMN_NAME
            FROM RDB$RELATION_CONSTRAINTS rc
            JOIN RDB$INDEX_SEGMENTS s ON rc.RDB$INDEX_NAME = s.RDB$INDEX_NAME
            WHERE rc.RDB$RELATION_NAME = @tableName
              AND rc.RDB$CONSTRAINT_TYPE = 'PRIMARY KEY'";

        using var cmdPk = new FbCommand(sqlPk, conexao);
        cmdPk.Parameters.AddWithValue("@tableName", nomeTabela.ToUpper());

        using var readerPk = await cmdPk.ExecuteReaderAsync();
        var chavesPrimarias = new HashSet<string>();

        while (await readerPk.ReadAsync())
        {
            chavesPrimarias.Add(readerPk.GetString(0).Trim());
        }

        foreach (var coluna in tabela.Colunas)
        {
            coluna.EhChavePrimaria = chavesPrimarias.Contains(coluna.Nome);
        }
    }

    public override async Task<int> InserirEmLoteAsync(IDbConnection conexao, string tabela, DataTable dados)
    {
        if (conexao is not FbConnection fbConn)
            throw new InvalidOperationException("Conexão deve ser FbConnection");

        if (dados.Rows.Count == 0)
            return 0;

        var colunas = dados.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
        var sql = ConstruirInsert(tabela, colunas);

        using var transacao = await fbConn.BeginTransactionAsync();
        try
        {
            int totalInserido = 0;
            using var comando = new FbCommand(sql, fbConn, transacao);

            // Prepara parâmetros
            foreach (var coluna in colunas)
            {
                comando.Parameters.Add($"@{coluna}", FbDbType.VarChar);
            }

            foreach (DataRow row in dados.Rows)
            {
                for (int i = 0; i < colunas.Count; i++)
                {
                    comando.Parameters[i].Value = row[colunas[i]] ?? DBNull.Value;
                }

                totalInserido += await comando.ExecuteNonQueryAsync();
            }

            await transacao.CommitAsync();
            return totalInserido;
        }
        catch
        {
            await transacao.RollbackAsync();
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
        // Firebird não tem UPSERT nativo
        // Seria necessário usar UPDATE OR INSERT, mas requer sintaxe específica
        return null;
    }

    private string ObterNomeTipoFirebird(short tipo, short subTipo)
    {
        return tipo switch
        {
            7 => subTipo == 1 ? "NUMERIC" : subTipo == 2 ? "DECIMAL" : "SMALLINT",
            8 => subTipo == 1 ? "NUMERIC" : subTipo == 2 ? "DECIMAL" : "INTEGER",
            10 => "FLOAT",
            12 => "DATE",
            13 => "TIME",
            14 => "CHAR",
            16 => subTipo == 1 ? "NUMERIC" : subTipo == 2 ? "DECIMAL" : "BIGINT",
            27 => "DOUBLE PRECISION",
            35 => "TIMESTAMP",
            37 => "VARCHAR",
            261 => subTipo == 0 ? "BLOB" : "TEXT",
            _ => "UNKNOWN"
        };
    }

    private string MapearTipoNet(short tipo, short subTipo)
    {
        return tipo switch
        {
            7 => subTipo == 1 || subTipo == 2 ? "System.Decimal" : "System.Int16",
            8 => subTipo == 1 || subTipo == 2 ? "System.Decimal" : "System.Int32",
            10 => "System.Single",
            12 => "System.DateTime",
            13 => "System.TimeSpan",
            14 => "System.String",
            16 => subTipo == 1 || subTipo == 2 ? "System.Decimal" : "System.Int64",
            27 => "System.Double",
            35 => "System.DateTime",
            37 => "System.String",
            261 => subTipo == 0 ? "System.Byte[]" : "System.String",
            _ => "System.Object"
        };
    }
}
