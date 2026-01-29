using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace DSI.Desktop.ViewModels;

public partial class MapeamentoTabelaViewModel : ObservableObject
{
    [ObservableProperty]
    private string _tabelaOrigem = string.Empty;

    [ObservableProperty]
    private string _tabelaDestino = string.Empty;

    [ObservableProperty]
    private ObservableCollection<MapeamentoColunaViewModel> _colunas = new();
}
