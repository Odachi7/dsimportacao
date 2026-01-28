using DSI.Persistencia.Contexto;
using Microsoft.EntityFrameworkCore;

namespace DSI.Persistencia.Repositorios;

/// <summary>
/// Implementação base do repositório com operações CRUD
/// </summary>
public class RepositorioBase<T> : IRepositorioBase<T> where T : class  
{
    protected readonly DsiDbContext _contexto;
    protected readonly DbSet<T> _dbSet;

    public RepositorioBase(DsiDbContext contexto)
    {
        _contexto = contexto;
        _dbSet = contexto.Set<T>();
    }

    public virtual async Task<IEnumerable<T>> ObterTodosAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<T?> ObterPorIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<T> AdicionarAsync(T entidade)
    {
        await _dbSet.AddAsync(entidade);
        return entidade;
    }

    public virtual async Task AtualizarAsync(T entidade)
    {
        _dbSet.Update(entidade);
        await Task.CompletedTask;
    }

    public virtual async Task RemoverAsync(Guid id)
    {
        var entidade = await ObterPorIdAsync(id);
        if (entidade != null)
        {
            _dbSet.Remove(entidade);
        }
    }

    public virtual async Task<int> SalvarAsync()
    {
        return await _contexto.SaveChangesAsync();
    }
}
