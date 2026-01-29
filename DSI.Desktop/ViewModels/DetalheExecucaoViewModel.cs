using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DSI.Aplicacao.Servicos;
using DSI.Desktop.Messages;
using System.Collections.ObjectModel;
using System.Windows;

namespace DSI.Desktop.ViewModels;

public partial class DetalheExecucaoViewModel : ObservableObject
{
    private readonly ServicoHistorico _servicoHistorico;
    private Guid _execucaoId;

    [ObservableProperty]
    private DetalhesExecucaoDto? _detalhes;

    [ObservableProperty]
    private bool _carregando;

    public ObservableCollection<ErroExecucaoDto> Erros { get; } = new();
    public ObservableCollection<EstatisticaTabelaDto> Estatisticas { get; } = new();

    public DetalheExecucaoViewModel(ServicoHistorico servicoHistorico)
    {
        _servicoHistorico = servicoHistorico ?? throw new ArgumentNullException(nameof(servicoHistorico));
    }

    public async Task CarregarDetalhesAsync(Guid execucaoId)
    {
        _execucaoId = execucaoId;
        try
        {
            Carregando = true;
            Detalhes = await _servicoHistorico.ObterDetalhesExecucaoAsync(execucaoId);

            Estatisticas.Clear();
            Erros.Clear();

            if (Detalhes != null)
            {
                foreach (var stat in Detalhes.Estatisticas) Estatisticas.Add(stat);
                foreach (var erro in Detalhes.Erros) Erros.Add(erro);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao carregar detalhes: {ex.Message}", "Erro", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            Carregando = false;
        }
    }

    [RelayCommand]
    private void Voltar()
    {
        // Volta para o Histórico (que é a view anterior, ou dashboard se for direto)
        // Para simplificar, voltamos para o Dashboard ou Histórico
        // Vamos assumir que voltar vai para Histórico se possível ou Dashboard
        // Por enquanto, enviamos VoltarDashboardMessage (talvez devêssemos ter VoltarHistoricoMessage)
        
        // Vamos usar uma mensagem genérica de voltar ou específica
        // Como a navigation stack é manual no MainWindow, vamos mandar voltar para Dashboard por padrão ou criar mensagem nova
        // Vamos criar uma mensagem 'NavegarParaHistoricoMessage' para ser mais preciso
        
        WeakReferenceMessenger.Default.Send(new NavegarParaHistoricoMessage());
    }
}
