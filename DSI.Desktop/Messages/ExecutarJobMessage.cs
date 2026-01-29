namespace DSI.Desktop.Messages;

/// <summary>
/// Mensagem para solicitar a execução de um job
/// </summary>
public record ExecutarJobMessage(Guid JobId);
