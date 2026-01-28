namespace DSI.Conectores.Abstracoes.Modelos;

/// <summary>
/// Representa informações de uma tabela descoberta no schema
/// </summary>
public class InfoTabela
{
    /// <summary>
    /// Nome da tabela
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Schema/Esquema ao qual a tabela pertence (se aplicável)
    /// </summary>
    public string? Schema { get; set; }

    /// <summary>
    /// Tipo da tabela (TABLE, VIEW, etc)
    /// </summary>
    public string Tipo { get; set; } = "TABLE";

    /// <summary>
    /// Quantidade estimada de linhas (se disponível)
    /// </summary>
    public long? QuantidadeLinhas { get; set; }

    /// <summary>
    /// Colunas da tabela
    /// </summary>
    public List<InfoColuna> Colunas { get; set; } = new();
}
