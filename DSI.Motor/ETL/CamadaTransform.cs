using DSI.Dominio.Entidades;
using DSI.Dominio.Enums;
using DSI.Motor.Modelos;
using DSI.Motor.Regras.Interfaces;

namespace DSI.Motor.ETL;

/// <summary>
/// Camada de transformação de dados (Transform)
/// Responsável por aplicar regras e transformações nos dados extraídos
/// </summary>
public class CamadaTransform
{
    private readonly IServiceProvider _serviceProvider;

    public CamadaTransform(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Transforma um lote de dados aplicando mapeamentos e regras
    /// </summary>
    public async Task<LoteTransformado> TransformarLoteAsync(
        ContextoExecucao contexto,
        LoteDados loteOriginal,
        TabelaJob tabelaJob)
    {
        var loteTransformado = new LoteTransformado
        {
            NumeroLote = loteOriginal.NumeroLote,
            TabelaDestino = tabelaJob.TabelaDestino
        };

        foreach (var linhaOriginal in loteOriginal.Linhas)
        {
            try
            {
                var linhaTransformada = await TransformarLinhaAsync(
                    contexto,
                    linhaOriginal,
                  tabelaJob);

                if (linhaTransformada != null)
                {
                    loteTransformado.LinhasSucesso.Add(linhaTransformada);
                }
            }
            catch (Exception ex)
            {
                var erro = new ErroLinha
                {
                    LinhaOriginal = linhaOriginal,
                    Mensagem = ex.Message,
                    Detalhes = ex.ToString()
                };

                loteTransformado.LinhasErro.Add(erro);

                // Trata erro conforme política
                if (contexto.Job.PoliticaErro == PoliticaErro.PararNoPrimeiroErro)
                {
                    throw new Exception($"Erro ao transformar linha: {ex.Message}", ex);
                }
            }
        }

        return loteTransformado;
    }

    /// <summary>
    /// Transforma uma única linha aplicando mapeamentos e regras
    /// </summary>
    private async Task<Dictionary<string, object?>?> TransformarLinhaAsync(
        ContextoExecucao contexto,
        Dictionary<string, object?> linhaOriginal,
        TabelaJob tabelaJob)
    {
        var linhaTransformada = new Dictionary<string, object?>();
        var pularLinha = false;

        // Aplica mapeamentos
        foreach (var mapeamento in tabelaJob.Mapeamentos)
        {
            var valorOriginal = linhaOriginal.GetValueOrDefault(mapeamento.ColunaOrigem);
            var valorTransformado = valorOriginal;

            // Aplica regras do mapeamento
            foreach (var regra in mapeamento.Regras.OrderBy(r => r.Ordem))
            {
                try
                {
                    var resultadoRegra = await AplicarRegraAsync(
                        regra,
                        valorTransformado,
                        linhaOriginal,
                        contexto);

                    if (!resultadoRegra.Sucesso)
                    {
                        // Trata falha conforme AcaoFalhaRegra
                        switch (regra.AcaoQuandoFalhar)
                        {
                            case AcaoFalhaRegra.AplicarDefault:
                                // TODO: Implementar lógica para obter valor padrão dos parâmetros
                                valorTransformado = null;
                                break;

                            case AcaoFalhaRegra.PularLinha:
                                pularLinha = true;
                                break;

                            case AcaoFalhaRegra.PararJob:
                                throw new Exception($"Regra {regra.TipoRegra} falhou: {resultadoRegra.MensagemErro}");

                            default:
                                // Mantém valor original
                                break;
                        }
                    }
                    else
                    {
                        valorTransformado = resultadoRegra.ValorTransformado;
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Erro ao aplicar regra {regra.TipoRegra} na coluna {mapeamento.ColunaDestino}: {ex.Message}", ex);
                }

                if (pularLinha)
                    break;
            }

            if (pularLinha)
                return null;

            linhaTransformada[mapeamento.ColunaDestino] = valorTransformado;
        }

        return linhaTransformada;
    }

    /// <summary>
    /// Aplica uma regra específica a um valor
    /// </summary>
    private async Task<ResultadoRegra> AplicarRegraAsync(
        Regra regra,
        object? valor,
        Dictionary<string, object?> linhaCompleta,
        ContextoExecucao contexto)
    {
        // TODO: Mover esta fábrica para classe separada, mas para o MVP vamos instanciar aqui
        IRegra implementacao = regra.TipoRegra switch
        {
            TipoRegra.Obrigatorio => new DSI.Motor.Regras.Implementacoes.RegraObrigatorio(),
            TipoRegra.DefaultSeNulo => new DSI.Motor.Regras.Implementacoes.RegraDefaultSeNulo(),
            TipoRegra.DefaultSeVazio => new DSI.Motor.Regras.Implementacoes.RegraDefaultSeVazio(),
            TipoRegra.NuloSeVazio => new DSI.Motor.Regras.Implementacoes.RegraNuloSeVazio(),
            TipoRegra.ValorConstante => new DSI.Motor.Regras.Implementacoes.RegraValorConstante(),
            
            TipoRegra.Trim => new DSI.Motor.Regras.Implementacoes.RegraTrim(),
            TipoRegra.Maiuscula => new DSI.Motor.Regras.Implementacoes.RegraUpper(),
            TipoRegra.Minuscula => new DSI.Motor.Regras.Implementacoes.RegraLower(),
            TipoRegra.Substituir => new DSI.Motor.Regras.Implementacoes.RegraSubstituir(),
            TipoRegra.TamanhoMaximo => new DSI.Motor.Regras.Implementacoes.RegraTamanhoMaximo(),

            TipoRegra.ConverterParaInt => new DSI.Motor.Regras.Implementacoes.RegraToInt(),
            TipoRegra.ConverterParaDecimal => new DSI.Motor.Regras.Implementacoes.RegraToDecimal(),
            TipoRegra.ConverterParaBool => new DSI.Motor.Regras.Implementacoes.RegraToBool(),
            TipoRegra.ConverterParaData => new DSI.Motor.Regras.Implementacoes.RegraParseData(),
            TipoRegra.Arredondar => new DSI.Motor.Regras.Implementacoes.RegraArredondar(),
            
            TipoRegra.LookupLocal => new DSI.Motor.Regras.Implementacoes.RegraLookupLocal(),
            // TODO: LookupBancoDados
            
            _ => null
        };

        if (implementacao != null)
        {
            return await implementacao.AplicarAsync(valor, regra.ParametrosJson, _serviceProvider);
        }

        // Sem implementação encontrada
        return new ResultadoRegra
        {
            Sucesso = true,
            ValorTransformado = valor
        };
    }
}

/// <summary>
/// Representa um lote transformado
/// </summary>
public class LoteTransformado
{
    public int NumeroLote { get; set; }
    public string TabelaDestino { get; set; } = string.Empty;
    public List<Dictionary<string, object?>> LinhasSucesso { get; set; } = new();
    public List<ErroLinha> LinhasErro { get; set; } = new();
}

/// <summary>
/// Representa um erro em uma linha
/// </summary>
public class ErroLinha
{
    public Dictionary<string, object?> LinhaOriginal { get; set; } = new();
    public string Mensagem { get; set; } = string.Empty;
    public string Detalhes { get; set; } = string.Empty;
}
