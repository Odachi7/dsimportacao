using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Messaging;

namespace DSI.Desktop.Views;

public partial class MonitorExecucaoView : UserControl
{
    public MonitorExecucaoView()
    {
        InitializeComponent();
    }

    private void BtnVoltar_Click(object sender, RoutedEventArgs e)
    {
        // Envia mensagem tipada
        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.Send(new DSI.Desktop.Messages.VoltarDashboardMessage());
    }
}
