using DSI.Aplicacao.Servicos;
using DSI.Dominio.Entidades;
using DSI.Dominio.Enums;
using DSI.Motor;
using DSI.Motor.ETL;
using DSI.Persistencia.Repositorios;
using DSI.Persistencia.Contexto;
using DSI.Seguranca.Criptografia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System.Data;
using DSI.Conectores.Abstracoes.Interfaces;
using DSI.Conectores.Abstracoes;
using Moq;

using Microsoft.Data.Sqlite; // Necessário para SqliteConnection

namespace DSI.Testes.Integracao;

public class FluxoCompletoEtlTests
{
    [Fact]
    public async Task ExecutarJob_FluxoCompleto_DeveProcessarDados()
    {
        // 1. Setup DI e Services
        var services = new ServiceCollection();
        
        // Configura conexão compartilhada para manter banco em memória entre escopos
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        services.AddScoped(typeof(ILogger<>), typeof(NullLogger<>));
        
        services.AddScoped<ServicoCriptografia>();
        services.AddScoped<ServicoExecucao>();
        services.AddScoped<CamadaExtract>();
        services.AddScoped(sp => new CamadaTransform(sp));
        services.AddScoped<CamadaLoad>();
        services.AddScoped<GerenciadorCheckpoint>();
        services.AddScoped<MotorETL>();

        // Infra de Banco em Memória com conexão compatilhada
        services.AddDbContext<DsiDbContext>(options => 
            options.UseSqlite(connection));
            
        services.AddScoped<IExecucaoRepositorio, ExecucaoRepositorio>();
        services.AddScoped<IJobRepositorio, JobRepositorio>();
        
        // Fabrica de Conectores
        var fabricaConectores = new FabricaConectores();
        
        var mockConectorOrigem = new Mock<IConector>();
        var mockConectorDestino = new Mock<IConector>();
        var mockConexaoOrigem = new Mock<IDbConnection>();
        var mockConexaoDestino = new Mock<IDbConnection>();

        // Configurar Mocks dos Conectores
        mockConectorOrigem.Setup(c => c.CriarConexao(It.IsAny<string>())).Returns(mockConexaoOrigem.Object);
        mockConectorOrigem.Setup(c => c.Capacidades).Returns(DSI.Conectores.Abstracoes.Enums.CapacidadesConector.Nenhuma);
        
        var mockReader = new Mock<IDataReader>();
        int contadorLeitura = 0;
        mockReader.Setup(r => r.Read()).Returns(() => contadorLeitura++ < 10);
        mockReader.Setup(r => r.GetName(0)).Returns("ID");
        mockReader.Setup(r => r.GetName(1)).Returns("NOME");
        mockReader.Setup(r => r.GetFieldType(0)).Returns(typeof(int));
        mockReader.Setup(r => r.GetFieldType(1)).Returns(typeof(string));
        mockReader.Setup(r => r.GetValue(0)).Returns(() => contadorLeitura);
        mockReader.Setup(r => r.GetValue(1)).Returns(() => $"Nome {contadorLeitura}");
        mockReader.Setup(r => r.FieldCount).Returns(2);

        mockConectorOrigem.Setup(c => c.ExecutarConsultaAsync(It.IsAny<IDbConnection>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync(mockReader.Object);

        mockConectorDestino.Setup(c => c.CriarConexao(It.IsAny<string>())).Returns(mockConexaoDestino.Object);
        mockConexaoDestino.Setup(c => c.Open());
        mockConexaoDestino.Setup(c => c.BeginTransaction()).Returns(new Mock<IDbTransaction>().Object);
        
        mockConectorDestino.Setup(c => c.InserirEmLoteAsync(It.IsAny<IDbConnection>(), It.IsAny<string>(), It.IsAny<DataTable>()))
            .ReturnsAsync(10);
        mockConectorDestino.Setup(c => c.Capacidades).Returns(DSI.Conectores.Abstracoes.Enums.CapacidadesConector.Nenhuma);

        fabricaConectores.Registrar(TipoBancoDados.MySql, () => mockConectorOrigem.Object);
        fabricaConectores.Registrar(TipoBancoDados.PostgreSql, () => mockConectorDestino.Object);
        
        services.AddSingleton(fabricaConectores);

        var provider = services.BuildServiceProvider();

        // 2. Preparar Banco de Dados (Seed)
        var context = provider.GetRequiredService<DsiDbContext>();
        context.Database.EnsureCreated();

        var conexaoOrigem = new Conexao 
        { 
            Id = Guid.NewGuid(), 
            Nome = "Origem",
            StringConexaoCriptografada = "origem_enc", 
            TipoBanco = TipoBancoDados.MySql 
        };
        var conexaoDestino = new Conexao 
        { 
            Id = Guid.NewGuid(), 
            Nome = "Destino",
            StringConexaoCriptografada = "destino_enc", 
            TipoBanco = TipoBancoDados.PostgreSql 
        };

        context.Conexoes.AddRange(conexaoOrigem, conexaoDestino);
        
        var jobId = Guid.NewGuid();
        var tabelaId = Guid.NewGuid();

        var job = new Job 
        { 
            Id = jobId,
            Nome = "Job Teste Integração",
            ConexaoOrigemId = conexaoOrigem.Id,
            ConexaoDestinoId = conexaoDestino.Id,
            EstrategiaConflito = EstrategiaConflito.ApenasInserir,
            TamanhoLote = 10
        };

        var tabela = new TabelaJob
        {
            Id = tabelaId,
            JobId = jobId,
            TabelaOrigem = "Origem",
            TabelaDestino = "Destino",
            Mapeamentos = new List<Mapeamento>
            {
                new Mapeamento { Id = Guid.NewGuid(), TabelaJobId = tabelaId, ColunaOrigem = "ID", ColunaDestino = "Id" },
                new Mapeamento { Id = Guid.NewGuid(), TabelaJobId = tabelaId, ColunaOrigem = "NOME", ColunaDestino = "Nome" }
            }
        };

        job.Tabelas.Add(tabela);
        
        context.Jobs.Add(job);
        await context.SaveChangesAsync();
        
        // 3. Executar
        var servicoExecucao = provider.GetRequiredService<ServicoExecucao>();
        var execucaoId = await servicoExecucao.ExecutarAsync(jobId);

        // Aguarda conclusão (Polling)
        Execucao? execucao = null;
        int maxRetries = 100; // 10 segundos
        
        while (maxRetries > 0)
        {
            await Task.Delay(100);
            context.ChangeTracker.Clear(); // Limpa cache para pegar dados frescos
            execucao = await context.Execucoes.Include(e => e.Erros).FirstOrDefaultAsync(e => e.Id == execucaoId);
            
            if (execucao != null && execucao.Status != StatusExecucao.Executando)
                break;
                
            maxRetries--;
        }

        // 4. Verification Check First to debug stuck jobs
        mockConectorOrigem.Verify(c => c.ExecutarConsultaAsync(
            It.IsAny<IDbConnection>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>()), 
            Times.AtLeastOnce, "Falha na verificação de execução de consulta na origem");

        mockConectorDestino.Verify(c => c.InserirEmLoteAsync(
            It.IsAny<IDbConnection>(), 
            "Destino", 
            It.IsAny<DataTable>()),
            Times.AtLeastOnce, "Falha na verificação de inserção em lote no destino");

        // Assert Final Status
        Assert.NotNull(execucao);
        Assert.True(execucao!.Status == StatusExecucao.Concluido, 
            $"Status final: {execucao.Status}. Erros: {string.Join("; ", execucao.Erros.Select(e => e.Mensagem))}");

        // Cleanup
        if (provider is IDisposable disposable)
            disposable.Dispose();
        
        connection.Close();
        connection.Dispose();
    }
}
