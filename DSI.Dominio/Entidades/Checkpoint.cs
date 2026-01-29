namespace DSI.Dominio.Entidades;

/// <summary>
/// Representa um checkpoint de importação incremental
/// </summary>
public class Checkpoint
{
    /// <summary>
    /// Identificador único do checkpoint
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID do job
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// Nome da tabela
    /// </summary>
    public string NomeTabela { get; set; } = string.Empty;

    /// <summary>
    /// Valor do último checkpoint (serializado como JSON)
    /// </summary>
    public string ValorCheckpoint { get; set; } = string.Empty;

    /// <summary>
    /// Data/hora do checkpoint
    /// </summary>
    public DateTime DataHora { get; set; }
}
