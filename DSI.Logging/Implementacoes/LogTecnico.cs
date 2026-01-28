using DSI.Logging.Interfaces;
using Serilog;
using Serilog.Events;

namespace DSI.Logging.Implementacoes;

/// <summary>
/// Implementação do log técnico usando Serilog com gravação em arquivo
/// </summary>
public class LogTecnico : ILogTecnico
{
    private readonly ILogger _logger;

    public LogTecnico(string caminhoLog)
    {
        // Configura Serilog para gravar em arquivo com rotação diária
        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                path: Path.Combine(caminhoLog, "dsi-tecnico-.log"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                retainedFileCountLimit: 30 // Mantém logs dos últimos 30 dias
            )
            .CreateLogger();
    }

    public void Informar(string mensagem, object? contexto = null)
    {
        if (contexto != null)
        {
            _logger.Information("{Mensagem} | Contexto: {@Contexto}", mensagem, contexto);
        }
        else
        {
            _logger.Information(mensagem);
        }
    }

    public void Avisar(string mensagem, object? contexto = null)
    {
        if (contexto != null)
        {
            _logger.Warning("{Mensagem} | Contexto: {@Contexto}", mensagem, contexto);
        }
        else
        {
            _logger.Warning(mensagem);
        }
    }

    public void Erro(string mensagem, Exception? excecao = null, object? contexto = null)
    {
        if (excecao != null && contexto != null)
        {
            _logger.Error(excecao, "{Mensagem} | Contexto: {@Contexto}", mensagem, contexto);
        }
        else if (excecao != null)
        {
            _logger.Error(excecao, mensagem);
        }
        else if (contexto != null)
        {
            _logger.Error("{Mensagem} | Contexto: {@Contexto}", mensagem, contexto);
        }
        else
        {
            _logger.Error(mensagem);
        }
    }

    public void Depurar(string mensagem, object? contexto = null)
    {
        if (contexto != null)
        {
            _logger.Debug("{Mensagem} | Contexto: {@Contexto}", mensagem, contexto);
        }
        else
        {
            _logger.Debug(mensagem);
        }
    }
}
