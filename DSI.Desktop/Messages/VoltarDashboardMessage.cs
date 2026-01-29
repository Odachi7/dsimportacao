namespace DSI.Desktop.Messages;

/// <summary>
/// Mensagem para solicitar navegação de volta ao Dashboard
/// </summary>
public record VoltarDashboardMessage(bool Force = true);
