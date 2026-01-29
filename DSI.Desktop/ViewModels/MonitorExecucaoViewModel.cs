using CommunityToolkit.Mvvm.ComponentModel;

namespace DSI.Desktop.ViewModels;

public partial class MonitorExecucaoViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusAtual = "Nenhuma execução ativa";

    // TODO: Adicionar lógica de monitoramento em tempo real
}
