using DSI.Conectores.Abstracoes.Interfaces;
using DSI.Conectores.Abstracoes.Enums;
using DSI.Dominio.Entidades;
using DSI.Dominio.Enums;
using System.Data;

namespace DSI.Motor.ETL;

/// <summary>
/// Camada de carga de dados (Load)
/// Responsável por inserir dados no destino com bulk insert e resolução de conflitos
/// </summary>
public class CamadaLoad
{
    private readonly IConector _conectorDestino;

    public CamadaLoad(IConector conectorDestino)
    {
        _conectorDestino = conectorDestino ?? throw new ArgumentNullException(nameof(conectorDestino));
    }

    /// <summary>
    /// Carrega um lote transformado no banco de destino
    /// </summary>
    public async Task<ResultadoLote> CarregarLoteAsync(
        ContextoExecucao contexto,
        LoteTransformado lote,
        TabelaJob tabelaJob)
    {
        var resultado = new ResultadoLote
        {
            NumeroLote = lote.NumeroLote
        };

        if (lote.LinhasSucesso.Count == 0)
        {
            resultado.LinhasInseridas = 0;
            return resultado;
        }

        try
        {
            // Converte para DataTable para bulk insert
            var dataTable = ConverterParaDataTable(lote.LinhasSucesso, lote.TabelaDestino);

            // Escolhe estratégia baseada na configuração do job
            switch (contexto.Job.EstrategiaConflito)
            {
                case EstrategiaConflito.UpsertSeSuportado:
                    resultado.LinhasInseridas = await CarregarComUpsertAsync(
                        contexto,
                        dataTable,
                        tabelaJob);
                    break;

                case EstrategiaConflito.PularSeExistir:
                case EstrategiaConflito.ApenasInserir:
                default:
                    resultado.LinhasInseridas = await CarregarComBulkInsertAsync(
                        contexto,
                        dataTable,
                        tabelaJob);
                    break;
            }

            contexto.TotalLinhasSucesso += resultado.LinhasInseridas;
        }
        catch (Exception ex)
        {
            resultado.Erro = ex.Message;
            resultado.DetalhesErro = ex.ToString();

           // Registra erro
            var erroExecucao = new ErroExecucao
            {
                Id = Guid.NewGuid(),
                ExecucaoId = contexto.Execucao.Id,
                OcorridoEm = DateTime.Now,
                TabelaJobId = tabelaJob.Id,
                ChaveLinha = lote.NumeroLote.ToString(),
                Mensagem = ex.Message
            };

            contexto.Execucao.Erros.Add(erroExecucao);

            if (contexto.Job.PoliticaErro == PoliticaErro.PararNoPrimeiroErro)
            {
                throw;
            }
        }

        return resultado;
    }

    /// <summary>
    /// Carrega dados usando bulk insert simples
    /// </summary>
    private async Task<int> CarregarComBulkInsertAsync(
        ContextoExecucao contexto,
        DataTable dados,
        TabelaJob tabelaJob)
    {
        return await _conectorDestino.InserirEmLoteAsync(
            contexto.ConexaoDestino,
            tabelaJob.TabelaDestino,
            dados);
    }

    /// <summary>
    /// Carrega dados usando UPSERT (INSERT ... ON CONFLICT / MERGE)
    /// </summary>
    private async Task<int> CarregarComUpsertAsync(
        ContextoExecucao contexto,
        DataTable dados,
        TabelaJob tabelaJob)
    {
        // Verifica se o conector suporta UPSERT
        if (!_conectorDestino.Capacidades.HasFlag(CapacidadesConector.UpsertNativo))
        {
            // Fallback: apaga e insere
            return await CarregarComDeleteInsertAsync(contexto, dados, tabelaJob);
        }

        var colunas = dados.Columns.Cast<DataColumn>().Select(c => c.ColumnName);
        var colunasChave = ObterColunasChave(tabelaJob);

        var comandoUpsert = _conectorDestino.ConstruirComandoUpsert(
            tabelaJob.TabelaDestino,
            colunas,
            colunasChave);

        if (string.IsNullOrEmpty(comandoUpsert))
        {
            throw new NotSupportedException("Conector não suporta UPSERT");
        }

        // Executa UPSERT linha por linha (alguns bancos não suportam bulk upsert)
        var linhasAfetadas = 0;

        foreach (DataRow linha in dados.Rows)
        {
            var parametros = new Dictionary<string, object>();
            
            foreach (DataColumn coluna in dados.Columns)
            {
                parametros[coluna.ColumnName] = linha[coluna] ?? DBNull.Value;
            }

            linhasAfetadas += await _conectorDestino.ExecutarComandoAsync(
                contexto.ConexaoDestino,
                comandoUpsert,
                parametros);
        }

        return linhasAfetadas;
    }

/// <summary>
    /// Carrega dados apagando duplicatas e inserindo (fallback para bancos sem UPSERT)
    /// </summary>
    private async Task<int> CarregarComDeleteInsertAsync(
        ContextoExecucao contexto,
        DataTable dados,
        TabelaJob tabelaJob)
    {
        var colunasChave = ObterColunasChave(tabelaJob);
        
        // Apaga registros existentes
        foreach (DataRow linha in dados.Rows)
        {
            var condicoes = colunasChave.Select(c => $"{c} = @{c}");
            var sql = $"DELETE FROM {tabelaJob.TabelaDestino} WHERE {string.Join(" AND ", condicoes)}";
            
            var parametros = new Dictionary<string, object>();
            foreach (var chave in colunasChave)
            {
                parametros[chave] = linha[chave] ?? DBNull.Value;
            }

            await _conectorDestino.ExecutarComandoAsync(
                contexto.ConexaoDestino,
                sql,
                parametros);
        }

        // Insere novos registros
        return await _conectorDestino.InserirEmLoteAsync(
            contexto.ConexaoDestino,
            tabelaJob.TabelaDestino,
            dados);
    }

    /// <summary>
    /// Converte lista de dicionários para DataTable
    /// </summary>
    private DataTable ConverterParaDataTable(
        List<Dictionary<string, object?>> linhas,
        string nomeTabela)
    {
        var dataTable = new DataTable(nomeTabela);

        if (linhas.Count == 0)
            return dataTable;

        // Cria colunas baseado na primeira linha
        foreach (var chave in linhas[0].Keys)
        {
            dataTable.Columns.Add(chave, typeof(object));
        }

        // Adiciona linhas
        foreach (var linha in linhas)
        {
            var row = dataTable.NewRow();
            
            foreach (var kvp in linha)
            {
                row[kvp.Key] = kvp.Value ?? DBNull.Value;
            }

            dataTable.Rows.Add(row);
        }

        return dataTable;
    }

    /// <summary>
    /// Obtém colunas que são chaves primárias
    /// </summary>
    private List<string> ObterColunasChave(TabelaJob tabelaJob)
    {
        // TODO: Implementar descoberta de chaves primárias do schema
        // Por enquanto, retorna primeira coluna
        return tabelaJob.Mapeamentos
            .Where(m => m.ColunaDestino.ToLower().Contains("id"))
            .Select(m => m.ColunaDestino)
            .Take(1)
            .ToList();
    }
}

/// <summary>
/// Resultado do carregamento de um lote
/// </summary>
public class ResultadoLote
{
    public int NumeroLote { get; set; }
    public int LinhasInseridas { get; set; }
    public string? Erro { get; set; }
    public string? DetalhesErro { get; set; }
}
