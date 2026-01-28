using DSI.Dominio.Entidades;

namespace DSI.Persistencia.Repositorios;

/// <summary>
/// Repositório específico para Jobs
/// </summary>
public interface IJobRepositorio : IRepositorioBase<Job>
{
    /// <summary>
    /// Obtém job com todas as suas tabelas, mapeamentos e regras
    /// </summary>
    Task<Job?> ObterCompletoAsync(Guid id);

    /// <summary>
    /// Obtém todos os jobs com resumo (sem navegação)
    /// </summary>
    Task<IEnumerable<Job>> ObterResumoAsync();

    /// <summary>
    /// Obtém jobs por conexão
    /// </summary>
    Task<IEnumerable<Job>> ObterPorConexaoAsync(Guid conexaoId);
}
