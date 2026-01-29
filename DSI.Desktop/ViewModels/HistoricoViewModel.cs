using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DSI.Aplicacao.Servicos;
using System.Collections.ObjectModel;
using System.Windows;

namespace DSI.Desktop.ViewModels;

public partial class HistoricoViewModel : ObservableObject
{
    private readonly ServicoHistorico _servicoHistorico;

    [ObservableProperty]
    private ObservableCollection<ResumoExecucaoDto> _execucoes = new();

    public HistoricoViewModel(ServicoHistorico servicoHistorico)
    {
        _servicoHistorico = servicoHistorico;
        _ = CarregarHistoricoAsync();
    }

    [RelayCommand]
    private async Task CarregarHistoricoAsync()
    {
        try
        {
            var historico = await _servicoHistorico.ListarHistoricoAsync();
            Execucoes.Clear();
            foreach (var exec in historico)
            {
                Execucoes.Add(exec);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao carregar histórico: {ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
