using DSI.Dominio.Entidades;
using DSI.Dominio.Enums;
using DSI.Persistencia.Contexto;
using Microsoft.EntityFrameworkCore;

namespace DSI.Aplicacao.Servicos;

/// <summary>
/// Serviço para consulta de histórico e auditoria de execuções
/// </summary>
public class ServicoHistorico
{
    private readonly DsiDbContext _context;

    public ServicoHistorico(DsiDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Obtém detalhes completos de uma execução
    /// </summary>
    public async Task<DetalhesExecucaoDto?> ObterDetalhesExecucaoAsync(Guid execucaoId)
    {
        var execucao = await _context.Execucoes
            .Include(e => e.EstatisticasTabelas)
            .Include(e => e.Erros)
            .FirstOrDefaultAsync(e => e.Id == execucaoId);

        if (execucao == null)
            return null;

        var job = await _context.Jobs.FindAsync(execucao.JobId);

        return new DetalhesExecucaoDto
        {
            ExecucaoId = execucao.Id,
            JobId = execucao.JobId,
            NomeJob = job?.Nome ?? "Job não encontrado",
            Status = execucao.Status,
            IniciadoEm = execucao.IniciadoEm,
            FinalizadoEm = execucao.FinalizadoEm,
            DuracaoSegundos = execucao.FinalizadoEm.HasValue
                ? (execucao.FinalizadoEm.Value - execucao.IniciadoEm).TotalSeconds
                : null,
            ResumoJson = execucao.ResumoJson,
            Estatisticas = execucao.EstatisticasTabelas.Select(e => new EstatisticaTabelaDto
            {
                TabelaJobId = e.TabelaJobId,
                LinhasLidas = e.LinhasLidas,
                LinhasInseridas = e.LinhasInseridas,
                LinhasAtualizadas = e.LinhasAtualizadas,
                LinhasPuladas = e.LinhasPuladas,
                LinhasComErro = e.LinhasComErro,
                DuracaoMs = e.DuracaoMs
            }).ToList(),
            Erros = execucao.Erros.Select(e => new ErroExecucaoDto
            {
                Id = e.Id,
                OcorridoEm = e.OcorridoEm,
                TabelaJobId = e.TabelaJobId,
                Coluna = e.Coluna,
                ChaveLinha = e.ChaveLinha,
                ValorOriginal = e.ValorOriginal,
                Mensagem = e.Mensagem,
                TipoRegra = e.TipoRegra
            }).ToList()
        };
    }

    /// <summary>
    /// Lista histórico de execuções com filtros
    /// </summary>
    public async Task<List<ResumoExecucaoDto>> ListarHistoricoAsync(
        Guid? jobId = null,
        StatusExecucao? status = null,
        DateTime? dataInicio = null,
        DateTime? dataFim = null,
        int pagina = 1,
        int tamanhoPagina = 50)
    {
        var query = _context.Execucoes.AsQueryable();

        // Aplica filtros
        if (jobId.HasValue)
            query = query.Where(e => e.JobId == jobId.Value);

        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);

        if (dataInicio.HasValue)
            query = query.Where(e => e.IniciadoEm >= dataInicio.Value);

        if (dataFim.HasValue)
            query = query.Where(e => e.IniciadoEm <= dataFim.Value);

        // Paginação
        var execucoes = await query
            .OrderByDescending(e => e.IniciadoEm)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .Include(e => e.EstatisticasTabelas)
            .Include(e => e.Erros)
            .ToListAsync();

        // Carrega nomes dos jobs
        var jobIds = execucoes.Select(e => e.JobId).Distinct();
        var jobs = await _context.Jobs
            .Where(j => jobIds.Contains(j.Id))
            .ToDictionaryAsync(j => j.Id, j => j.Nome);

        return execucoes.Select(e => new ResumoExecucaoDto
        {
            ExecucaoId = e.Id,
            JobId = e.JobId,
            NomeJob = jobs.GetValueOrDefault(e.JobId, "Desconhecido"),
            Status = e.Status,
            IniciadoEm = e.IniciadoEm,
            FinalizadoEm = e.FinalizadoEm,
            DuracaoSegundos = e.FinalizadoEm.HasValue
                ? (e.FinalizadoEm.Value - e.IniciadoEm).TotalSeconds
                : null,
            TotalTabelas = e.EstatisticasTabelas.Count,
            TotalLinhasProcessadas = e.EstatisticasTabelas.Sum(est => est.LinhasLidas),
            TotalLinhasSucesso = e.EstatisticasTabelas.Sum(est => est.LinhasInseridas + est.LinhasAtualizadas),
            TotalErros = e.Erros.Count
        }).ToList();
    }

    /// <summary>
    /// Obtém estatísticas agregadas de um job
    /// </summary>
    public async Task<EstatisticasJobDto> ObterEstatisticasJobAsync(Guid jobId)
    {
        var execucoes = await _context.Execucoes
            .Where(e => e.JobId == jobId)
            .Include(e => e.EstatisticasTabelas)
            .Include(e => e.Erros)
            .ToListAsync();

        if (execucoes.Count == 0)
        {
            return new EstatisticasJobDto { JobId = jobId };
        }

        var job = await _context.Jobs.FindAsync(jobId);

        return new EstatisticasJobDto
        {
            JobId = jobId,
            NomeJob = job?.Nome ?? "Job não encontrado",
            TotalExecucoes = execucoes.Count,
            ExecucoesSucesso = execucoes.Count(e => e.Status == StatusExecucao.Concluido),
            ExecucoesFalha = execucoes.Count(e => e.Status == StatusExecucao.Falhou),
            ExecucoesCanceladas = execucoes.Count(e => e.Status == StatusExecucao.Cancelado),
            UltimaExecucao = execucoes.Max(e => e.IniciadoEm),
            DuracaoMediaSegundos = execucoes
                .Where(e => e.FinalizadoEm.HasValue)
                .Average(e => (e.FinalizadoEm!.Value - e.IniciadoEm).TotalSeconds),
            TotalLinhasProcessadas = execucoes
                .SelectMany(e => e.EstatisticasTabelas)
                .Sum(est => est.LinhasLidas),
            TotalLinhasSucesso = execucoes
                .SelectMany(e => e.EstatisticasTabelas)
                .Sum(est => est.LinhasInseridas + est.LinhasAtualizadas),
            TotalErros = execucoes.Sum(e => e.Erros.Count)
        };
    }

    /// <summary>
    /// Obtém erros de uma execução com paginação
    /// </summary>
    public async Task<ListaErrosDto> ObterErrosExecucaoAsync(
        Guid execucaoId,
        int pagina = 1,
        int tamanhoPagina = 100)
    {
        var totalErros = await _context.Set<ErroExecucao>()
            .CountAsync(e => e.ExecucaoId == execucaoId);

        var erros = await _context.Set<ErroExecucao>()
            .Where(e => e.ExecucaoId == execucaoId)
            .OrderBy(e => e.OcorridoEm)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync();

        return new ListaErrosDto
        {
            ExecucaoId = execucaoId,
            TotalErros = totalErros,
            Pagina = pagina,
            TamanhoPagina = tamanhoPagina,
            TotalPaginas = (int)Math.Ceiling(totalErros / (double)tamanhoPagina),
            Erros = erros.Select(e => new ErroExecucaoDto
            {
                Id = e.Id,
                OcorridoEm = e.OcorridoEm,
                TabelaJobId = e.TabelaJobId,
                Coluna = e.Coluna,
                ChaveLinha = e.ChaveLinha,
                ValorOriginal = e.ValorOriginal,
                Mensagem = e.Mensagem,
                TipoRegra = e.TipoRegra
            }).ToList()
        };
    }

    /// <summary>
    /// Limpa histórico antigo de execuções
    /// </summary>
    public async Task<int> LimparHistoricoAntigoAsync(int diasRetencao)
    {
        var dataLimite = DateTime.Now.AddDays(-diasRetencao);

        var execucoesAntigas = await _context.Execucoes
            .Where(e => e.IniciadoEm < dataLimite)
            .ToListAsync();

        _context.Execucoes.RemoveRange(execucoesAntigas);
        return await _context.SaveChangesAsync();
    }
}

// DTOs

public class DetalhesExecucaoDto
{
    public Guid ExecucaoId { get; set; }
    public Guid JobId { get; set; }
    public string NomeJob { get; set; } = string.Empty;
    public StatusExecucao Status { get; set; }
    public DateTime IniciadoEm { get; set; }
    public DateTime? FinalizadoEm { get; set; }
    public double? DuracaoSegundos { get; set; }
    public string ResumoJson { get; set; } = "{}";
    public List<EstatisticaTabelaDto> Estatisticas { get; set; } = new();
    public List<ErroExecucaoDto> Erros { get; set; } = new();
}

public class ResumoExecucaoDto
{
    public Guid ExecucaoId { get; set; }
    public Guid JobId { get; set; }
    public string NomeJob { get; set; } = string.Empty;
    public StatusExecucao Status { get; set; }
    public DateTime IniciadoEm { get; set; }
    public DateTime? FinalizadoEm { get; set; }
    public double? DuracaoSegundos { get; set; }
    public int TotalTabelas { get; set; }
    public int TotalLinhasProcessadas { get; set; }
    public int TotalLinhasSucesso { get; set; }
    public int TotalErros { get; set; }
}

public class EstatisticaTabelaDto
{
    public Guid TabelaJobId { get; set; }
    public int LinhasLidas { get; set; }
    public int LinhasInseridas { get; set; }
    public int LinhasAtualizadas { get; set; }
    public int LinhasPuladas { get; set; }
    public int LinhasComErro { get; set; }
    public long DuracaoMs { get; set; }
}

public class ErroExecucaoDto
{
    public Guid Id { get; set; }
    public DateTime OcorridoEm { get; set; }
    public Guid TabelaJobId { get; set; }
    public string Coluna { get; set; } = string.Empty;
    public string? ChaveLinha { get; set; }
    public string? ValorOriginal { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public TipoRegra? TipoRegra { get; set; }
}

public class EstatisticasJobDto
{
    public Guid JobId { get; set; }
    public string NomeJob { get; set; } = string.Empty;
    public int TotalExecucoes { get; set; }
    public int ExecucoesSucesso { get; set; }
    public int ExecucoesFalha { get; set; }
    public int ExecucoesCanceladas { get; set; }
    public DateTime? UltimaExecucao { get; set; }
    public double DuracaoMediaSegundos { get; set; }
    public int TotalLinhasProcessadas { get; set; }
    public int TotalLinhasSucesso { get; set; }
    public int TotalErros { get; set; }
}

public class ListaErrosDto
{
    public Guid ExecucaoId { get; set; }
    public int TotalErros { get; set; }
    public int Pagina { get; set; }
    public int TamanhoPagina { get; set; }
    public int TotalPaginas { get; set; }
    public List<ErroExecucaoDto> Erros { get; set; } = new();
}
