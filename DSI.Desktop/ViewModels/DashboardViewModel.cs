using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DSI.Aplicacao.Servicos;
using DSI.Aplicacao.DTOs;
using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

using DSI.Desktop.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace DSI.Desktop.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ServicoJob _servicoJob;
    private readonly ServicoExecucao _servicoExecucao;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private ObservableCollection<JobDto> _jobs = new();

    [ObservableProperty]
    private JobDto? _jobSelecionado;

    [ObservableProperty]
    private bool _carregando;

    public DashboardViewModel(
        ServicoJob servicoJob,
        ServicoExecucao servicoExecucao,
        IServiceProvider serviceProvider)
    {
        _servicoJob = servicoJob;
        _servicoExecucao = servicoExecucao;
        _serviceProvider = serviceProvider;
        
        _ = CarregarJobsAsync();
    }

    [RelayCommand]
    private async Task CarregarJobsAsync()
    {
        try
        {
            Carregando = true;
            var jobs = await _servicoJob.ObterTodosAsync();
            
            Jobs.Clear();
            foreach (var job in jobs)
            {
                Jobs.Add(job);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao carregar jobs: {ex.Message}", "Erro", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            Carregando = false;
        }
    }

    [RelayCommand]
    private void NovoJob()
    {
        // Abre wizard de novo job resolvendo via DI
        var wizard = _serviceProvider.GetRequiredService<Views.JobWizardWindow>();
        wizard.ShowDialog();
        
        // Recarrega lista após fechar wizard
        _ = CarregarJobsAsync();
    }

    [RelayCommand(CanExecute = nameof(PodeExecutarJob))]
    private async Task ExecutarJobAsync()
    {
        if (JobSelecionado == null) return;

        try
        {
            // Envia solicitação de execução para a MainWindow
            WeakReferenceMessenger.Default.Send(new ExecutarJobMessage(JobSelecionado.Id));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao executar job: {ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(PodeExecutarJob))]
    private void EditarJob()
    {
        if (JobSelecionado == null) return;
        MessageBox.Show($"Editar job {JobSelecionado.Nome} (TODO)", "Info");
    }

    [RelayCommand(CanExecute = nameof(PodeExecutarJob))]
    private async Task ExcluirJobAsync()
    {
        if (JobSelecionado == null) return;

        var resultado = MessageBox.Show(
            $"Deseja realmente excluir o job '{JobSelecionado.Nome}'?",
            "Confirmar Exclusão",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (resultado == MessageBoxResult.Yes)
        {
            try
            {
                await _servicoJob.RemoverAsync(JobSelecionado.Id);
                await CarregarJobsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao excluir job: {ex.Message}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private bool PodeExecutarJob() => JobSelecionado != null;
}
