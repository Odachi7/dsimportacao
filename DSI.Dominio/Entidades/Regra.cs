using DSI.Dominio.Enums;

namespace DSI.Dominio.Entidades;

/// <summary>
/// Representa uma regra de transformação ou validação aplicada a uma coluna
/// </summary>
public class Regra
{
    /// <summary>
    /// Identificador único
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID do mapeamento ao qual esta regra pertence
    /// </summary>
    public Guid MapeamentoId { get; set; }

    /// <summary>
    /// Tipo da regra
    /// </summary>
    public TipoRegra TipoRegra { get; set; }

    /// <summary>
    /// Parâmetros da regra em formato JSON
    /// Ex: {"formatos": ["dd/MM/yyyy", "yyyy-MM-dd"], "timezone": "America/Sao_Paulo"}
    /// </summary>
    public string ParametrosJson { get; set; } = "{}";

    /// <summary>
    /// Ação a tomar quando a regra falha
    /// </summary>
    public AcaoFalhaRegra AcaoQuandoFalhar { get; set; }

    /// <summary>
    /// Ordem de aplicação da regra (regras são aplicadas em ordem crescente)
    /// </summary>
    public int Ordem { get; set; }
}
