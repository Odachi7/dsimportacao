using DSI.Dominio.Enums;

namespace DSI.Dominio.Entidades;

/// <summary>
/// Representa um Job (trabalho) de importação configurado
/// </summary>
public class Job
{
    /// <summary>
    /// Identificador único do job
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Nome amigável do job
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// ID da conexão de origem
    /// </summary>
    public Guid ConexaoOrigemId { get; set; }

    /// <summary>
    /// ID da conexão de destino
    /// </summary>
    public Guid ConexaoDestinoId { get; set; }

    /// <summary>
    /// Modo de importação (Completo ou Incremental)
    /// </summary>
    public ModoImportacao Modo { get; set; }

    /// <summary>
    /// Tamanho do lote para processamento em batch
    /// </summary>
    public int TamanhoLote { get; set; } = 1000;

    /// <summary>
    /// Política de tratamento de erros
    /// </summary>
    public PoliticaErro PoliticaErro { get; set; }

    /// <summary>
    /// Estratégia de resolução de conflitos
    /// </summary>
    public EstrategiaConflito EstrategiaConflito { get; set; }

    /// <summary>
    /// Data de criação
    /// </summary>
    public DateTime CriadoEm { get; set; }

    /// <summary>
    /// Data da última atualização
    /// </summary>
    public DateTime AtualizadoEm { get; set; }

    /// <summary>
    /// Tabelas incluídas neste job
    /// </summary>
    public List<TabelaJob> Tabelas { get; set; } = new();
}
