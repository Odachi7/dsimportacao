using DSI.Aplicacao.Servicos;
using DSI.Desktop.ViewModels;
using DSI.Desktop.Views;
using DSI.Motor.ETL;
using DSI.Persistencia.Contexto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
using DSI.Persistencia.Repositorios;
using DSI.Seguranca.Criptografia;
using DSI.Dominio.Enums;
using DSI.Conectores.Abstracoes;

namespace DSI.Desktop;

public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // DbContext
                services.AddDbContext<DsiDbContext>(options =>
                    options.UseSqlite("Data Source=dsi.db"));

                // Serviços de Aplicação
                services.AddScoped<ServicoConexao>();
                services.AddScoped<ServicoJob>();
                services.AddScoped<ServicoMapeamento>();
                services.AddScoped<ServicoLookup>();
                services.AddScoped<ServicoExecucao>();
                services.AddScoped<ServicoPreview>();
                services.AddScoped<ServicoHistorico>();

                // Motor ETL
                services.AddScoped<MotorETL>();
                services.AddScoped<MotorETL>();
                services.AddScoped<GerenciadorCheckpoint>();
                services.AddScoped<CamadaExtract>();
                services.AddScoped<CamadaTransform>();
                services.AddScoped<CamadaLoad>();

                // ViewModels
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<ConexoesViewModel>();
                services.AddTransient<JobWizardViewModel>();
                services.AddTransient<MonitorExecucaoViewModel>();
                services.AddTransient<HistoricoViewModel>();
                services.AddTransient<DetalheExecucaoViewModel>();
                services.AddTransient<NovaConexaoViewModel>();

                // Repositórios
                services.AddScoped<IConexaoRepositorio, ConexaoRepositorio>();
                services.AddScoped<IJobRepositorio, JobRepositorio>();
                services.AddScoped<IExecucaoRepositorio, ExecucaoRepositorio>();

                // Segurança
                services.AddScoped<ServicoCriptografia>();

                // Fábrica de Conectores (Singleton pois é stateless/configuração)
                services.AddSingleton<FabricaConectores>(sp =>
                {
                    var fabrica = new FabricaConectores();
                    
                    // Registrar conectores (assumindo que as dlls estão carregadas)
                    // Nota: Idealmente usaríamos reflection ou um plugin system, 
                    // mas para o MVP vamos registrar explictamente os conhecidos.
                    
                    fabrica.Registrar<DSI.Conectores.MySql.ConectorMySql>(TipoBancoDados.MySql);
                    fabrica.Registrar<DSI.Conectores.PostgreSql.ConectorPostgreSql>(TipoBancoDados.PostgreSql);
                    fabrica.Registrar<DSI.Conectores.SqlServer.ConectorSqlServer>(TipoBancoDados.SqlServer);
                    fabrica.Registrar<DSI.Conectores.Firebird.ConectorFirebird>(TipoBancoDados.Firebird);
                    fabrica.Registrar<DSI.Conectores.Odbc.ConectorOdbc>(TipoBancoDados.Odbc);

                    return fabrica;
                });

                // Views (Windows)
                services.AddTransient<MainWindow>();
                services.AddTransient<DashboardView>();
                services.AddTransient<ConexoesView>();
                services.AddTransient<JobWizardWindow>();
                services.AddTransient<MonitorExecucaoView>();
                services.AddTransient<HistoricoView>();
                services.AddTransient<DetalheExecucaoView>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        // Cria/atualiza banco de dados
        using (var scope = _host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DsiDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }
}
