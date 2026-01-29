using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DSI.Aplicacao.Servicos;
using System.Collections.ObjectModel;
using System.Windows;

namespace DSI.Desktop.ViewModels;

using CommunityToolkit.Mvvm.Messaging;
using DSI.Desktop.Messages;
using DSI.Dominio.Enums;
using DSI.Aplicacao.DTOs;

public partial class HistoricoViewModel : ObservableObject
{
    private readonly ServicoHistorico _servicoHistorico;

    [ObservableProperty]
    private DateTime? _dataInicio = DateTime.Today.AddDays(-7);

    [ObservableProperty]
    private DateTime? _dataFim = DateTime.Today;

    [ObservableProperty]
    private StatusExecucao? _statusSelecionado;

    [ObservableProperty]
    private ResumoExecucaoDto? _execucaoSelecionada;

    [ObservableProperty]
    private ObservableCollection<ResumoExecucaoDto> _execucoes = new();

    // Coleção para ComboBox de Status
    public ObservableCollection<StatusExecucao> StatusOpcoes { get; } = new(Enum.GetValues<StatusExecucao>());

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
            var historico = await _servicoHistorico.ListarHistoricoAsync(
                dataInicio: DataInicio,
                dataFim: DataFim?.AddDays(1).AddSeconds(-1), // Fim do dia
                status: StatusSelecionado
            );
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

    [RelayCommand]
    private void VerDetalhes(ResumoExecucaoDto execucao)
    {
        if (execucao == null) return;
        
        WeakReferenceMessenger.Default.Send(new VerDetalhesHistoricoMessage(execucao.ExecucaoId));
    }

    [RelayCommand]
    private void LimparFiltros()
    {
        DataInicio = null;
        DataFim = null;
        StatusSelecionado = null;
        _ = CarregarHistoricoAsync();
    }
}
