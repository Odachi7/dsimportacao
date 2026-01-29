using DSI.Aplicacao.DTOs;
using DSI.Aplicacao.Utilitarios;
using DSI.Conectores.Abstracoes.Interfaces;
using DSI.Conectores.Abstracoes.Modelos;
using DSI.Dominio.Entidades;
using DSI.Dominio.Enums;
using DSI.Persistencia.Repositorios;

namespace DSI.Aplicacao.Servicos;

/// <summary>
/// Serviço para gerenciamento de mapeamentos de colunas
/// </summary>
public class ServicoMapeamento
{
    private readonly IJobRepositorio _jobRepositorio;
    private readonly ServicoConexao _servicoConexao;

    public ServicoMapeamento(
        IJobRepositorio jobRepositorio,
        ServicoConexao servicoConexao)
    {
        _jobRepositorio = jobRepositorio;
        _servicoConexao = servicoConexao;
    }

    /// <summary>
    /// Configura mapeamentos para uma tabela
    /// </summary>
    public async Task ConfigurarMapeamentosAsync(ConfigurarMapeamentosDto dto)
    {
        var job = await _jobRepositorio.ObterCompletoAsync(dto.TabelaJobId);
        if (job == null)
            throw new InvalidOperationException("Job não encontrado");

        var tabelaJob = job.Tabelas.FirstOrDefault(t => t.Id == dto.TabelaJobId);
        if (tabelaJob == null)
            throw new InvalidOperationException("Tabela não encontrada no Job");

        // Limpa mapeamentos existentes
        tabelaJob.Mapeamentos.Clear();

        // Adiciona novos mapeamentos
        foreach (var mapDto in dto.Mapeamentos)
        {
            var mapeamento = new Mapeamento
            {
                Id = mapDto.Id ?? Guid.NewGuid(),
                TabelaJobId = dto.TabelaJobId,
                ColunaOrigem = mapDto.ColunaOrigem,
                ColunaDestino = mapDto.ColunaDestino,
                TipoDestino = mapDto.TipoDestino,
                Ignorada = mapDto.Ignorada,
                ValorConstante = mapDto.ValorConstante
            };

            tabelaJob.Mapeamentos.Add(mapeamento);

            foreach (var regraDto in mapDto.Regras)
            {
                mapeamento.Regras.Add(new Regra
                {
                    Id = regraDto.Id ?? Guid.NewGuid(),
                    MapeamentoId = mapeamento.Id,
                    TipoRegra = regraDto.TipoRegra,
                    Ordem = regraDto.Ordem,
                    ParametrosJson = regraDto.Parametros ?? "",
                    AcaoQuandoFalhar = AcaoFalhaRegra.AplicarDefault // Default para MVP
                });
            }
        }

        await _jobRepositorio.AtualizarAsync(job);
        await _jobRepositorio.SalvarAsync();
    }

    /// <summary>
    /// Realiza auto-mapeamento de colunas
    /// </summary>
    public async Task<ResultadoAutoMapeamento> AutoMapearAsync(
        Guid jobId,
        string tabelaOrigem,
        string tabelaDestino,
        IConector conectorOrigem,
        IConector conectorDestino)
    {
        var job = await _jobRepositorio.ObterPorIdAsync(jobId);
        if (job == null)
            throw new InvalidOperationException("Job não encontrado");

        // Obtém schemas das tabelas
        var stringConexaoOrigem = await _servicoConexao.ObterStringConexaoAsync(job.ConexaoOrigemId);
        var stringConexaoDestino = await _servicoConexao.ObterStringConexaoAsync(job.ConexaoDestinoId);

        var schemaOrigem = await conectorOrigem.DescobrirSchemaTabelaAsync(stringConexaoOrigem, tabelaOrigem);
        var schemaDestino = await conectorDestino.DescobrirSchemaTabelaAsync(stringConexaoDestino, tabelaDestino);

        var resultado = new ResultadoAutoMapeamento();

        // Conjunto de colunas destino já mapeadas
        var colunasDestinoMapeadas = new HashSet<string>();

        // 1. Mapeamentos exatos (nomes iguais, case-insensitive)
        foreach (var colunaOrigem in schemaOrigem.Colunas)
        {
            var colunaDestino = schemaDestino.Colunas.FirstOrDefault(c =>
                c.Nome.Equals(colunaOrigem.Nome, StringComparison.OrdinalIgnoreCase));

            if (colunaDestino != null)
            {
                resultado.MapeamentosExatos.Add(new MapeamentoDto
                {
                    ColunaOrigem = colunaOrigem.Nome,
                    ColunaDestino = colunaDestino.Nome,
                    TipoDestino = colunaDestino.TipoDados,
                    Ignorada = false
                });

                colunasDestinoMapeadas.Add(colunaDestino.Nome);
            }
        }

        // 2. Sugestões por similaridade (para colunas não mapeadas exatamente)
        foreach (var colunaOrigem in schemaOrigem.Colunas)
        {
            // Se já foi mapeada exatamente, pula
            if (resultado.MapeamentosExatos.Any(m => m.ColunaOrigem == colunaOrigem.Nome))
                continue;

            // Encontra coluna mais similar que ainda não foi mapeada
            var melhorSugestao = schemaDestino.Colunas
                .Where(c => !colunasDestinoMapeadas.Contains(c.Nome))
                .Select(c => new
                {
                    Coluna = c,
                    Similaridade = SimilaridadeStrings.CalcularSimilaridade(colunaOrigem.Nome, c.Nome)
                })
                .Where(x => x.Similaridade >= 60) // Mínimo 60% de similaridade
                .OrderByDescending(x => x.Similaridade)
                .FirstOrDefault();

            if (melhorSugestao != null)
            {
                resultado.Sugestoes.Add(new SugestaoMapeamento
                {
                    ColunaOrigem = colunaOrigem.Nome,
                    ColunaDestinoSugerida = melhorSugestao.Coluna.Nome,
                    Similaridade = melhorSugestao.Similaridade
                });
            }
            else
            {
                // Sem sugestão viável
                resultado.ColunasSemMapeamento.Add(colunaOrigem.Nome);
            }
        }

        return resultado;
    }

    /// <summary>
    /// Obtém mapeamentos de uma tabela
    /// </summary>
    public async Task<List<MapeamentoDto>> ObterMapeamentosAsync(Guid tabelaJobId)
    {
        var job = await _jobRepositorio.ObterCompletoAsync(tabelaJobId);
        if (job == null)
            return new List<MapeamentoDto>();

        var tabelaJob = job.Tabelas.FirstOrDefault(t => t.Id == tabelaJobId);
        if (tabelaJob == null)
            return new List<MapeamentoDto>();

        return tabelaJob.Mapeamentos.Select(m => new MapeamentoDto
        {
            Id = m.Id,
            ColunaOrigem = m.ColunaOrigem,
            ColunaDestino = m.ColunaDestino,
            TipoDestino = m.TipoDestino,
            Ignorada = m.Ignorada,
            ValorConstante = m.ValorConstante,
            Regras = m.Regras.Select(r => new RegraDto 
            {
                Id = r.Id,
                TipoRegra = r.TipoRegra,
                Ordem = r.Ordem,
                Parametros = r.ParametrosJson
            }).OrderBy(r => r.Ordem).ToList()
        }).ToList();
    }
}
