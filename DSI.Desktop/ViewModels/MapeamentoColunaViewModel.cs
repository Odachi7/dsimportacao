using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace DSI.Desktop.ViewModels;

public partial class MapeamentoColunaViewModel : ObservableObject
{
    [ObservableProperty]
    private string _colunaOrigem = string.Empty;

    [ObservableProperty]
    private string _tipoOrigem = string.Empty;

    [ObservableProperty]
    private string _colunaDestino = string.Empty;

    [ObservableProperty]
    private string _tipoDestino = string.Empty;

    [ObservableProperty]
    private bool _ignorar;

    [ObservableProperty]
    private bool _chavePrimaria;

    [ObservableProperty]
    private ObservableCollection<RegraViewModel> _regras = new();
}
