namespace DSI.Logging.Interfaces;

/// <summary>
/// Interface para log técnico (logs detalhados com stack traces)
/// </summary>
public interface ILogTecnico
{
    /// <summary>
    /// Registra informação técnica
    /// </summary>
    void Informar(string mensagem, object? contexto = null);

    /// <summary>
    /// Registra aviso técnico
    /// </summary>
    void Avisar(string mensagem, object? contexto = null);

    /// <summary>
    /// Registra erro técnico com exceção
    /// </summary>
    void Erro(string mensagem, Exception? excecao = null, object? contexto = null);

    /// <summary>
    /// Registra log de depuração (apenas em modo desenvolvimento)
    /// </summary>
    void Depurar(string mensagem, object? contexto = null);
}
