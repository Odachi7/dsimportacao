using DSI.Aplicacao.DTOs;
using DSI.Conectores.Abstracoes.Interfaces;
using DSI.Persistencia.Repositorios;
using System.Data;
using System.Text.Json;
using LookupEntidade = DSI.Dominio.Entidades.Lookup;
using DSI.Dominio.Enums;

namespace DSI.Aplicacao.Servicos;

/// <summary>
/// Serviço para gerenciamento de Lookups (De/Para)
/// </summary>
public class ServicoLookup
{
    private readonly IJobRepositorio _jobRepositorio;
    private readonly ServicoConexao _servicoConexao;
    private readonly Dictionary<Guid, Dictionary<string, string>> _cacheLookups = new();

    public ServicoLookup(
        IJobRepositorio jobRepositorio,
        ServicoConexao servicoConexao)
    {
        _jobRepositorio = jobRepositorio;
        _servicoConexao = servicoConexao;
    }

    /// <summary>
    /// Cria um novo Lookup
    /// </summary>
    public async Task<LookupDto> CriarAsync(CriarLookupDto dto)
    {
        // Validações
        if (string.IsNullOrWhiteSpace(dto.Nome))
            throw new ArgumentException("Nome do Lookup é obrigatório");

        if (dto.Tipo == TipoLookup.ListaLocal && (dto.ValoresLocais == null || !dto.ValoresLocais.Any()))
            throw new ArgumentException("Lookup local deve ter valores");

        if (dto.Tipo == TipoLookup.TabelaBancoDados)
        {
            if (dto.ConexaoBancoId == null)
                throw new ArgumentException("Conexão do banco é obrigatória para Lookup de banco");
            
            if (string.IsNullOrWhiteSpace(dto.TabelaBanco) ||
                string.IsNullOrWhiteSpace(dto.ColunaChave) ||
                string.IsNullOrWhiteSpace(dto.ColunaValor))
                throw new ArgumentException("Tabela, coluna chave e coluna valor são obrigatórias");
        }

        // Cria configuração JSON baseada no tipo
        var configuracao = dto.Tipo == TipoLookup.ListaLocal
            ? JsonSerializer.Serialize(new { valores = dto.ValoresLocais })
            : JsonSerializer.Serialize(new
            {
                conexaoId = dto.ConexaoBancoId,
                tabela = dto.TabelaBanco,
                colunaChave = dto.ColunaChave,
                colunaValor = dto.ColunaValor
            });

        var lookup = new LookupEntidade
        {
            Id = Guid.NewGuid(),
            MapeamentoId = dto.MapeamentoId,
            Nome = dto.Nome,
            Tipo = dto.Tipo,
            ConfiguracaoJson = configuracao,
            ValorPadrao = dto.ValorPadrao
        };

        // Adiciona lookup ao mapeamento
        var job = await _jobRepositorio.ObterCompletoAsync(dto.MapeamentoId);
        if (job == null)
            throw new InvalidOperationException("Job não encontrado");

        // Encontra mapeamento e adiciona lookup
        foreach (var tabela in job.Tabelas)
        {
            var mapeamento = tabela.Mapeamentos.FirstOrDefault(m => m.Id == dto.MapeamentoId);
            if (mapeamento != null)
            {
                mapeamento.Lookups.Add(lookup);
                break;
            }
        }

        await _jobRepositorio.AtualizarAsync(job);
        await _jobRepositorio.SalvarAsync();

        // Atualiza cache se for local
        if (dto.Tipo == TipoLookup.ListaLocal && dto.ValoresLocais != null)
        {
            _cacheLookups[lookup.Id] = new Dictionary<string, string>(dto.ValoresLocais);
        }

        return MapearParaDto(lookup, dto.ValoresLocais?.Count ?? 0);
    }

    /// <summary>
    /// Resolve um valor usando Lookup
    /// </summary>
    public async Task<string?> ResolverAsync(Guid lookupId, string chave, IConector? conector = null)
    {
        var job = await _jobRepositorio.ObterCompletoAsync(lookupId);
        if (job == null)
            return null;

        LookupEntidade? lookup = null;
        foreach (var tabela in job.Tabelas)
        {
            foreach (var mapeamento in tabela.Mapeamentos)
            {
                lookup = mapeamento.Lookups.FirstOrDefault(l => l.Id == lookupId);
                if (lookup != null) break;
            }
            if (lookup != null) break;
        }

        if (lookup == null)
            return null;

        if (lookup.Tipo == TipoLookup.ListaLocal)
        {
            return ResolverLocal(lookup, chave);
        }
        else if (lookup.Tipo == TipoLookup.TabelaBancoDados && conector != null)
        {
            return await ResolverBancoAsync(lookup, chave, conector);
        }

        return lookup.ValorPadrao;
    }

    /// <summary>
    /// Resolve valor de lookup local
    /// </summary>
    private string? ResolverLocal(LookupEntidade lookup, string chave)
    {
        // Tenta usar cache
        if (_cacheLookups.TryGetValue(lookup.Id, out var cache))
        {
            return cache.TryGetValue(chave, out var valor) ? valor : lookup.ValorPadrao;
        }

        // Carrega do JSON
        var config = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(lookup.ConfiguracaoJson);
        if (config != null && config.TryGetValue("valores", out var valores))
        {
            _cacheLookups[lookup.Id] = valores;
            return valores.TryGetValue(chave, out var valor) ? valor : lookup.ValorPadrao;
        }

        return lookup.ValorPadrao;
    }

    /// <summary>
    /// Resolve valor consultando banco de dados
    /// </summary>
    private async Task<string?> ResolverBancoAsync(LookupEntidade lookup, string chave, IConector conector)
    {
        var config = JsonSerializer.Deserialize<Dictionary<string, object>>(lookup.ConfiguracaoJson);
        if (config == null)
            return lookup.ValorPadrao;

        var conexaoId = Guid.Parse(config["conexaoId"].ToString()!);
        var tabela = config["tabela"].ToString();
        var colunaChave = config["colunaChave"].ToString();
        var colunaValor = config["colunaValor"].ToString();

        var stringConexao = await _servicoConexao.ObterStringConexaoAsync(conexaoId);
        var conexao = conector.CriarConexao(stringConexao);

        try
        {
            conexao.Open();

            var sql = $"SELECT {colunaValor} FROM {tabela} WHERE {colunaChave} = @chave LIMIT 1";
            var parametros = new Dictionary<string, object> { { "@chave", chave } };

            using var reader = await conector.ExecutarConsultaAsync(conexao, sql, parametros);
            
            if (await Task.Run(() => reader.Read()))
            {
                return reader.GetString(0);
            }

            return lookup.ValorPadrao;
        }
        finally
        {
            conexao.Dispose();
        }
    }

    /// <summary>
    /// Limpa cache de lookups
    /// </summary>
    public void LimparCache()
    {
        _cacheLookups.Clear();
    }

    private LookupDto MapearParaDto(LookupEntidade lookup, int quantidadeItens)
    {
        return new LookupDto
        {
            Id = lookup.Id,
            MapeamentoId = lookup.MapeamentoId,
            Nome = lookup.Nome,
            Tipo = lookup.Tipo,
            QuantidadeItens = quantidadeItens,
            ValorPadrao = lookup.ValorPadrao
        };
    }
}
