using DSI.Desktop.Views;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using DSI.Desktop.Messages;
using DSI.Desktop.ViewModels;

namespace DSI.Desktop;

public partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider;

    public MainWindow(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        
        // Registra listeners de navegação
        RegistrarMensagens();

        // Mostra Dashboard por padrão
        MostrarDashboard();
    }

    private void RegistrarMensagens()
    {
        // Navegar para Monitor e Iniciar Job
        WeakReferenceMessenger.Default.Register<ExecutarJobMessage>(this, async (r, m) =>
        {
            var view = _serviceProvider.GetRequiredService<MonitorExecucaoView>();
            var viewModel = _serviceProvider.GetRequiredService<MonitorExecucaoViewModel>(); // View injetada não tem VM, VM é transient
            
            // Associa VM à View (poderia ser feito na View se usar DataTemplate)
            view.DataContext = viewModel;
            ContentArea.Content = view;

            await viewModel.IniciarJobAsync(m.JobId);
        });

        // Ver Detalhes
        WeakReferenceMessenger.Default.Register<VerDetalhesHistoricoMessage>(this, async (r, m) =>
        {
            var view = _serviceProvider.GetRequiredService<DetalheExecucaoView>();
            var viewModel = _serviceProvider.GetRequiredService<DetalheExecucaoViewModel>();

            view.DataContext = viewModel;
            ContentArea.Content = view;

            await viewModel.CarregarDetalhesAsync(m.ExecucaoId);
        });

        // Navegar para Histórico
        WeakReferenceMessenger.Default.Register<NavegarParaHistoricoMessage>(this, (r, m) =>
        {
            MostrarHistorico();
        });
    }

    private void Navegar<TView, TViewModel>() where TView : System.Windows.Controls.UserControl
    {
        var view = _serviceProvider.GetRequiredService<TView>();
        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
        view.DataContext = viewModel;
        ContentArea.Content = view;
    }

    private void MostrarHistorico()
    {
        Navegar<HistoricoView, HistoricoViewModel>();
    }

    private void BtnDashboard_Click(object sender, RoutedEventArgs e)
    {
        MostrarDashboard();
    }

    private void BtnConexoes_Click(object sender, RoutedEventArgs e)
    {
        Navegar<ConexoesView, ConexoesViewModel>();
    }

    private void BtnJobs_Click(object sender, RoutedEventArgs e)
    {
        MostrarDashboard(); 
    }

    private void BtnMonitor_Click(object sender, RoutedEventArgs e)
    {
        Navegar<MonitorExecucaoView, MonitorExecucaoViewModel>();
    }

    private void BtnHistorico_Click(object sender, RoutedEventArgs e)
    {
        MostrarHistorico();
    }

    private void MostrarDashboard()
    {
        Navegar<DashboardView, DashboardViewModel>();
    }
}