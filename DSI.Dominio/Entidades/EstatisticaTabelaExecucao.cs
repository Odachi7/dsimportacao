namespace DSI.Dominio.Entidades;

/// <summary>
/// Estatísticas de execução por tabela
/// </summary>
public class EstatisticaTabelaExecucao
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
    /// ID da tabela do job
    /// </summary>
    public Guid TabelaJobId { get; set; }

    /// <summary>
    /// Quantidade de linhas lidas da origem
    /// </summary>
    public int LinhasLidas { get; set; }

    /// <summary>
    /// Quantidade de linhas inseridas
    /// </summary>
    public int LinhasInseridas { get; set; }

    /// <summary>
    /// Quantidade de linhas atualizadas (em caso de upsert)
    /// </summary>
    public int LinhasAtualizadas { get; set; }

    /// <summary>
    /// Quantidade de linhas puladas
    /// </summary>
    public int LinhasPuladas { get; set; }

    /// <summary>
    /// Quantidade de linhas com erro
    /// </summary>
    public int LinhasComErro { get; set; }

    /// <summary>
    /// Duração do processamento em milissegundos
    /// </summary>
    public long DuracaoMs { get; set; }
}
