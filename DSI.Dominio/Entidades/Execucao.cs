using DSI.Dominio.Enums;

namespace DSI.Dominio.Entidades;

/// <summary>
/// Representa uma execução (run) de um job
/// </summary>
public class Execucao
{
    /// <summary>
    /// Identificador único da execução
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID do job que foi executado
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// Data/hora de início da execução
    /// </summary>
    public DateTime IniciadoEm { get; set; }

    /// <summary>
    /// Data/hora de término da execução (null se ainda em andamento)
    /// </summary>
    public DateTime? FinalizadoEm { get; set; }

    /// <summary>
    /// Status da execução
    /// </summary>
    public StatusExecucao Status { get; set; }

    /// <summary>
    /// Resumo da execução em formato JSON
    /// Ex: {"totalLinhas": 5000, "inseridas": 4800, "erros": 200}
    /// </summary>
    public string ResumoJson { get; set; } = "{}";

    /// <summary>
    /// Estatísticas por tabela
    /// </summary>
    public List<EstatisticaTabelaExecucao> EstatisticasTabelas { get; set; } = new();

    /// <summary>
    /// Erros ocorridos durante a execução
    /// </summary>
    public List<ErroExecucao> Erros { get; set; } = new();
}
