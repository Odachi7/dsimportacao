using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DSI.Aplicacao.Servicos;
using DSI.Dominio.Entidades;
using DSI.Dominio.Enums;
using DSI.Motor;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace DSI.Desktop.ViewModels;

public partial class MonitorExecucaoViewModel : ObservableObject, IDisposable
{
    private readonly ServicoExecucao _servicoExecucao;
    private readonly ServicoJob _servicoJob;
    private Guid _execucaoIdAtual;

    [ObservableProperty]
    private string _nomeJob = "Aguardando Job...";

    [ObservableProperty]
    private string _statusTexto = "Pronto";

    [ObservableProperty]
    private double _progresso; // 0 a 100

    [ObservableProperty]
    private long _linhasProcessadas;

    [ObservableProperty]
    private long _linhasSucesso;

    [ObservableProperty]
    private long _linhasErro;

    [ObservableProperty]
    private string _mensagemAtual = "";

    [ObservableProperty]
    private bool _estaExecutando;



    [ObservableProperty]
    private bool _exibirLogsTecnicos = false;

    private readonly List<string> _todosLogs = new();
    public ObservableCollection<string> Logs { get; } = new();

    partial void OnExibirLogsTecnicosChanged(bool value)
    {
        AtualizarListaLogs();
    }

    public MonitorExecucaoViewModel(
        ServicoExecucao servicoExecucao,
        ServicoJob servicoJob)
    {
        _servicoExecucao = servicoExecucao ?? throw new ArgumentNullException(nameof(servicoExecucao));
        _servicoJob = servicoJob ?? throw new ArgumentNullException(nameof(servicoJob));

        // Assina eventos globais
        _servicoExecucao.ProgressoRecebido += OnProgressoRecebido;
    }

    private void OnProgressoRecebido(object? sender, (Guid ExecucaoId, ProgressoEventArgs Args) e)
    {
        // Só atualiza se for a execução atual
        if (e.ExecucaoId != _execucaoIdAtual) return;

        // Atualiza UI na thread correta
        Application.Current.Dispatcher.Invoke(() =>
        {
            Progresso = e.Args.Percentual;
            MensagemAtual = e.Args.Mensagem;
            LinhasProcessadas = e.Args.LinhasProcessadas;
            LinhasSucesso = e.Args.LinhasSucesso;
            LinhasErro = e.Args.LinhasErro;

            // Log detalhado
            var logMsg = $"[{DateTime.Now:HH:mm:ss}] {e.Args.Mensagem}";
            _todosLogs.Insert(0, logMsg);
            if (_todosLogs.Count > 1000) _todosLogs.RemoveAt(_todosLogs.Count - 1);

            if (DeveExibirLog(e.Args.Mensagem))
            {
                Logs.Insert(0, logMsg);
                if (Logs.Count > 100) Logs.RemoveAt(Logs.Count - 1);
            }
        });
    }

    private void AtualizarListaLogs()
    {
        Logs.Clear();
        foreach (var log in _todosLogs.Where(l => DeveExibirLog(l)))
        {
            Logs.Add(log);
            if (Logs.Count >= 100) break;
        }
    }

    private bool DeveExibirLog(string mensagemOuLogCompleto)
    {
        if (ExibirLogsTecnicos) return true;

        // Heurística de logs técnicos
        // Se a mensagem contém palavras de baixo nível, consideramos técnico
        var msgLower = mensagemOuLogCompleto.ToLower();
        if (msgLower.Contains("lote")) return false;
        if (msgLower.Contains("processando registros")) return false;
        if (msgLower.Contains("validando")) return false;
        if (msgLower.Contains("transformando")) return false;
        
        return true;
    }

    public async Task IniciarJobAsync(Guid jobId)
    {
        try
        {
            EstaExecutando = true;
            EstaExecutando = true;
            Logs.Clear();
            _todosLogs.Clear();
            Progresso = 0;
            LinhasProcessadas = 0;
            LinhasSucesso = 0;
            LinhasErro = 0;
            
            var job = await _servicoJob.ObterPorIdAsync(jobId);
            NomeJob = job?.Nome ?? "Job Desconhecido";
            StatusTexto = "Iniciando Execução...";
            NomeJob = job?.Nome ?? "Job Desconhecido";
            StatusTexto = "Iniciando Execução...";
            var msg = $"[{DateTime.Now:HH:mm:ss}] Iniciando Job '{NomeJob}'...";
            Logs.Add(msg);
            _todosLogs.Add(msg);

            // Inicia execução
            _execucaoIdAtual = await _servicoExecucao.ExecutarAsync(jobId);
            
            StatusTexto = "Executando...";
        }
        catch (Exception ex)
        {
            StatusTexto = "Erro ao Iniciar";
            Logs.Add($"[ERRO] {ex.Message}");
            MessageBox.Show($"Falha ao iniciar Job: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            EstaExecutando = false;
        }
    }

    [RelayCommand]
    private async Task Cancelar()
    {
        if (!EstaExecutando) return;

        if (MessageBox.Show("Deseja realmente cancelar a execução?", "Confirmar", 
            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            await _servicoExecucao.CancelarAsync(_execucaoIdAtual);
            StatusTexto = "Cancelamento Solicitado...";
            Logs.Add("Solicitado cancelamento...");
            EstaExecutando = false; // Assume parado, mas serviço vai atualizar status final no banco
        }
    }

    public void Dispose()
    {
        _servicoExecucao.ProgressoRecebido -= OnProgressoRecebido;
    }
}
