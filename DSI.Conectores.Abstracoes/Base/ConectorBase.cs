using DSI.Conectores.Abstracoes.Enums;
using DSI.Conectores.Abstracoes.Interfaces;
using DSI.Conectores.Abstracoes.Modelos;
using System.Data;
using System.Diagnostics;

namespace DSI.Conectores.Abstracoes.Base;

/// <summary>
/// Classe base abstrata para facilitar implementação de conectores
/// </summary>
public abstract class ConectorBase : IConector
{
    public abstract string Nome { get; }
    public abstract CapacidadesConector Capacidades { get; }

    public abstract IDbConnection CriarConexao(string stringConexao);

    public virtual async Task<ResultadoTesteConexao> TestarConexaoAsync(string stringConexao)
    {
        var stopwatch = Stopwatch.StartNew();
        var resultado = new ResultadoTesteConexao();

        try
        {
            using var conexao = CriarConexao(stringConexao);
            await Task.Run(() => conexao.Open());

            resultado.Sucesso = true;
            resultado.Mensagem = "Conexão estabelecida com sucesso";
            resultado.NomeBancoDados = conexao.Database;
            resultado.VersaoServidor = ObterVersaoServidor(conexao);
            resultado.TempoRespostaMs = stopwatch.ElapsedMilliseconds;
        }
        catch (Exception ex)
        {
            resultado.Sucesso = false;
            resultado.Mensagem = "Falha ao conectar";
            resultado.DetalhesErro = ex.Message;
            resultado.TempoRespostaMs = stopwatch.ElapsedMilliseconds;
        }

        return resultado;
    }

    protected abstract string ObterVersaoServidor(IDbConnection conexao);

    public abstract Task<IEnumerable<InfoTabela>> DescobrirTabelasAsync(string stringConexao);
    public abstract Task<InfoTabela> DescobrirSchemaTabelaAsync(string stringConexao, string nomeTabela);

    public virtual async Task<IDataReader> ExecutarConsultaAsync(IDbConnection conexao, string sql, Dictionary<string, object>? parametros = null)
    {
        var comando = conexao.CreateCommand();
        comando.CommandText = sql;

        if (parametros != null)
        {
            foreach (var param in parametros)
            {
                var dbParam = comando.CreateParameter();
                dbParam.ParameterName = param.Key;
                dbParam.Value = param.Value ?? DBNull.Value;
                comando.Parameters.Add(dbParam);
            }
        }

        return await Task.Run(() => comando.ExecuteReader());
    }

    public virtual async Task<int> ExecutarComandoAsync(IDbConnection conexao, string sql, Dictionary<string, object>? parametros = null)
    {
        using var comando = conexao.CreateCommand();
        comando.CommandText = sql;

        if (parametros != null)
        {
            foreach (var param in parametros)
            {
                var dbParam = comando.CreateParameter();
                dbParam.ParameterName = param.Key;
                dbParam.Value = param.Value ?? DBNull.Value;
                comando.Parameters.Add(dbParam);
            }
        }

        return await Task.Run(() => comando.ExecuteNonQuery());
    }

    public abstract Task<int> InserirEmLoteAsync(IDbConnection conexao, string tabela, DataTable dados);
    public abstract string? ConstruirComandoUpsert(string tabela, IEnumerable<string> colunas, IEnumerable<string> colunasChave);

    public virtual void Dispose()
    {
        // Implementações específicas podem sobrescrever se necessário
    }
}
