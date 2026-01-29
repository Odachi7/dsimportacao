using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DSI.Aplicacao.Servicos;
using DSI.Desktop.Messages;
using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Win32;
using System.IO;
using System.Text;

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

    public bool TemErros => Erros.Any();

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
                OnPropertyChanged(nameof(TemErros));
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

    [RelayCommand]
    private async Task ExportarCsvAsync()
    {
        if (!Erros.Any())
        {
            MessageBox.Show("Não há erros para exportar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Arquivo CSV (*.csv)|*.csv",
                FileName = $"Erros_Job_{Detalhes?.NomeJob}_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                var sb = new StringBuilder();
                // Header
                sb.AppendLine("DataHora;Tabela;Coluna;ValorOriginal;Mensagem");

                foreach (var erro in Erros)
                {
                    var linha = $"{erro.OcorridoEm};{erro.TabelaJobId};{EscapeCsv(erro.Coluna)};{EscapeCsv(erro.ValorOriginal)};{EscapeCsv(erro.Mensagem)}";
                    sb.AppendLine(linha);
                }

                await File.WriteAllTextAsync(dialog.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show("Arquivo exportado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao exportar: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string EscapeCsv(string? valor)
    {
        if (string.IsNullOrEmpty(valor)) return "";
        return $"\"{valor.Replace("\"", "\"\"")}\"";
    }

    [RelayCommand]
    private async Task ExportarRelatorioAsync()
    {
        if (Detalhes == null) return;

        try
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Relatório de Texto (*.txt)|*.txt",
                FileName = $"Relatorio_Job_{Detalhes.NomeJob}_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };

            if (dialog.ShowDialog() == true)
            {
                var sb = new StringBuilder();
                sb.AppendLine("=== RELATÓRIO DE EXECUÇÃO ===");
                sb.AppendLine($"Job: {Detalhes.NomeJob}");
                sb.AppendLine($"Status: {Detalhes.Status}");
                sb.AppendLine($"Início: {Detalhes.IniciadoEm}");
                sb.AppendLine($"Duração: {Detalhes.DuracaoSegundos:N2} segundos");
                sb.AppendLine("=============================");
                sb.AppendLine("");
                sb.AppendLine("ESTATÍSTICAS POR TABELA:");
                
                foreach (var stat in Estatisticas)
                {
                    sb.AppendLine($"[{stat.TabelaJobId}]");
                    sb.AppendLine($"  - Lidas: {stat.LinhasLidas}");
                    sb.AppendLine($"  - Inseridas: {stat.LinhasInseridas}");
                    sb.AppendLine($"  - Atualizadas: {stat.LinhasAtualizadas}");
                    sb.AppendLine($"  - Erros: {stat.LinhasComErro}");
                    sb.AppendLine("");
                }

                if (TemErros)
                {
                    sb.AppendLine("RESUMO DE ERROS:");
                    sb.AppendLine($"Total de Erros Registrados: {Erros.Count}");
                    // Listar primeiros 10
                    foreach (var erro in Erros.Take(10))
                    {
                        sb.AppendLine($"[{erro.OcorridoEm:HH:mm:ss}] {erro.TabelaJobId} - {erro.Mensagem}");
                    }
                    if (Erros.Count > 10) sb.AppendLine("... (veja CSV para lista completa)");
                }

                await File.WriteAllTextAsync(dialog.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show("Relatório exportado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao exportar relatório: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
