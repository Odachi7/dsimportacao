using DSI.Dominio.Entidades;

namespace DSI.Persistencia.Repositorios;

/// <summary>
/// Repositório específico para Execuções
/// </summary>
public interface IExecucaoRepositorio : IRepositorioBase<Execucao>
{
    /// <summary>
    /// Obtém execução completa com estatísticas e erros
    /// </summary>
    Task<Execucao?> ObterComDadosAsync(Guid id);

    /// <summary>
    /// Obtém histórico de execuções de um job
    /// </summary>
    Task<IEnumerable<Execucao>> ObterHistoricoPorJobAsync(Guid jobId, int quantidade = 50);

    /// <summary>
    /// Obtém última execução de um job
    /// </summary>
    Task<Execucao?> ObterUltimaExecucaoAsync(Guid jobId);

    /// <summary>
    /// Obtém execuções em andamento
    /// </summary>
    Task<IEnumerable<Execucao>> ObterEmAndamentoAsync();
}
