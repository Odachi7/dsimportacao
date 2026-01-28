using DSI.Persistencia.Contexto;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DSI.Persistencia;

/// <summary>
/// Factory para criação do DbContext em design-time (migrations)
/// </summary>
public class DsiDbContextFactory : IDesignTimeDbContextFactory<DsiDbContext>
{
    public DsiDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DsiDbContext>();
        
        // Usa um banco SQLite temporário para migrations
        optionsBuilder.UseSqlite("Data Source=dsi.db");

        return new DsiDbContext(optionsBuilder.Options);
    }
}
