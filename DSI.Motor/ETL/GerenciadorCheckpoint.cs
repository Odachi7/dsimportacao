using DSI.Dominio.Entidades;

namespace DSI.Motor.ETL;

/// <summary>
/// Gerenciador de checkpoints para importações incrementais
/// Armazena e recupera pontos de checkpoint para retomada de importações
/// </summary>
public class GerenciadorCheckpoint
{
    /// <summary>
    /// Salva checkpoint de uma tabela
    /// </summary>
    public async Task SalvarCheckpointAsync(
        Guid execucaoId,
        string tabela,
        object valorCheckpoint)
    {
        // TODO: Implementar persistência no SQLite
        await Task.CompletedTask;
    }

    /// <summary>
    /// Recupera último checkpoint de uma tabela
    /// </summary>
    public async Task<object?> RecuperarCheckpointAsync(
        Guid jobId,
        string tabela)
    {
        // TODO: Implementar recuperação do SQLite
        await Task.CompletedTask;
        return null;
    }

    /// <summary>
   /// Limpa checkpoints antigos
    /// </summary>
    public async Task LimparCheckpointsAntigosAsync(int diasRetencao)
    {
        // TODO: Implementar limpeza no SQLite
        await Task.CompletedTask;
    }
}
