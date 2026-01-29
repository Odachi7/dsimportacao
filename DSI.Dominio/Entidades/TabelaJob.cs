namespace DSI.Dominio.Entidades;

/// <summary>
/// Representa uma tabela incluída em um job
/// </summary>
public class TabelaJob
{
    /// <summary>
    /// Identificador único
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID do job pai
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// Nome da tabela de origem
    /// </summary>
    public string TabelaOrigem { get; set; } = string.Empty;

    /// <summary>
    /// Nome da tabela de destino
    /// </summary>
    public string TabelaDestino { get; set; } = string.Empty;

    /// <summary>
    /// Ordem de execução (para respeitar dependências)
    /// </summary>
    public int OrdemExecucao { get; set; }

    /// <summary>
    /// Coluna usada como checkpoint para importação incremental (opcional)
    /// </summary>
    public string? ColunaCheckpoint { get; set; }

    /// <summary>
    /// Último valor do checkpoint para importação incremental
    /// </summary>
    public string? UltimoCheckpoint { get; set; }

    /// <summary>
    /// Mapeamentos de colunas desta tabela
    /// </summary>
    public List<Mapeamento> Mapeamentos { get; set; } = new();
}
