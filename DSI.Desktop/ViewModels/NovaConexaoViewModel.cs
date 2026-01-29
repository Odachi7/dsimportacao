using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DSI.Aplicacao.DTOs;
using DSI.Aplicacao.Servicos;
using DSI.Conectores.Abstracoes;
using DSI.Dominio.Enums;
using System.Windows;

namespace DSI.Desktop.ViewModels;

public partial class NovaConexaoViewModel : ObservableObject
{
    private readonly ServicoConexao _servicoConexao;
    private readonly FabricaConectores _fabricaConectores;

    public NovaConexaoViewModel(
        ServicoConexao servicoConexao,
        FabricaConectores fabricaConectores)
    {
        _servicoConexao = servicoConexao ?? throw new ArgumentNullException(nameof(servicoConexao));
        _fabricaConectores = fabricaConectores ?? throw new ArgumentNullException(nameof(fabricaConectores));
        
        // Inicializa com MySQL por padrão
        _tipoBanco = TipoBancoDados.MySql;
    }

    [ObservableProperty]
    private string _nome = string.Empty;

    [ObservableProperty]
    private TipoBancoDados _tipoBanco;

    [ObservableProperty]
    private string _stringConexao = string.Empty;

    public IEnumerable<TipoBancoDados> TiposBancoDisponiveis => Enum.GetValues<TipoBancoDados>();

    // Evento para fechar a janela
    public event Action<bool>? RequestClose;

    [RelayCommand]
    private async Task TestarConexaoAsync()
    {
        if (string.IsNullOrWhiteSpace(StringConexao))
        {
            MessageBox.Show("Informe a string de conexão.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var conector = _fabricaConectores.ObterConector(TipoBanco);
            var resultado = await conector.TestarConexaoAsync(StringConexao);

            MessageBox.Show(
                resultado.Sucesso ? $"Sucesso!\n{resultado.Mensagem}" : $"Falha:\n{resultado.Mensagem}\n{resultado.DetalhesErro}",
                resultado.Sucesso ? "Conexão OK" : "Erro de Conexão",
                MessageBoxButton.OK,
                resultado.Sucesso ? MessageBoxImage.Information : MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao testar: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task SalvarAsync()
    {
        if (string.IsNullOrWhiteSpace(Nome))
        {
            MessageBox.Show("Informe o nome da conexão.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(StringConexao))
        {
            MessageBox.Show("Informe a string de conexão.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var dto = new CriarConexaoDto
            {
                Nome = Nome,
                TipoBanco = TipoBanco,
                StringConexao = StringConexao,
                ModoConexao = ModoConexao.Nativo // Default
            };

            await _servicoConexao.CriarAsync(dto);
            
            MessageBox.Show("Conexão salva com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            RequestClose?.Invoke(true);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Cancelar()
    {
        RequestClose?.Invoke(false);
    }
}
