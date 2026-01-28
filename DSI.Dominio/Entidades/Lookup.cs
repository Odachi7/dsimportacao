using DSI.Dominio.Enums;

namespace DSI.Dominio.Entidades;

/// <summary>
/// Representa uma tabela De/Para (Lookup) para transformação de valores
/// </summary>
public class Lookup
{
    /// <summary>
    /// Identificador único
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID do mapeamento ao qual este lookup pertence
    /// </summary>
    public Guid MapeamentoId { get; set; }

    /// <summary>
    /// Nome do lookup
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Tipo do lookup (lista local ou tabela de banco)
    /// </summary>
    public TipoLookup Tipo { get; set; }

    /// <summary>
    /// Configuração em JSON
    /// Para lista local: {"valores": {"chave1": "valor1", "chave2": "valor2"}}
    /// Para banco: {"conexaoId": "guid", "tabela": "nome", "colunaChave": "col", "colunaValor": "col"}
    /// </summary>
    public string ConfiguracaoJson { get; set; } = "{}";

    /// <summary>
    /// Valor padrão quando a chave não é encontrada
    /// </summary>
    public string? ValorPadrao { get; set; }
}
