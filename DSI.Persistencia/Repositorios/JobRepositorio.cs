using DSI.Dominio.Entidades;
using DSI.Persistencia.Contexto;
using Microsoft.EntityFrameworkCore;

namespace DSI.Persistencia.Repositorios;

/// <summary>
/// Implementação do repositório de Jobs
/// </summary>
public class JobRepositorio : RepositorioBase<Job>, IJobRepositorio
{
    public JobRepositorio(DsiDbContext contexto) : base(contexto)
    {
    }

    public async Task<Job?> ObterCompletoAsync(Guid id)
    {
        return await _dbSet
            .Include(j => j.Tabelas)
                .ThenInclude(t => t.Mapeamentos)
                    .ThenInclude(m => m.Regras)
            .FirstOrDefaultAsync(j => j.Id == id);
    }

    public async Task<IEnumerable<Job>> ObterResumoAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .OrderByDescending(j => j.AtualizadoEm)
            .ToListAsync();
    }

    public async Task<IEnumerable<Job>> ObterPorConexaoAsync(Guid conexaoId)
    {
        return await _dbSet
            .Where(j => j.ConexaoOrigemId == conexaoId || j.ConexaoDestinoId == conexaoId)
            .OrderBy(j => j.Nome)
            .ToListAsync();
    }
}
