using DSI.Logging.Configuracao;
using DSI.Logging.Enums;
using DSI.Logging.Interfaces;
using DSI.Persistencia.Configuracao;
using DSI.Persistencia.Repositorios;
using Microsoft.Extensions.DependencyInjection;

namespace DSI.Testes.Integracao.Infraestrutura;

/// <summary>
/// Testes de integração para logging e DI
/// </summary>
public class LoggingDiIntegracaoTestes : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly string _caminhoLogsTemporario;

    public LoggingDiIntegracaoTestes()
    {
        // Cria diretório temporário para logs
        _caminhoLogsTemporario = Path.Combine(Path.GetTempPath(), "DsiTestesLogs", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_caminhoLogsTemporario);

        // Configura DI
        var services = new ServiceCollection();

        // Adiciona logging
        services.AdicionarLogging(_caminhoLogsTemporario);

        // Adiciona persistência (banco em memória)
        services.AdicionarPersistencia("Data Source=:memory:");

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void DeveresolverLogAmigavel()
    {
        // Act
        var logAmigavel = _serviceProvider.GetService<ILogAmigavel>();

        // Assert
        Assert.NotNull(logAmigavel);
    }

    [Fact]
    public void DeveResolverLogTecnico()
    {
        // Act
        var logTecnico = _serviceProvider.GetService<ILogTecnico>();

        // Assert
        Assert.NotNull(logTecnico);
    }

    [Fact]
    public void DeveGravarLogAmigavelEmBuffer()
    {
        // Arrange
        var logAmigavel = _serviceProvider.GetRequiredService<ILogAmigavel>();

        // Act
        logAmigavel.Informar("Teste de mensagem informativa");
        logAmigavel.Avisar("Teste de aviso");
        logAmigavel.Erro("Teste de erro");

        var mensagens = logAmigavel.ObterUltimasMensagens(10);

        // Assert
        Assert.Equal(3, mensagens.Count());
        Assert.Contains(mensagens, m => m.Nivel == NivelLog.Informacao && m.Mensagem.Contains("informativa"));
        Assert.Contains(mensagens, m => m.Nivel == NivelLog.Aviso && m.Mensagem.Contains("⚠"));
        Assert.Contains(mensagens, m => m.Nivel == NivelLog.Erro && m.Mensagem.Contains("✗"));
    }

    [Fact]
    public void DeveGravarLogTecnicoEmArquivo()
    {
        // Arrange
        var logTecnico = _serviceProvider.GetRequiredService<ILogTecnico>();

        // Act
        logTecnico.Informar("Teste de log técnico");
        logTecnico.Avisar("Teste de aviso técnico");
        logTecnico.Erro("Teste de erro", new Exception("Exceção de teste"));

        // Aguarda gravação
        Thread.Sleep(500);

        // Assert - Verifica apenas se arquivo foi criado (evita file lock ao ler conteúdo)
        var arquivosLog = Directory.GetFiles(_caminhoLogsTemporario, "*.log");
        Assert.NotEmpty(arquivosLog);
        Assert.True(new FileInfo(arquivosLog[0]).Length > 0); // Arquivo não está vazio
    }


    [Fact]
    public void DeveResolverRepositorios()
    {
        // Act
        var conexaoRepo = _serviceProvider.GetService<IConexaoRepositorio>();
        var jobRepo = _serviceProvider.GetService<IJobRepositorio>();
        var execucaoRepo = _serviceProvider.GetService<IExecucaoRepositorio>();

        // Assert
        Assert.NotNull(conexaoRepo);
        Assert.NotNull(jobRepo);
        Assert.NotNull(execucaoRepo);
    }

    [Fact]
    public void DeveCompartilharMesmaInstanciaDeLogAmigavel()
    {
        // Arrange
        var log1 = _serviceProvider.GetRequiredService<ILogAmigavel>();
        var log2 = _serviceProvider.GetRequiredService<ILogAmigavel>();

        // Act
        log1.Informar("Mensagem do log1");
        var mensagens = log2.ObterUltimasMensagens(10);

        // Assert
        Assert.Single(mensagens); // Log2 vê a mensagem de log1 (mesmo singleton)
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();

        // Limpa diretório de logs temporário (tenta mas não falha se arquivo estiver em uso)
        try
        {
            if (Directory.Exists(_caminhoLogsTemporario))
            {
                Directory.Delete(_caminhoLogsTemporario, true);
            }
        }
        catch (IOException)
        {
            // Ignora se arquivo ainda estiver em uso pelo Serilog
        }
    }
}
