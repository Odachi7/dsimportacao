namespace DSI.Dominio.Entidades;

/// <summary>
/// Representa o mapeamento de uma coluna origem para uma coluna destino
/// </summary>
public class Mapeamento
{
    /// <summary>
    /// Identificador único
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID da tabela do job
    /// </summary>
    public Guid TabelaJobId { get; set; }

    /// <summary>
    /// Nome da coluna de origem (pode ser null se for valor constante)
    /// </summary>
    public string? ColunaOrigem { get; set; }

    /// <summary>
    /// Nome da coluna de destino
    /// </summary>
    public string ColunaDestino { get; set; } = string.Empty;

    /// <summary>
    /// Tipo da coluna destino
    /// </summary>
    public string TipoDestino { get; set; } = string.Empty;

    /// <summary>
    /// Se true, esta coluna será ignorada na importação
    /// </summary>
    public bool Ignorada { get; set; }

    /// <summary>
    /// Valor constante (usado quando ColunaOrigem é null)
    /// </summary>
    public string? ValorConstante { get; set; }

    /// <summary>
    /// Regras aplicadas a esta coluna
    /// </summary>
    public List<Regra> Regras { get; set; } = new();
}
