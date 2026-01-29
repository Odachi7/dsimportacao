using DSI.Dominio.Entidades;
using DSI.Dominio.Enums;
using System.Data;

namespace DSI.Motor;

/// <summary>
/// Contexto de execução do processo ETL
/// </summary>
public class ContextoExecucao : IDisposable
{
    /// <summary>
    /// Execução atual
    /// </summary>
    public Execucao Execucao { get; }

    /// <summary>
    /// Job sendo executado
    /// </summary>
    public Job Job { get; }

    /// <summary>
    /// Conexão com banco de origem
    /// </summary>
    public IDbConnection ConexaoOrigem { get; set; }

    /// <summary>
    /// Conexão com banco de destino
    /// </summary>
    public IDbConnection ConexaoDestino { get; set; }

    /// <summary>
    /// Transação do banco de destino
    /// </summary>
    public IDbTransaction? Transacao { get; set; }

    /// <summary>
    /// Token de cancelamento
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Evento disparado quando o progresso é atualizado
    /// </summary>
    public event EventHandler<ProgressoEventArgs>? ProgressoAtualizado;

    /// <summary>
    /// Total de linhas processadas
    /// </summary>
    public long TotalLinhasProcessadas { get; set; }

    /// <summary>
    /// Total de linhas com sucesso
    /// </summary>
    public long TotalLinhasSucesso { get; set; }

    /// <summary>
    /// Total de linhas com erro
    /// </summary>
    public long TotalLinhasErro { get; set; }

    public ContextoExecucao(
        Execucao execucao,
        Job job,
        IDbConnection conexaoOrigem,
        IDbConnection conexaoDestino,
        CancellationToken cancellationToken = default)
    {
        Execucao = execucao ?? throw new ArgumentNullException(nameof(execucao));
        Job = job ?? throw new ArgumentNullException(nameof(job));
        ConexaoOrigem = conexaoOrigem ?? throw new ArgumentNullException(nameof(conexaoOrigem));
        ConexaoDestino = conexaoDestino ?? throw new ArgumentNullException(nameof(conexaoDestino));
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Reporta progresso da execução
    /// </summary>
    public void ReportarProgresso(string mensagem, int percentual)
    {
        ProgressoAtualizado?.Invoke(this, new ProgressoEventArgs
        {
            Mensagem = mensagem,
            Percentual = percentual,
            LinhasProcessadas = TotalLinhasProcessadas,
            LinhasSucesso = TotalLinhasSucesso,
            LinhasErro = TotalLinhasErro
        });
    }

    /// <summary>
    /// Inicia transação no banco de destino
    /// </summary>
    public void IniciarTransacao()
    {
        if (ConexaoDestino.State != ConnectionState.Open)
            ConexaoDestino.Open();

        Transacao = ConexaoDestino.BeginTransaction();
    }

    /// <summary>
    /// Confirma transação
    /// </summary>
    public void ConfirmarTransacao()
    {
        Transacao?.Commit();
    }

    /// <summary>
    /// Reverte transação
    /// </summary>
    public void ReverterTransacao()
    {
        Transacao?.Rollback();
    }

    public void Dispose()
    {
        Transacao?.Dispose();
        ConexaoOrigem?.Dispose();
        ConexaoDestino?.Dispose();
    }
}

/// <summary>
/// Argumentos do evento de progresso
/// </summary>
public class ProgressoEventArgs : EventArgs
{
    public string Mensagem { get; set; } = string.Empty;
    public int Percentual { get; set; }
    public long LinhasProcessadas { get; set; }
    public long LinhasSucesso { get; set; }
    public long LinhasErro { get; set; }
}
