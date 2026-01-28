using DSI.Dominio.Enums;

namespace DSI.Dominio.Entidades;

/// <summary>
/// Representa um erro ocorrido durante a execução
/// </summary>
public class ErroExecucao
{
    /// <summary>
    /// Identificador único
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID da execução
    /// </summary>
    public Guid ExecucaoId { get; set; }

    /// <summary>
    /// ID da tabela do job onde ocorreu o erro
    /// </summary>
    public Guid TabelaJobId { get; set; }

    /// <summary>
    /// Chave da linha (se disponível, ex: ID da PK)
    /// </summary>
    public string? ChaveLinha { get; set; }

    /// <summary>
    /// Nome da coluna onde ocorreu o erro
    /// </summary>
    public string Coluna { get; set; } = string.Empty;

    /// <summary>
    /// Tipo da regra que falhou (se aplicável)
    /// </summary>
    public TipoRegra? TipoRegra { get; set; }

    /// <summary>
    /// Valor original que causou o erro
    /// </summary>
    public string? ValorOriginal { get; set; }

    /// <summary>
    /// Mensagem de erro amigável
    /// </summary>
    public string Mensagem { get; set; } = string.Empty;

    /// <summary>
    /// Data/hora do erro
    /// </summary>
    public DateTime OcorridoEm { get; set; }
}
