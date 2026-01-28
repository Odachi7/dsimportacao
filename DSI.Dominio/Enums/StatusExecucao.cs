namespace DSI.Dominio.Enums;

/// <summary>
/// Status de execução de um job
/// </summary>
public enum StatusExecucao
{
    /// <summary>
    /// Execução em andamento
    /// </summary>
    Executando = 1,

    /// <summary>
    /// Execução concluída com sucesso
    /// </summary>
    Concluido = 2,

    /// <summary>
    /// Execução falhou
    /// </summary>
    Falhou = 3,

    /// <summary>
    /// Execução cancelada pelo usuário
    /// </summary>
    Cancelado = 4
}
