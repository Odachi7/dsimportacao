using DSI.Logging.Enums;

namespace DSI.Logging.Interfaces;

/// <summary>
/// Interface para log amigável (mensagens simples para o usuário)
/// </summary>
public interface ILogAmigavel
{
    /// <summary>
    /// Registra uma mensagem informativa amigável
    /// </summary>
    void Informar(string mensagem);

    /// <summary>
    /// Registra um aviso amigável
    /// </summary>
    void Avisar(string mensagem);

    /// <summary>
    /// Registra um erro amigável
    /// </summary>
    void Erro(string mensagem);

    /// <summary>
    /// Obtém as últimas mensagens do log amigável (para exibir na UI)
    /// </summary>
    IEnumerable<MensagemLog> ObterUltimasMensagens(int quantidade = 100);

    /// <summary>
    /// Limpa o buffer de mensagens
    /// </summary>
    void LimparBuffer();
}

/// <summary>
/// Representa uma mensagem de log
/// </summary>
public class MensagemLog
{
    public DateTime DataHora { get; set; }
    public NivelLog Nivel { get; set; }
    public string Mensagem { get; set; } = string.Empty;
}
