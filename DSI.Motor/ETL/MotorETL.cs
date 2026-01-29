using DSI.Conectores.Abstracoes.Interfaces;
using DSI.Dominio.Entidades;
using DSI.Dominio.Enums;
using System.Diagnostics;

namespace DSI.Motor.ETL;

/// <summary>
/// Motor principal ETL que orquestra Extract, Transform e Load
/// </summary>
public class MotorETL
{
    private readonly CamadaExtract _camadaExtract;
    private readonly CamadaTransform _camadaTransform;
    private readonly CamadaLoad _camadaLoad;
    private readonly GerenciadorCheckpoint _gerenciadorCheckpoint;

    public MotorETL(
        CamadaExtract camadaExtract,
        CamadaTransform camadaTransform,
        CamadaLoad camadaLoad,
        GerenciadorCheckpoint gerenciadorCheckpoint)
    {
        _camadaExtract = camadaExtract ?? throw new ArgumentNullException(nameof(camadaExtract));
        _camadaTransform = camadaTransform ?? throw new ArgumentNullException(nameof(camadaTransform));
        _camadaLoad = camadaLoad ?? throw new ArgumentNullException(nameof(camadaLoad));
        _gerenciadorCheckpoint = gerenciadorCheckpoint ?? throw new ArgumentNullException(nameof(gerenciadorCheckpoint));
    }

    /// <summary>
    /// Executa processo ETL completo para um job
    /// </summary>
    public async Task<Execucao> ExecutarAsync(
        ContextoExecucao contexto)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            contexto.Execucao.Status = StatusExecucao.Executando;
            contexto.Execucao.IniciadoEm = DateTime.Now;

            // Inicia transação se configurado
            if (UsarTransacao(contexto.Job))
            {
                contexto.IniciarTransacao();
            }

            // Processa cada tabela do job
            foreach (var tabelaJob in contexto.Job.Tabelas)
            {
                await ProcessarTabelaAsync(contexto, tabelaJob);

                if (contexto.CancellationToken.IsCancellationRequested)
                {
                    contexto.Execucao.Status = StatusExecucao.Cancelado;
                    break;
                }
            }

            // Confirma transação se tudo ocorreu bem
            // Confirma transação se tudo ocorreu bem
            if (UsarTransacao(contexto.Job) && contexto.Execucao.Status != StatusExecucao.Cancelado)
            {
                contexto.ConfirmarTransacao();
            }
            
            if (contexto.Execucao.Status != StatusExecucao.Cancelado)
            {
                contexto.Execucao.Status = StatusExecucao.Concluido;
            }
        }
        catch (Exception ex)
        {
            contexto.Execucao.Status = StatusExecucao.Falhou;

            // Reverte transação em caso de erro
            if (UsarTransacao(contexto.Job))
            {
                contexto.ReverterTransacao();
            }

            // Registra erro
            var erroExecucao = new ErroExecucao
            {
                Id = Guid.NewGuid(),
                ExecucaoId = contexto.Execucao.Id,
                OcorridoEm = DateTime.Now,
                Mensagem = $"Erro fatal na execução: {ex.Message}"
            };

            contexto.Execucao.Erros.Add(erroExecucao);

            throw;
        }
        finally
        {
            stopwatch.Stop();
            contexto.Execucao.FinalizadoEm = DateTime.Now;

            // Atualiza resumo
            contexto.Execucao.ResumoJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                totalLinhas = contexto.TotalLinhasProcessadas,
                linhasSucesso = contexto.TotalLinhasSucesso,
                linhasErro = contexto.TotalLinhasErro,
                tempoExecucaoMs = stopwatch.ElapsedMilliseconds
            });
        }

        return contexto.Execucao;
    }

    /// <summary>
    /// Processa uma tabela específica
    /// </summary>
    private async Task ProcessarTabelaAsync(
        ContextoExecucao contexto,
        TabelaJob tabelaJob)
    {
        var estatistica = new EstatisticaTabelaExecucao
        {
            Id = Guid.NewGuid(),
            ExecucaoId = contexto.Execucao.Id,
            TabelaJobId = tabelaJob.Id,
            LinhasLidas = 0,
            LinhasInseridas = 0,
            LinhasAtualizadas = 0,
            LinhasComErro = 0
        };

        contexto.Execucao.EstatisticasTabelas.Add(estatistica);

        try
        {
            // Recupera checkpoint se modo incremental
            object? checkpoint = null;
            if (contexto.Job.Modo == ModoImportacao.Incremental)
            {
                checkpoint = await _gerenciadorCheckpoint.RecuperarCheckpointAsync(
                    contexto.Job.Id,
                    tabelaJob.TabelaOrigem);
            }

            object? ultimoCheckpoint = checkpoint;

            // Processa em lotes
            await foreach (var loteExtraido in _camadaExtract.ExtrairEmLotesAsync(
                contexto,
                tabelaJob,
                contexto.Job.TamanhoLote,
                checkpoint))
            {
                contexto.CancellationToken.ThrowIfCancellationRequested();

                estatistica.LinhasLidas += loteExtraido.Linhas.Count;
                contexto.TotalLinhasProcessadas += loteExtraido.Linhas.Count;

                // Transform
                var loteTransformado = await _camadaTransform.TransformarLoteAsync(
                    contexto,
                    loteExtraido,
                    tabelaJob);

                estatistica.LinhasComErro += loteTransformado.LinhasErro.Count;
                contexto.TotalLinhasErro += loteTransformado.LinhasErro.Count;

                // Registra erros de transformação
                foreach (var erroLinha in loteTransformado.LinhasErro)
                {
                    var erroExecucao = new ErroExecucao
                    {
                        Id = Guid.NewGuid(),
                        ExecucaoId = contexto.Execucao.Id,
                        OcorridoEm = DateTime.Now,
                        TabelaJobId = tabelaJob.Id,
                        ChaveLinha = loteExtraido.NumeroLote.ToString(),
                        Mensagem = erroLinha.Mensagem
                    };

                    contexto.Execucao.Erros.Add(erroExecucao);
                }

                // Load
                if (loteTransformado.LinhasSucesso.Count > 0)
                {
                    var resultadoLote = await _camadaLoad.CarregarLoteAsync(
                        contexto,
                        loteTransformado,
                        tabelaJob);

                    estatistica.LinhasInseridas += resultadoLote.LinhasInseridas;

                    // Atualiza checkpoint com último valor processado
                    if (contexto.Job.Modo == ModoImportacao.Incremental &&
                        !string.IsNullOrEmpty(tabelaJob.ColunaCheckpoint))
                    {
                        var ultimaLinha = loteExtraido.Linhas.LastOrDefault();
                        if (ultimaLinha != null && ultimaLinha.ContainsKey(tabelaJob.ColunaCheckpoint))
                        {
                            ultimoCheckpoint = ultimaLinha[tabelaJob.ColunaCheckpoint];
                        }
                    }
                }

                // Reporta progresso
                var percentual = CalcularPercentual(estatistica.LinhasLidas, 100000); // Estimativa
                contexto.ReportarProgresso(
                    $"Processando {tabelaJob.TabelaOrigem}: {estatistica.LinhasLidas} linhas",
                    percentual);
            }

            // Salva checkpoint final
            if (contexto.Job.Modo == ModoImportacao.Incremental && ultimoCheckpoint != null)
            {
                await _gerenciadorCheckpoint.SalvarCheckpointAsync(
                    contexto.Execucao.Id,
                    tabelaJob.TabelaOrigem,
                    ultimoCheckpoint);
            }
        }
        catch (Exception ex)
        {
            var erroExecucao = new ErroExecucao
            {
                Id = Guid.NewGuid(),
                ExecucaoId = contexto.Execucao.Id,
                OcorridoEm = DateTime.Now,
                TabelaJobId = tabelaJob.Id,
                Mensagem = $"Erro ao processar tabela: {ex.Message}"
            };

            contexto.Execucao.Erros.Add(erroExecucao);

            if (contexto.Job.PoliticaErro == PoliticaErro.PararNoPrimeiroErro)
            {
                throw;
            }
        }
    }

    /// <summary>
    /// Determina se deve usar transação
    /// </summary>
    private bool UsarTransacao(Job job)
    {
        // Usa transação exceto para jobs muito grandes ou incrementais
        return job.Modo != ModoImportacao.Incremental &&
               job.TamanhoLote <= 10000;
    }

    /// <summary>
    /// Calcula percentual de progresso
    /// </summary>
    private int CalcularPercentual(long processadas, long estimativaTotal)
    {
        if (estimativaTotal == 0)
            return 0;

        var percentual = (int)((processadas * 100) / estimativaTotal);
        return Math.Min(percentual, 99); // Nunca retorna 100% até terminar
    }
}
