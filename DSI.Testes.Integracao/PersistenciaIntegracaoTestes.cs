using DSI.Dominio.Entidades;
using DSI.Dominio.Enums;
using DSI.Persistencia.Contexto;
using DSI.Persistencia.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace DSI.Testes.Integracao.Persistencia;

/// <summary>
/// Testes de integração da camada de persistência
/// </summary>
public class PersistenciaIntegracaoTestes : IDisposable
{
    private readonly DsiDbContext _contexto;
    private readonly IConexaoRepositorio _conexaoRepo;
    private readonly IJobRepositorio _jobRepo;

    public PersistenciaIntegracaoTestes()
    {
        // Cria banco de dados em memória para testes
        var options = new DbContextOptionsBuilder<DsiDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _contexto = new DsiDbContext(options);
        _contexto.Database.OpenConnection();
        _contexto.Database.EnsureCreated();

        _conexaoRepo = new ConexaoRepositorio(_contexto);
        _jobRepo = new JobRepositorio(_contexto);
    }

    [Fact]
    public async Task DeveCriarEObterConexao()
    {
        // Arrange
        var conexao = new Conexao
        {
            Id = Guid.NewGuid(),
            Nome = "MySQL Teste",
            TipoBanco = TipoBancoDados.MySql,
            ModoConexao = ModoConexao.Nativo,
            StringConexaoCriptografada = "Server=localhost;Database=teste",
            CriadoEm = DateTime.UtcNow,
            AtualizadoEm = DateTime.UtcNow
        };

        // Act
        await _conexaoRepo.AdicionarAsync(conexao);
        await _conexaoRepo.SalvarAsync();
        var resultado = await _conexaoRepo.ObterPorIdAsync(conexao.Id);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal("MySQL Teste", resultado.Nome);
        Assert.Equal(TipoBancoDados.MySql, resultado.TipoBanco);
    }

    [Fact]
    public async Task DeveCriarJobComTabelasEMapeamentos()
    {
        // Arrange
        var job = new Job
        {
            Id = Guid.NewGuid(),
            Nome = "Job Teste",
            ConexaoOrigemId = Guid.NewGuid(),
            ConexaoDestinoId = Guid.NewGuid(),
            Modo = ModoImportacao.Completo,
            TamanhoLote = 500,
            PoliticaErro = PoliticaErro.PularLinhasInvalidas,
            EstrategiaConflito = EstrategiaConflito.ApenasInserir,
            CriadoEm = DateTime.UtcNow,
            AtualizadoEm = DateTime.UtcNow
        };

        var tabela = new TabelaJob
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            TabelaOrigem = "clientes",
            TabelaDestino = "customers",
            OrdemExecucao = 1
        };

        var mapeamento = new Mapeamento
        {
            Id = Guid.NewGuid(),
            TabelaJobId = tabela.Id,
            ColunaOrigem = "nome",
            ColunaDestino = "name",
            TipoDestino = "VARCHAR",
            Ignorada = false
        };

        tabela.Mapeamentos.Add(mapeamento);
        job.Tabelas.Add(tabela);

        // Act
        await _jobRepo.AdicionarAsync(job);
        await _jobRepo.SalvarAsync();
        var resultado = await _jobRepo.ObterCompletoAsync(job.Id);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal("Job Teste", resultado.Nome);
        Assert.Single(resultado.Tabelas);
        Assert.Equal("clientes", resultado.Tabelas.First().TabelaOrigem);
        Assert.Single(resultado.Tabelas.First().Mapeamentos);
        Assert.Equal("nome", resultado.Tabelas.First().Mapeamentos.First().ColunaOrigem);
    }

    [Fact]
    public async Task DeveListarConexoesPorTipo()
    {
        // Arrange
        var conexao1 = new Conexao
        {
            Id = Guid.NewGuid(),
            Nome = "MySQL 1",
            TipoBanco = TipoBancoDados.MySql,
            ModoConexao = ModoConexao.Nativo,
            StringConexaoCriptografada = "test1",
            CriadoEm = DateTime.UtcNow,
            AtualizadoEm = DateTime.UtcNow
        };

        var conexao2 = new Conexao
        {
            Id = Guid.NewGuid(),
            Nome = "Firebird 1",
            TipoBanco = TipoBancoDados.Firebird,
            ModoConexao = ModoConexao.Nativo,
            StringConexaoCriptografada = "test2",
            CriadoEm = DateTime.UtcNow,
            AtualizadoEm = DateTime.UtcNow
        };

        await _conexaoRepo.AdicionarAsync(conexao1);
        await _conexaoRepo.AdicionarAsync(conexao2);
        await _conexaoRepo.SalvarAsync();

        // Act
        var mysqlConexoes = await _conexaoRepo.ObterPorTipoBancoAsync(TipoBancoDados.MySql);

        // Assert
        Assert.Single(mysqlConexoes);
        Assert.Equal("MySQL 1", mysqlConexoes.First().Nome);
    }

    public void Dispose()
    {
        _contexto.Database.CloseConnection();
        _contexto.Dispose();
    }
}
