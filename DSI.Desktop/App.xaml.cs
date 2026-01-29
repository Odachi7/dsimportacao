using DSI.Aplicacao.Servicos;
using DSI.Desktop.ViewModels;
using DSI.Desktop.Views;
using DSI.Motor.ETL;
using DSI.Persistencia.Contexto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

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
                services.AddScoped<GerenciadorCheckpoint>();

                // ViewModels
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<ConexoesViewModel>();
                services.AddTransient<JobWizardViewModel>();
                services.AddTransient<MonitorExecucaoViewModel>();
                services.AddTransient<HistoricoViewModel>();

                // Views (Windows)
                services.AddTransient<MainWindow>();
                services.AddTransient<DashboardView>();
                services.AddTransient<ConexoesView>();
                services.AddTransient<JobWizardWindow>();
                services.AddTransient<MonitorExecucaoView>();
                services.AddTransient<HistoricoView>();
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
