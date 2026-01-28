using DSI.Aplicacao.DTOs;
using DSI.Conectores.Abstracoes.Interfaces;
using DSI.Conectores.Abstracoes.Modelos;
using DSI.Dominio.Entidades;
using DSI.Persistencia.Repositorios;
using DSI.Seguranca.Criptografia;

namespace DSI.Aplicacao.Servicos;

/// <summary>
/// Serviço de aplicação para gerenciamento de conexões
/// </summary>
public class ServicoConexao
{
    private readonly IConexaoRepositorio _repositorio;
    private readonly ServicoCriptografia _criptografia;
    private readonly Dictionary<Guid, IConector> _conectoresCache;

    public ServicoConexao(
        IConexaoRepositorio repositorio,
        ServicoCriptografia criptografia)
    {
        _repositorio = repositorio;
        _criptografia = criptografia;
        _conectoresCache = new Dictionary<Guid, IConector>();
    }

    /// <summary>
    /// Cria uma nova conexão
    /// </summary>
    public async Task<ConexaoDto> CriarAsync(CriarConexaoDto dto)
    {
        // Validações
        if (string.IsNullOrWhiteSpace(dto.Nome))
            throw new ArgumentException("Nome da conexão é obrigatório");

        if (string.IsNullOrWhiteSpace(dto.StringConexao))
            throw new ArgumentException("String de conexão é obrigatória");

        // Verifica se já existe conexão com o mesmo nome
        if (await _repositorio.ExistePorNomeAsync(dto.Nome))
            throw new InvalidOperationException($"Já existe uma conexão com o nome '{dto.Nome}'");

        // Cria entidade
        var conexao = new Conexao
        {
            Id = Guid.NewGuid(),
            Nome = dto.Nome,
            TipoBanco = dto.TipoBanco,
            ModoConexao = dto.ModoConexao,
            StringConexaoCriptografada = _criptografia.Criptografar(dto.StringConexao),
            CriadoEm = DateTime.UtcNow,
            AtualizadoEm = DateTime.UtcNow
        };

        await _repositorio.AdicionarAsync(conexao);
        await _repositorio.SalvarAsync();

        return MapearParaDto(conexao);
    }

    /// <summary>
    /// Atualiza uma conexão existente
    /// </summary>
    public async Task<ConexaoDto> AtualizarAsync(AtualizarConexaoDto dto)
    {
        var conexao = await _repositorio.ObterPorIdAsync(dto.Id);
        if (conexao == null)
            throw new InvalidOperationException("Conexão não encontrada");

        // Valida nome único
        var conexaoComMesmoNome = await _repositorio.ObterPorNomeAsync(dto.Nome);
        if (conexaoComMesmoNome != null && conexaoComMesmoNome.Id != dto.Id)
            throw new InvalidOperationException($"Já existe uma conexão com o nome '{dto.Nome}'");

        // Atualiza campos
        conexao.Nome = dto.Nome;
        conexao.TipoBanco = dto.TipoBanco;
        conexao.ModoConexao = dto.ModoConexao;

        // Atualiza string de conexão se fornecida
        if (!string.IsNullOrWhiteSpace(dto.StringConexao))
        {
            conexao.StringConexaoCriptografada = _criptografia.Criptografar(dto.StringConexao);
        }

        conexao.AtualizadoEm = DateTime.UtcNow;

        await _repositorio.AtualizarAsync(conexao);
        await _repositorio.SalvarAsync();

        // Limpa cache de conector
        _conectoresCache.Remove(conexao.Id);

        return MapearParaDto(conexao);
    }

    /// <summary>
    /// Obtém todas as conexões
    /// </summary>
    public async Task<IEnumerable<ConexaoDto>> ObterTodasAsync()
    {
        var conexoes = await _repositorio.ObterTodosAsync();
        return conexoes.Select(MapearParaDto);
    }

    /// <summary>
    /// Obtém uma conexão por ID
    /// </summary>
    public async Task<ConexaoDto?> ObterPorIdAsync(Guid id)
    {
        var conexao = await _repositorio.ObterPorIdAsync(id);
        return conexao != null ? MapearParaDto(conexao) : null;
    }

    /// <summary>
    /// Remove uma conexão
    /// </summary>
    public async Task RemoverAsync(Guid id)
    {
        // TODO: Validar se não há jobs usando esta conexão
        await _repositorio.RemoverAsync(id);
        await _repositorio.SalvarAsync();
        _conectoresCache.Remove(id);
    }

    /// <summary>
    /// Testa a conexão
    /// </summary>
    public async Task<ResultadoTesteConexao> TestarConexaoAsync(Guid id, IConector conector)
    {
        var conexao = await _repositorio.ObterPorIdAsync(id);
        if (conexao == null)
            throw new InvalidOperationException("Conexão não encontrada");

        var stringConexao = _criptografia.Descriptografar(conexao.StringConexaoCriptografada);
        return await conector.TestarConexaoAsync(stringConexao);
    }

    /// <summary>
    /// Obtém string de conexão descriptografada (uso interno)
    /// </summary>
    public async Task<string> ObterStringConexaoAsync(Guid id)
    {
        var conexao = await _repositorio.ObterPorIdAsync(id);
        if (conexao == null)
            throw new InvalidOperationException("Conexão não encontrada");

        return _criptografia.Descriptografar(conexao.StringConexaoCriptografada);
    }

    /// <summary>
    /// Descobre schema de uma conexão
    /// </summary>
    public async Task<IEnumerable<InfoTabela>> DescobrirTabelasAsync(Guid id, IConector conector)
    {
        var stringConexao = await ObterStringConexaoAsync(id);
        return await conector.DescobrirTabelasAsync(stringConexao);
    }

    private ConexaoDto MapearParaDto(Conexao conexao)
    {
        return new ConexaoDto
        {
            Id = conexao.Id,
            Nome = conexao.Nome,
            TipoBanco = conexao.TipoBanco,
            ModoConexao = conexao.ModoConexao,
            CriadoEm = conexao.CriadoEm,
            AtualizadoEm = conexao.AtualizadoEm,
            TemStringConexao = !string.IsNullOrEmpty(conexao.StringConexaoCriptografada)
        };
    }
}
