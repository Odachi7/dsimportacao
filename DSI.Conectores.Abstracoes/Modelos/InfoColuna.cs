namespace DSI.Conectores.Abstracoes.Modelos;

/// <summary>
/// Representa informações de uma coluna descoberta no schema
/// </summary>
public class InfoColuna
{
    /// <summary>
    /// Nome da coluna
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de dados da coluna (específico do banco)
    /// </summary>
    public string TipoDados { get; set; } = string.Empty;

    /// <summary>
    /// Tipo .NET equivalente
    /// </summary>
    public string TipoNet { get; set; } = string.Empty;

    /// <summary>
    /// Indica se a coluna aceita nulos
    /// </summary>
    public bool AceitaNulo { get; set; }

    /// <summary>
    /// Indica se é chave primária
    /// </summary>
    public bool EhChavePrimaria { get; set; }

    /// <summary>
    /// Indica se é auto-incremento/identity
    /// </summary>
    public bool EhIdentity { get; set; }

    /// <summary>
    /// Tamanho/Comprimento da coluna (se aplicável)
    /// </summary>
    public int? Tamanho { get; set; }

    /// <summary>
    /// Precisão (para tipos numéricos)
    /// </summary>
    public int? Precisao { get; set; }

    /// <summary>
    /// Escala (para tipos numéricos)
    /// </summary>
    public int? Escala { get; set; }

    /// <summary>
    /// Valor padrão da coluna
    /// </summary>
    public string? ValorPadrao { get; set; }
}
