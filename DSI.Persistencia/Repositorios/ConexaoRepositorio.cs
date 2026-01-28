using DSI.Dominio.Entidades;
using DSI.Persistencia.Contexto;
using Microsoft.EntityFrameworkCore;

namespace DSI.Persistencia.Repositorios;

/// <summary>
/// Implementação do repositório de Conexões
/// </summary>
public class ConexaoRepositorio : RepositorioBase<Conexao>, IConexaoRepositorio
{
    public ConexaoRepositorio(DsiDbContext contexto) : base(contexto)
    {
    }

    public async Task<IEnumerable<Conexao>> ObterPorTipoBancoAsync(DSI.Dominio.Enums.TipoBancoDados tipoBanco)
    {
        return await _dbSet
            .Where(c => c.TipoBanco == tipoBanco)
            .OrderBy(c => c.Nome)
            .ToListAsync();
    }

    public async Task<Conexao?> ObterPorNomeAsync(string nome)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.Nome == nome);
    }

    public async Task<bool> ExistePorNomeAsync(string nome)
    {
        return await _dbSet
            .AnyAsync(c => c.Nome == nome);
    }
}
