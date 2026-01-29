using DSI.Desktop.Views;
using System.Windows;

namespace DSI.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Mostra Dashboard por padrão
        MostrarDashboard();
    }

    private void BtnDashboard_Click(object sender, RoutedEventArgs e)
    {
        MostrarDashboard();
    }

    private void BtnConexoes_Click(object sender, RoutedEventArgs e)
    {
        ContentArea.Content = new ConexoesView();
    }

    private void BtnJobs_Click(object sender, RoutedEventArgs e)
    {
        MostrarDashboard(); // Jobs estão no dashboard
    }

    private void BtnMonitor_Click(object sender, RoutedEventArgs e)
    {
        ContentArea.Content = new MonitorExecucaoView();
    }

    private void BtnHistorico_Click(object sender, RoutedEventArgs e)
    {
        ContentArea.Content = new HistoricoView();
    }

    private void MostrarDashboard()
    {
        ContentArea.Content = new DashboardView();
    }
}