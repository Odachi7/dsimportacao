using CommunityToolkit.Mvvm.ComponentModel;

namespace DSI.Desktop.ViewModels;

public partial class JobWizardViewModel : ObservableObject
{
    [ObservableProperty]
    private int _passoAtual = 1;

    [ObservableProperty]
    private string _nomeJob = string.Empty;

    // TODO: Adicionar propriedades para os 7 passos do wizard
}
