using DSI.Conectores.Abstracoes.Enums;
using DSI.Conectores.Abstracoes.Modelos;
using System.Data;

namespace DSI.Conectores.Abstracoes.Interfaces;

/// <summary>
/// Interface base para todos os conectores de banco de dados
/// </summary>
public interface IConector : IDisposable
{
    /// <summary>
    /// Nome do conector
    /// </summary>
    string Nome { get; }

    /// <summary>
    /// Capacidades suportadas por este conector
    /// </summary>
    CapacidadesConector Capacidades { get; }

    /// <summary>
    /// Testa a conexão com o banco de dados
    /// </summary>
    Task<ResultadoTesteConexao> TestarConexaoAsync(string stringConexao);

    /// <summary>
    /// Descobre todas as tabelas no banco de dados
    /// </summary>
    Task<IEnumerable<InfoTabela>> DescobrirTabelasAsync(string stringConexao);

    /// <summary>
    /// Descobre schema detalhado de uma tabela específica
    /// </summary>
    Task<InfoTabela> DescobrirSchemaTabelaAsync(string stringConexao, string nomeTabela);

    /// <summary>
    /// Cria uma conexão ADO.NET para o banco
    /// </summary>
    IDbConnection CriarConexao(string stringConexao);

    /// <summary>
    /// Executa uma consulta e retorna um DataReader para streaming
    /// </summary>
    Task<IDataReader> ExecutarConsultaAsync(IDbConnection conexao, string sql, Dictionary<string, object>? parametros = null);

    /// <summary>
    /// Executa um comando (INSERT, UPDATE, DELETE)
    /// </summary>
    Task<int> ExecutarComandoAsync(IDbConnection conexao, string sql, Dictionary<string, object>? parametros = null);

    /// <summary>
    /// Insere múltiplas linhas de forma otimizada (bulk insert se suportado)
    /// </summary>
    Task<int> InserirEmLoteAsync(IDbConnection conexao, string tabela, DataTable dados);

    /// <summary>
    /// Constrói comando UPSERT nativo se suportado
    /// </summary>
    string? ConstruirComandoUpsert(string tabela, IEnumerable<string> colunas, IEnumerable<string> colunasChave);
}
