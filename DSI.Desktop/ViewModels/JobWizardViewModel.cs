using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DSI.Aplicacao.DTOs;
using DSI.Aplicacao.Servicos;
using DSI.Dominio.Enums;
using DSI.Motor.ETL; // Possivel local do ModoJob se nao estiver em Dominio
using System.Collections.ObjectModel;
using System.Windows;

namespace DSI.Desktop.ViewModels;

/// <summary>
/// ViewModel para o Wizard de criação de Jobs (7 passos)
/// </summary>
public partial class JobWizardViewModel : ObservableObject
{
    private readonly ServicoJob _servicoJob;
    private readonly ServicoConexao _servicoConexao;
    private readonly ServicoMapeamento _servicoMapeamento;
    private readonly DSI.Conectores.Abstracoes.FabricaConectores _fabricaConectores;

    public event EventHandler? RequestClose;

    [ObservableProperty]
    private int _passoAtual = 1;

    [ObservableProperty]
    private bool _podeAvancar = true;

    [ObservableProperty]
    private bool _podeVoltar = false;

    // Passo 1: Informações Básicas
    [ObservableProperty]
    private string _nomeJob = string.Empty;

    // Passo 2: Conexões
    [ObservableProperty]
    private ObservableCollection<ConexaoDto> _conexoesDisponiveis = new();

    [ObservableProperty]
    private ConexaoDto? _conexaoOrigemSelecionada;

    [ObservableProperty]
    private ConexaoDto? _conexaoDestinoSelecionada;

    // Passo 3: Configurações ETL
    [ObservableProperty]
    private ModoImportacao _modoSelecionado = ModoImportacao.Completo;

    [ObservableProperty]
    private int _tamanhoLote = 1000;

    [ObservableProperty]
    private PoliticaErro _politicaErroSelecionada = PoliticaErro.AplicarDefaultEContinuar;

    // Passo 4: Tabelas
    [ObservableProperty]
    private ObservableCollection<TabelaSelecaoViewModel> _tabelasDisponiveis = new();

    [ObservableProperty]
    private bool _carregandoTabelas;

    // Passo 5: Mapeamentos
    [ObservableProperty]
    private ObservableCollection<MapeamentoTabelaViewModel> _mapeamentos = new();

    [ObservableProperty]
    private MapeamentoTabelaViewModel? _mapeamentoSelecionado;

    [ObservableProperty]
    private bool _carregandoMapeamentos;

    [ObservableProperty]
    private EstrategiaConflito _estrategiaConflitoSelecionada = EstrategiaConflito.ApenasInserir;

    [ObservableProperty]
    private TipoRegra _regraSelecionadaParaAdicionar = TipoRegra.Obrigatorio;

    [ObservableProperty]
    private MapeamentoColunaViewModel? _colunaParaRegraSelecionada;

    public ObservableCollection<TipoRegra> RegrasDisponiveis { get; } = new(Enum.GetValues<TipoRegra>()
        .OrderBy(t => t.ToString())); // Pode melhorar a ordenação depois

    public ObservableCollection<ModoImportacao> ModosDisponiveis { get; } = new()
    {
        ModoImportacao.Completo,
        ModoImportacao.Incremental
    };

    public ObservableCollection<PoliticaErro> PoliticasErroDisponiveis { get; } = new()
    {
        PoliticaErro.PararNoPrimeiroErro,
        PoliticaErro.PularLinhasInvalidas,
        PoliticaErro.AplicarDefaultEContinuar
    };

    public ObservableCollection<EstrategiaConflito> EstrategiasConflitoDisponiveis { get; } = new()
    {
        EstrategiaConflito.ApenasInserir,
        EstrategiaConflito.UpsertSeSuportado,
        EstrategiaConflito.PularSeExistir
    };

    public JobWizardViewModel(
        ServicoJob servicoJob,
        ServicoConexao servicoConexao,
        ServicoMapeamento servicoMapeamento,
        DSI.Conectores.Abstracoes.FabricaConectores fabricaConectores)
    {
        _servicoJob = servicoJob;
        _servicoConexao = servicoConexao;
        _servicoMapeamento = servicoMapeamento;
        _fabricaConectores = fabricaConectores;

        _ = CarregarConexoesAsync();
    }

    private async Task CarregarConexoesAsync()
    {
        try
        {
            var conexoes = await _servicoConexao.ObterTodasAsync();
            ConexoesDisponiveis.Clear();
            foreach (var conexao in conexoes)
            {
                ConexoesDisponiveis.Add(conexao);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao carregar conexões: {ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Avancar()
    {
        if (!ValidarPassoAtual()) return;

        if (PassoAtual < 7)
        {
            PassoAtual++;
            AtualizarBotoes();

            if (PassoAtual == 4)
            {
                _ = CarregarTabelasOrigemAsync();
            }
            else if (PassoAtual == 5)
            {
                _ = CarregarMapeamentosAsync();
            }
        }
    }

    [RelayCommand]
    private void Voltar()
    {
        if (PassoAtual > 1)
        {
            PassoAtual--;
            AtualizarBotoes();
        }
    }

    [RelayCommand]
    private async Task FinalizarAsync()
    {
        if (!ValidarPassoAtual()) return;

        try
        {
            // 1. Criar Job (Header)
            var jobDto = new CriarJobDto
            {
                Nome = NomeJob,
                ConexaoOrigemId = ConexaoOrigemSelecionada!.Id,
                ConexaoDestinoId = ConexaoDestinoSelecionada!.Id,
                Modo = ModoSelecionado,
                TamanhoLote = TamanhoLote,
                PoliticaErro = PoliticaErroSelecionada,
                EstrategiaConflito = EstrategiaConflitoSelecionada
            };

            var jobCriado = await _servicoJob.CriarAsync(jobDto);
            
            MessageBox.Show($"Job '{jobCriado.Nome}' criado com sucesso!", "Sucesso",
                MessageBoxButton.OK, MessageBoxImage.Information);

            // TODO: Fechar wizard e retornar ao dashboard
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao criar job: {ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void AdicionarRegra()
    {
        if (ColunaParaRegraSelecionada == null) return;

        var novaRegra = new RegraViewModel
        {
            Tipo = RegraSelecionadaParaAdicionar,
            Parametros = "" 
        };

        ColunaParaRegraSelecionada.Regras.Add(novaRegra);
    }

    [RelayCommand]
    private void RemoverRegra(RegraViewModel regra)
    {
        if (ColunaParaRegraSelecionada == null || regra == null) return;
        
        if (ColunaParaRegraSelecionada.Regras.Contains(regra))
        {
            ColunaParaRegraSelecionada.Regras.Remove(regra);
        }
    }



    [RelayCommand]
    private void Cancelar()
    {
        var resultado = MessageBox.Show(
            "Deseja realmente cancelar? Todas as alterações serão perdidas.",
            "Confirmar Cancelamento",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (resultado == MessageBoxResult.Yes)
        {
            // TODO: Fechar wizard
        }
    }

    private bool ValidarPassoAtual()
    {
        switch (PassoAtual)
        {
            case 1: // Informações Básicas
                if (string.IsNullOrWhiteSpace(NomeJob))
                {
                    MessageBox.Show("Nome do Job é obrigatório", "Validação",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                break;

            case 2: // Conexões
                if (ConexaoOrigemSelecionada == null || ConexaoDestinoSelecionada == null)
                {
                    MessageBox.Show("Selecione as conexões de origem e destino", "Validação",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                if (ConexaoOrigemSelecionada.Id == ConexaoDestinoSelecionada.Id)
                {
                    MessageBox.Show("As conexões de origem e destino devem ser diferentes", "Validação",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                break;

            case 3: // Configurações
                if (TamanhoLote < 1 || TamanhoLote > 10000)
                {
                    MessageBox.Show("Tamanho do lote deve estar entre 1 e 10000", "Validação",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                break;

            case 4: // Tabelas
                if (!TabelasDisponiveis.Any(t => t.Selecionada))
                {
                    MessageBox.Show("Selecione pelo menos uma tabela", "Validação",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                break;
        }

        return true;
    }

    private async Task CarregarTabelasOrigemAsync()
    {
        if (ConexaoOrigemSelecionada == null) return;

        try
        {
            CarregandoTabelas = true;
            TabelasDisponiveis.Clear();

            // 1. Obtém conexão completa (com string conexao) e descriptografa
            // Como ServicoConexao.DescobrirTabelasAsync faz isso internamente, 
            // precisamos apenas do Conector e do ID.

            // 2. Obtém o conector adequado
            // Precisamos saber o TipoBanco da conexão selecionada.
            // O DTO ConexaoDto tem TipoBanco.
            var tipoBanco = ConexaoOrigemSelecionada.TipoBanco;

            if (!_fabricaConectores.TemConector(tipoBanco))
            {
                MessageBox.Show($"Não há conector configurado para o tipo {tipoBanco}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var conector = _fabricaConectores.ObterConector(tipoBanco);

            // 3. Descobre tabelas
            var tabelas = await _servicoConexao.DescobrirTabelasAsync(ConexaoOrigemSelecionada.Id, conector);

            // 4. Popula lista
            foreach (var tabela in tabelas)
            {
                TabelasDisponiveis.Add(new TabelaSelecaoViewModel
                {
                    Nome = tabela.Nome,
                    Tipo = tabela.Tipo, // TABLE ou VIEW
                    EstimativaLinhas = tabela.QuantidadeLinhas ?? 0
                });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao carregar tabelas: {ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            CarregandoTabelas = false;
        }
    }

    private async Task CarregarMapeamentosAsync()
    {
        if (ConexaoOrigemSelecionada == null || ConexaoDestinoSelecionada == null) return;
        
        // Verifica se há tabelas selecionadas
        var tabelasSelecionadas = TabelasDisponiveis.Where(t => t.Selecionada).ToList();
        if (!tabelasSelecionadas.Any()) return;

        try
        {
            CarregandoMapeamentos = true;
            Mapeamentos.Clear();

            // Obtém conectores
            var conectorOrigem = _fabricaConectores.ObterConector(ConexaoOrigemSelecionada.TipoBanco);
            // Para destino, se não tiver conector (ex: tipo desconhecido), falha.
            // Mas assumimos que validacoes anteriores garantem.
            var conectorDestino = _fabricaConectores.ObterConector(ConexaoDestinoSelecionada.TipoBanco);

            // Strings de conexão (descriptografadas) - precisamos obter via serviço pois o DTO Criptografado não serve.
            // Wait, ServicoConexao expoe DescobrirTabelas, mas nao expoe metodo publico para ObterStringConexaoAsync "crua" para o ViewModel,
            // mas podemos chamar DescobrirSchemaTabelaAsync via conector passando a string.
            // O ServicoConexao tem um método helper interno. 
            // VOU USAR ServicoConexao.ObterStringConexaoAsync (que é publico na classe ServicoConexao que li antes).
            
            var strOrigem = await _servicoConexao.ObterStringConexaoAsync(ConexaoOrigemSelecionada.Id);
            var strDestino = await _servicoConexao.ObterStringConexaoAsync(ConexaoDestinoSelecionada.Id);

            foreach (var tabela in tabelasSelecionadas)
            {
                var mapa = new MapeamentoTabelaViewModel
                {
                    TabelaOrigem = tabela.Nome,
                    TabelaDestino = tabela.Nome // Default: mesmo nome
                };

                // Descobre schema completo da tabela origem
                var schemaOrigem = await conectorOrigem.DescobrirSchemaTabelaAsync(strOrigem, tabela.Nome);

                // Tenta descobrir schema destino (pode não existir ainda)
                DSI.Conectores.Abstracoes.Modelos.InfoTabela? schemaDestino = null;
                try
                {
                    schemaDestino = await conectorDestino.DescobrirSchemaTabelaAsync(strDestino, tabela.Nome);
                }
                catch
                {
                    // Tabela destino provavelmente não existe
                }

                foreach (var colOrigem in schemaOrigem.Colunas)
                {
                    var colMapa = new MapeamentoColunaViewModel
                    {
                        ColunaOrigem = colOrigem.Nome,
                        TipoOrigem = colOrigem.TipoDados,
                        ColunaDestino = colOrigem.Nome, // Default
                        // TipoDestino vamos tentar inferir ou usar o mesmo
                        TipoDestino = colOrigem.TipoDados,
                        ChavePrimaria = colOrigem.EhChavePrimaria
                    };

                    // Se destino existe, tenta match
                    if (schemaDestino != null)
                    {
                        var colDestino = schemaDestino.Colunas.FirstOrDefault(c => c.Nome.Equals(colOrigem.Nome, StringComparison.OrdinalIgnoreCase));
                        if (colDestino != null)
                        {
                            colMapa.ColunaDestino = colDestino.Nome;
                            colMapa.TipoDestino = colDestino.TipoDados;
                        }
                        else
                        {
                            // Não achou no destino, mantém sugestão de criar igual
                        }
                    }

                    mapa.Colunas.Add(colMapa);
                }

                Mapeamentos.Add(mapa);
            }

            // Seleciona o primeiro
            MapeamentoSelecionado = Mapeamentos.FirstOrDefault();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao carregar mapeamentos: {ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            CarregandoMapeamentos = false;
        }
    }

    private void AtualizarBotoes()
    {
        PodeVoltar = PassoAtual > 1;
        PodeAvancar = PassoAtual < 7;
    }

    partial void OnPassoAtualChanged(int value)
    {
        AtualizarBotoes();
    }
}
