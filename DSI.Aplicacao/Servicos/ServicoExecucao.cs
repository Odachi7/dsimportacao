using DSI.Dominio.Entidades;
using DSI.Dominio.Enums;
using DSI.Motor;
using DSI.Motor.ETL;
using DSI.Persistencia.Contexto;
using DSI.Conectores.Abstracoes.Interfaces;
using Microsoft.EntityFrameworkCore;
using DSI.Conectores.Abstracoes;
using DSI.Seguranca.Criptografia;

namespace DSI.Aplicacao.Servicos;

/// <summary>
/// Serviço para executar jobs ETL
/// </summary>
public class ServicoExecucao
{
    private readonly DsiDbContext _context;

    private readonly MotorETL _motorETL;
    private readonly FabricaConectores _fabricaConectores;
    private readonly ServicoCriptografia _servicoCriptografia;
    private readonly Dictionary<Guid, CancellationTokenSource> _execucoesAtivas = new();

    /// <summary>
    /// Evento disparado quando o progresso de qualquer execução é atualizado
    /// </summary>
    public event EventHandler<(Guid ExecucaoId, ProgressoEventArgs Args)>? ProgressoRecebido;

    public ServicoExecucao(
        DsiDbContext context,
        MotorETL motorETL,
        FabricaConectores fabricaConectores,
        ServicoCriptografia servicoCriptografia)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _motorETL = motorETL ?? throw new ArgumentNullException(nameof(motorETL));
        _fabricaConectores = fabricaConectores ?? throw new ArgumentNullException(nameof(fabricaConectores));
        _servicoCriptografia = servicoCriptografia ?? throw new ArgumentNullException(nameof(servicoCriptografia));
    }

    /// <summary>
    /// Executa um job de forma assíncrona
    /// </summary>
    public async Task<Guid> ExecutarAsync(Guid jobId)
    {
        // Carrega job completo
        var job = await _context.Jobs
            .Include(j => j.Tabelas)
                .ThenInclude(t => t.Mapeamentos)
                    .ThenInclude(m => m.Regras)
            .Include(j => j.ConexaoOrigem)
            .Include(j => j.ConexaoDestino)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job == null)
            throw new InvalidOperationException($"Job {jobId} não encontrado");

        // Cria nova execução
        var execucao = new Execucao
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            IniciadoEm = DateTime.Now,
            Status = StatusExecucao.Executando
        };

        _context.Execucoes.Add(execucao);
        await _context.SaveChangesAsync();

        // Cria token de cancelamento
        var cts = new CancellationTokenSource();
        _execucoesAtivas[execucao.Id] = cts;

        // Executa em background
        _ = Task.Run(async () =>
        {
            try
            {
                // Resolve conectores via Factory
                var conectorOrigem = _fabricaConectores.ObterConector(job.ConexaoOrigem.TipoBanco);
                var conectorDestino = _fabricaConectores.ObterConector(job.ConexaoDestino.TipoBanco);

                // Cria conexões (com descriptografia)
                var strOrigem = await ObterStringConexaoAsync(job.ConexaoOrigem);
                var strDestino = await ObterStringConexaoAsync(job.ConexaoDestino);

                var conexaoOrigem = conectorOrigem.CriarConexao(strOrigem);
                var conexaoDestino = conectorDestino.CriarConexao(strDestino);

                // Cria contexto de execução
                using var contexto = new ContextoExecucao(
                    execucao,
                    job,
                    conexaoOrigem,
                    conexaoDestino,
                    conectorOrigem,
                    conectorDestino,
                    cts.Token);

                // Assina evento de progresso
                contexto.ProgressoAtualizado += async (sender, args) =>
                {
                    // Notifica UI via evento
                    ProgressoRecebido?.Invoke(this, (execucao.Id, args));
                    
                    // Persiste no banco
                    await AtualizarProgressoAsync(execucao.Id, args);
                };

                // Executa motor ETL
                await _motorETL.ExecutarAsync(contexto);

                // Atualiza execução final
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                execucao.Status = StatusExecucao.Falhou;
                
                var erro = new ErroExecucao
                {
                    Id = Guid.NewGuid(),
                    ExecucaoId = execucao.Id,
                    OcorridoEm = DateTime.Now,
                    Mensagem = $"Erro fatal: {ex.Message}"
                };
                
                execucao.Erros.Add(erro);
                await _context.SaveChangesAsync();
            }
            finally
            {
                _execucoesAtivas.Remove(execucao.Id);
            }
        }, cts.Token);

        return execucao.Id;
    }

    /// <summary>
    /// Cancela uma execução em andamento
    /// </summary>
    public async Task<bool> CancelarAsync(Guid execucaoId)
    {
        if (_execucoesAtivas.TryGetValue(execucaoId, out var cts))
        {
            cts.Cancel();
            
            var execucao = await _context.Execucoes.FindAsync(execucaoId);
            if (execucao != null)
            {
                execucao.Status = StatusExecucao.Cancelado;
                execucao.FinalizadoEm = DateTime.Now;
                await _context.SaveChangesAsync();
            }
            
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// Obtém status de uma execução
    /// </summary>
    public async Task<StatusExecucaoDto?> ObterStatusAsync(Guid execucaoId)
    {
        var execucao = await _context.Execucoes
            .Include(e => e.EstatisticasTabelas)
            .Include(e => e.Erros)
            .FirstOrDefaultAsync(e => e.Id == execucaoId);

        if (execucao == null)
            return null;

        return new StatusExecucaoDto
        {
            ExecucaoId = execucao.Id,
            JobId = execucao.JobId,
            Status = execucao.Status,
            IniciadoEm = execucao.IniciadoEm,
            FinalizadoEm = execucao.FinalizadoEm,
            ResumoJson = execucao.ResumoJson,
            EstaEmExecucao = _execucoesAtivas.ContainsKey(execucao.Id),
            TotalTabelas = execucao.EstatisticasTabelas.Count,
            TotalErros = execucao.Erros.Count
        };
    }

    /// <summary>
    /// Lista execuções de um job
    /// </summary>
    public async Task<List<StatusExecucaoDto>> ListarExecucoesAsync(
        Guid jobId,
        int limite = 50)
    {
        var execucoes = await _context.Execucoes
            .Where(e => e.JobId == jobId)
            .OrderByDescending(e => e.IniciadoEm)
            .Take(limite)
            .Include(e => e.EstatisticasTabelas)
            .Include(e => e.Erros)
            .ToListAsync();

        return execucoes.Select(e => new StatusExecucaoDto
        {
            ExecucaoId = e.Id,
            JobId = e.JobId,
            Status = e.Status,
            IniciadoEm = e.IniciadoEm,
            FinalizadoEm = e.FinalizadoEm,
            ResumoJson = e.ResumoJson,
            EstaEmExecucao = _execucoesAtivas.ContainsKey(e.Id),
            TotalTabelas = e.EstatisticasTabelas.Count,
            TotalErros = e.Erros.Count
        }).ToList();
    }

    // Métodos privados auxiliares
    
    private Task<string> ObterStringConexaoAsync(Conexao conexao)
    {
        if (conexao == null) return Task.FromResult(string.Empty);
        
        try
        {
            return Task.FromResult(_servicoCriptografia.Descriptografar(conexao.StringConexaoCriptografada));
        }
        catch
        {
            // Fallback se não estiver criptografado (legado ou teste)
            return Task.FromResult(conexao.StringConexaoCriptografada);
        }
    }

    private async Task AtualizarProgressoAsync(Guid execucaoId, ProgressoEventArgs args)
    {
        // Atualiza progresso no banco (poderia usar SignalR para UI em tempo real)
        var execucao = await _context.Execucoes.FindAsync(execucaoId);
        if (execucao != null)
        {
            // Atualiza resumo com progresso
            execucao.ResumoJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                ultimaMensagem = args.Mensagem,
                percentual = args.Percentual,
                linhasProcessadas = args.LinhasProcessadas,
                linhasSucesso = args.LinhasSucesso,
                linhasErro = args.LinhasErro
            });
            
            await _context.SaveChangesAsync();
        }
    }
}

/// <summary>
/// DTO com status de execução
/// </summary>
public class StatusExecucaoDto
{
    public Guid ExecucaoId { get; set; }
    public Guid JobId { get; set; }
    public StatusExecucao Status { get; set; }
    public DateTime IniciadoEm { get; set; }
    public DateTime? FinalizadoEm { get; set; }
    public string ResumoJson { get; set; } = "{}";
    public bool EstaEmExecucao { get; set; }
    public int TotalTabelas { get; set; }
    public int TotalErros { get; set; }
}
