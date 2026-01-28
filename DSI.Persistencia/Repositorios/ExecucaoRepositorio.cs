using DSI.Dominio.Entidades;
using DSI.Dominio.Enums;
using DSI.Persistencia.Contexto;
using Microsoft.EntityFrameworkCore;

namespace DSI.Persistencia.Repositorios;

/// <summary>
/// Implementação do repositório de Execuções
/// </summary>
public class ExecucaoRepositorio : RepositorioBase<Execucao>, IExecucaoRepositorio
{
    public ExecucaoRepositorio(DsiDbContext contexto) : base(contexto)
    {
    }

    public async Task<Execucao?> ObterComDadosAsync(Guid id)
    {
        return await _dbSet
            .Include(e => e.EstatisticasTabelas)
            .Include(e => e.Erros)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IEnumerable<Execucao>> ObterHistoricoPorJobAsync(Guid jobId, int quantidade = 50)
    {
        return await _dbSet
            .Where(e => e.JobId == jobId)
            .OrderByDescending(e => e.IniciadoEm)
            .Take(quantidade)
            .ToListAsync();
    }

    public async Task<Execucao?> ObterUltimaExecucaoAsync(Guid jobId)
    {
        return await _dbSet
            .Where(e => e.JobId == jobId)
            .OrderByDescending(e => e.IniciadoEm)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Execucao>> ObterEmAndamentoAsync()
    {
        return await _dbSet
            .Where(e => e.Status == StatusExecucao.Executando)
            .OrderBy(e => e.IniciadoEm)
            .ToListAsync();
    }
}
