using DSI.Logging.Implementacoes;
using DSI.Logging.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DSI.Logging.Configuracao;

/// <summary>
/// Extensões para configurar sistema de logging
/// </summary>
public static class ConfiguracaoLogging
{
    /// <summary>
    /// Adiciona sistema de logging dual (amigável + técnico) ao container de DI
    /// </summary>
    public static IServiceCollection AdicionarLogging(
        this IServiceCollection services,
        string? caminhoLogsTecnicos = null)
    {
        // Define caminho padrão se não informado
        var caminhoLogs = caminhoLogsTecnicos 
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DsImporter", "logs");

        // Garante que o diretório existe
        Directory.CreateDirectory(caminhoLogs);

        // Registra log amigável como singleton (compartilhado em toda aplicação)
        services.AddSingleton<ILogAmigavel, LogAmigavel>();

        // Registra log técnico como singleton com caminho configurado
        services.AddSingleton<ILogTecnico>(provider => new LogTecnico(caminhoLogs));

        return services;
    }
}
