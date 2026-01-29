using CommunityToolkit.Mvvm.ComponentModel;

namespace DSI.Desktop.ViewModels;

public partial class TabelaSelecaoViewModel : ObservableObject
{
    [ObservableProperty]
    private string _nome = string.Empty;

    [ObservableProperty]
    private string _tipo = string.Empty; // TABLE ou VIEW

    [ObservableProperty]
    private bool _selecionada;

    [ObservableProperty]
    private long _estimativaLinhas;
}
