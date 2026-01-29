using DSI.Aplicacao.DTOs;
using DSI.Dominio.Entidades;
using DSI.Dominio.Enums;
using DSI.Persistencia.Repositorios;

namespace DSI.Aplicacao.Servicos;

/// <summary>
/// Serviço de aplicação para gerenciamento de Jobs
/// </summary>
public class ServicoJob
{
    private readonly IJobRepositorio _jobRepositorio;
    private readonly IConexaoRepositorio _conexaoRepositorio;

    public ServicoJob(
        IJobRepositorio jobRepositorio,
        IConexaoRepositorio conexaoRepositorio)
    {
        _jobRepositorio = jobRepositorio;
        _conexaoRepositorio = conexaoRepositorio;
    }

    /// <summary>
    /// Cria um novo Job
    /// </summary>
    public async Task<JobDto> CriarAsync(CriarJobDto dto)
    {
        // Validações
        await ValidarDtoAsync(dto);

        // Cria entidade
        var job = new Job
        {
            Id = Guid.NewGuid(),
            Nome = dto.Nome,
            ConexaoOrigemId = dto.ConexaoOrigemId,
            ConexaoDestinoId = dto.ConexaoDestinoId,
            Modo = dto.Modo,
            TamanhoLote = dto.TamanhoLote,
            PoliticaErro = dto.PoliticaErro,
            EstrategiaConflito = dto.EstrategiaConflito,
            CriadoEm = DateTime.UtcNow,
            AtualizadoEm = DateTime.UtcNow
        };

        await _jobRepositorio.AdicionarAsync(job);
        await _jobRepositorio.SalvarAsync();

        return await MapearParaDtoAsync(job);
    }

    /// <summary>
    /// Atualiza um Job existente
    /// </summary>
    public async Task<JobDto> AtualizarAsync(AtualizarJobDto dto)
    {
        var job = await _jobRepositorio.ObterPorIdAsync(dto.Id);
        if (job == null)
            throw new InvalidOperationException("Job não encontrado");

        // Validações
        await ValidarDtoAsync(dto);

        // Atualiza campos
        job.Nome = dto.Nome;
        job.ConexaoOrigemId = dto.ConexaoOrigemId;
        job.ConexaoDestinoId = dto.ConexaoDestinoId;
        job.Modo = dto.Modo;
        job.TamanhoLote = dto.TamanhoLote;
        job.PoliticaErro = dto.PoliticaErro;
        job.EstrategiaConflito = dto.EstrategiaConflito;
        job.AtualizadoEm = DateTime.UtcNow;

        await _jobRepositorio.AtualizarAsync(job);
        await _jobRepositorio.SalvarAsync();

        return await MapearParaDtoAsync(job);
    }

    /// <summary>
    /// Obtém todos os Jobs
    /// </summary>
    public async Task<IEnumerable<JobDto>> ObterTodosAsync()
    {
        var jobs = await _jobRepositorio.ObterTodosAsync();
        var tasks = jobs.Select(MapearParaDtoAsync);
        return await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Obtém um Job por ID
    /// </summary>
    public async Task<JobDto?> ObterPorIdAsync(Guid id)
    {
        var job = await _jobRepositorio.ObterPorIdAsync(id);
        return job != null ? await MapearParaDtoAsync(job) : null;
    }

    /// <summary>
    /// Obtém Job completo com tabelas e mapeamentos
    /// </summary>
    public async Task<JobCompletoDto?> ObterCompletoAsync(Guid id)
    {
        var job = await _jobRepositorio.ObterCompletoAsync(id);
        if (job == null)
            return null;

        var dto = new JobCompletoDto
        {
            Job = await MapearParaDtoAsync(job),
            Tabelas = job.Tabelas.Select(t => new TabelaJobDto
            {
                Id = t.Id,
                TabelaOrigem = t.TabelaOrigem,
                TabelaDestino = t.TabelaDestino,
                OrdemExecucao = t.OrdemExecucao,
                ColunaCheckpoint = t.ColunaCheckpoint,
                UltimoCheckpoint = t.UltimoCheckpoint
            }).ToList()
        };

        return dto;
    }

    /// <summary>
    /// Remove um Job
    /// </summary>
    public async Task RemoverAsync(Guid id)
    {
        // TODO: Validar se não há execuções em andamento
        await _jobRepositorio.RemoverAsync(id);
        await _jobRepositorio.SalvarAsync();
    }

    /// <summary>
    /// Valida o DTO
    /// </summary>
    private async Task ValidarDtoAsync(dynamic dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
            throw new ArgumentException("Nome do Job é obrigatório");

        if (dto.TamanhoLote < 1 || dto.TamanhoLote > 10000)
            throw new ArgumentException("Tamanho do lote deve estar entre 1 e 10000");

        // Valida conexões existem
        var conexaoOrigem = await _conexaoRepositorio.ObterPorIdAsync(dto.ConexaoOrigemId);
        if (conexaoOrigem == null)
            throw new InvalidOperationException("Conexão de origem não encontrada");

        var conexaoDestino = await _conexaoRepositorio.ObterPorIdAsync(dto.ConexaoDestinoId);
        if (conexaoDestino == null)
            throw new InvalidOperationException("Conexão de destino não encontrada");

        // Valida que não são a mesma conexão
        if (dto.ConexaoOrigemId == dto.ConexaoDestinoId)
            throw new InvalidOperationException("Conexões de origem e destino devem ser diferentes");
    }

    /// <summary>
    /// Mapeia entidade para DTO
    /// </summary>
    private async Task<JobDto> MapearParaDtoAsync(Job job)
    {
        var conexaoOrigem = await _conexaoRepositorio.ObterPorIdAsync(job.ConexaoOrigemId);
        var conexaoDestino = await _conexaoRepositorio.ObterPorIdAsync(job.ConexaoDestinoId);

        return new JobDto
        {
            Id = job.Id,
            Nome = job.Nome,
            ConexaoOrigemId = job.ConexaoOrigemId,
            ConexaoOrigemNome = conexaoOrigem?.Nome ?? "Desconhecida",
            ConexaoDestinoId = job.ConexaoDestinoId,
            ConexaoDestinoNome = conexaoDestino?.Nome ?? "Desconhecida",
            Modo = job.Modo,
            TamanhoLote = job.TamanhoLote,
            PoliticaErro = job.PoliticaErro,
            EstrategiaConflito = job.EstrategiaConflito,
            QuantidadeTabelas = job.Tabelas?.Count ?? 0,
            CriadoEm = job.CriadoEm,
            AtualizadoEm = job.AtualizadoEm
        };
    }
}
