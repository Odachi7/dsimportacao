using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DSI.Aplicacao.Servicos;
using DSI.Aplicacao.DTOs;
using System.Collections.ObjectModel;
using System.Windows;

namespace DSI.Desktop.ViewModels;

public partial class ConexoesViewModel : ObservableObject
{
    private readonly ServicoConexao _servicoConexao;

    [ObservableProperty]
    private ObservableCollection<ConexaoDto> _conexoes = new();

    [ObservableProperty]
    private ConexaoDto? _conexaoSelecionada;

    public ConexoesViewModel(ServicoConexao servicoConexao)
    {
        _servicoConexao = servicoConexao;
        _ = CarregarConexoesAsync();
    }

    [RelayCommand]
    private async Task CarregarConexoesAsync()
    {
        try
        {
            var conexoes = await _servicoConexao.ObterTodasAsync();
            Conexoes.Clear();
            foreach (var conn in conexoes)
            {
                Conexoes.Add(conn);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao carregar conexões: {ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void NovaConexao()
    {
        MessageBox.Show("Abrir formulário de nova conexão (TODO)", "Info");
    }

    [RelayCommand(CanExecute = nameof(PodeTestarConexao))]
    private async Task TestarConexaoAsync()
    {
        if (ConexaoSelecionada == null) return;

        try
        {
            var resultado = await _servicoConexao.TestarConexaoAsync(ConexaoSelecionada.Id, null!); // TODO: Passar conector
            MessageBox.Show(
                resultado.Sucesso ? $"Conexão testada com sucesso!\n{resultado.Mensagem}\nTempo: {resultado.TempoRespostaMs}ms" : $"Falha ao conectar:\n{resultado.DetalhesErro ?? resultado.Mensagem}",
                resultado.Sucesso ? "Sucesso" : "Erro",
                MessageBoxButton.OK,
                resultado.Sucesso ? MessageBoxImage.Information : MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao testar conexão: {ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool PodeTestarConexao() => ConexaoSelecionada != null;
}
