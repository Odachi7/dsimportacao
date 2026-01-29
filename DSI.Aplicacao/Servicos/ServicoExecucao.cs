using DSI.Dominio.Entidades;
using DSI.Dominio.Enums;
using DSI.Motor;
using DSI.Motor.ETL;
using DSI.Persistencia.Contexto;
using DSI.Conectores.Abstracoes.Interfaces;
using Microsoft.EntityFrameworkCore;
using DSI.Conectores.Abstracoes;
using DSI.Seguranca.Criptografia;
using Microsoft.Extensions.DependencyInjection; // Necessário para IServiceScopeFactory

namespace DSI.Aplicacao.Servicos;

/// <summary>
/// Serviço para executar jobs ETL
/// </summary>
public class ServicoExecucao
{
    private readonly DsiDbContext _context;

    private readonly MotorETL _motorETL;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly Dictionary<Guid, CancellationTokenSource> _execucoesAtivas = new();

    public event EventHandler<(Guid ExecucaoId, ProgressoEventArgs Args)>? ProgressoRecebido;

    public ServicoExecucao(
        DsiDbContext context,
        MotorETL motorETL,
        FabricaConectores fabricaConectores,
        ServicoCriptografia servicoCriptografia,
        IServiceScopeFactory serviceScopeFactory)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _motorETL = motorETL ?? throw new ArgumentNullException(nameof(motorETL));
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
    }

    public async Task<Guid> ExecutarAsync(Guid jobId)
    {
        var job = await _context.Jobs
            .Include(j => j.Tabelas)
                .ThenInclude(t => t.Mapeamentos)
                    .ThenInclude(m => m.Regras)
            .Include(j => j.ConexaoOrigem)
            .Include(j => j.ConexaoDestino)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job == null)
            throw new InvalidOperationException($"Job {jobId} não encontrado");

        var execucao = new Execucao
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            IniciadoEm = DateTime.Now,
            Status = StatusExecucao.Executando
        };

        _context.Execucoes.Add(execucao);
        await _context.SaveChangesAsync();

        var cts = new CancellationTokenSource();
        _execucoesAtivas[execucao.Id] = cts;

        _ = Task.Run(async () =>
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DsiDbContext>();
            var fabricaConectores = scope.ServiceProvider.GetRequiredService<FabricaConectores>();
            var motorETL = scope.ServiceProvider.GetRequiredService<MotorETL>();
            var servicoCriptografia = scope.ServiceProvider.GetRequiredService<ServicoCriptografia>();

            try
            {
                var conectorOrigem = fabricaConectores.ObterConector(job.ConexaoOrigem.TipoBanco);
                var conectorDestino = fabricaConectores.ObterConector(job.ConexaoDestino.TipoBanco);

                // Recarrega conexões do job anexado ao contexto novo (opcional, mas seguro) ou usa as passadas se não tracking?
                // O objeto 'job' veio do contexto anterior. Pode ser usado como DTO aqui.

                var strOrigem = await ObterStringConexaoAsync(job.ConexaoOrigem, servicoCriptografia);
                var strDestino = await ObterStringConexaoAsync(job.ConexaoDestino, servicoCriptografia);

                var conexaoOrigem = conectorOrigem.CriarConexao(strOrigem);
                var conexaoDestino = conectorDestino.CriarConexao(strDestino);

                // Carrega a execução anexada a este contexto para garantir tracking correto
                var execucaoBackground = await context.Execucoes.FindAsync(execucao.Id) 
                                         ?? throw new InvalidOperationException("Execução não encontrada no background");

                // Cria contexto de execução usando a instância TRACKED
                using var contextoExecucao = new ContextoExecucao(
                    execucaoBackground,
                    job,
                    conexaoOrigem,
                    conexaoDestino,
                    conectorOrigem,
                    conectorDestino,
                    cts.Token);
                
                // Assina evento de progresso
                // Assina evento de progresso
                contextoExecucao.ProgressoAtualizado += async (sender, args) =>
                {
                    // Notifica UI via evento
                    ProgressoRecebido?.Invoke(this, (execucao.Id, args));
                    
                    // Persiste no banco usando um novo escopo para evitar concorrência no DbContext principal
                    await AtualizarProgressoSafelyAsync(execucao.Id, args);
                };

                // Executa motor ETL
                await motorETL.ExecutarAsync(contextoExecucao);

                // Atualiza execução final (o status foi atualizado na instância execucaoBackground pelo MotorETL)
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Carrega execução para registrar erro
                var execucaoErro = await context.Execucoes.FindAsync(execucao.Id);
                
                if (execucaoErro != null)
                {
                    execucaoErro.Status = StatusExecucao.Falhou;
                    var erro = new ErroExecucao
                    {
                        Id = Guid.NewGuid(),
                        ExecucaoId = execucao.Id,
                        OcorridoEm = DateTime.Now,
                        Mensagem = $"Erro fatal: {ex.Message}"
                    };
                    context.Entry(erro).State = EntityState.Added;
                    execucaoErro.Erros.Add(erro);
                    await context.SaveChangesAsync();
                }
            }
            finally
            {
                _execucoesAtivas.Remove(execucao.Id);
            }
        }, cts.Token);

        return execucao.Id;
    }

    private async Task AtualizarProgressoSafelyAsync(Guid execucaoId, ProgressoEventArgs args)
    {
        try 
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DsiDbContext>();
            
            var exec = await context.Execucoes.FindAsync(execucaoId);
            if (exec != null)
            {
                exec.ResumoJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    ultimaMensagem = args.Mensagem,
                    percentual = args.Percentual,
                    linhasProcessadas = args.LinhasProcessadas,
                    linhasSucesso = args.LinhasSucesso,
                    linhasErro = args.LinhasErro
                });
                await context.SaveChangesAsync();
            }
        }
        catch 
        {
            // Ignora erros de atualização de progresso para não parar o job
        }
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
    
    private Task<string> ObterStringConexaoAsync(Conexao conexao, ServicoCriptografia servicoCriptografia)
    {
        if (conexao == null) return Task.FromResult(string.Empty);
        
        try
        {
            return Task.FromResult(servicoCriptografia.Descriptografar(conexao.StringConexaoCriptografada));
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
