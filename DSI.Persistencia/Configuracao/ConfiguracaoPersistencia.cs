using DSI.Logging.Implementacoes;
using DSI.Logging.Interfaces;
using DSI.Persistencia.Contexto;
using DSI.Persistencia.Repositorios;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DSI.Persistencia.Configuracao;

/// <summary>
/// Extensões para configurar injeção de dependências da camada de persistência
/// </summary>
public static class ConfiguracaoPersistencia
{
    /// <summary>
    /// Adiciona serviços de persistência ao container de DI
    /// </summary>
    public static IServiceCollection AdicionarPersistencia(
        this IServiceCollection services,
        string stringConexao)
    {
        // Configura DbContext com SQLite
        services.AddDbContext<DsiDbContext>(options =>
            options.UseSqlite(stringConexao));

        // Registra repositórios
        services.AddScoped<IConexaoRepositorio, ConexaoRepositorio>();
        services.AddScoped<IJobRepositorio, JobRepositorio>();
        services.AddScoped<IExecucaoRepositorio, ExecucaoRepositorio>();

        return services;
    }

    /// <summary>
    /// Garante que o banco de dados foi criado e aplica migrations
    /// </summary>
    public static async Task InicializarBancoDadosAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsiDbContext>();
        
        // Cria o banco e aplica migrations
        await context.Database.MigrateAsync();
    }
}
