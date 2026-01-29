using DSI.Conectores.Abstracoes.Interfaces;
using DSI.Dominio.Entidades;
using System.Data;

namespace DSI.Motor.ETL;

/// <summary>
/// Camada de extração de dados (Extract)
/// Responsável por ler dados da origem com suporte a streaming e batching
/// </summary>
public class CamadaExtract
{
    private readonly IConector _conectorOrigem;

    public CamadaExtract(IConector conectorOrigem)
    {
        _conectorOrigem = conectorOrigem ?? throw new ArgumentNullException(nameof(conectorOrigem));
    }

    /// <summary>
    /// Extrai dados de uma tabela em lotes
    /// </summary>
    /// <param name="contexto">Contexto de execução</param>
    /// <param name="tabelaJob">Configuração da tabela</param>
    /// <param name="tamanhoLote">Tamanho do lote</param>
    /// <param name="checkpoint">Checkpoint para importação incremental (opcional)</param>
    /// <returns>Enumerável de lotes de dados</returns>
    public async IAsyncEnumerable<LoteDados> ExtrairEmLotesAsync(
        ContextoExecucao contexto,
        TabelaJob tabelaJob,
        int tamanhoLote,
        object? checkpoint = null)
    {
        var sql = ConstruirConsultaExtracao(tabelaJob, checkpoint);
        
        using var reader = await _conectorOrigem.ExecutarConsultaAsync(
            contexto.ConexaoOrigem,
            sql);

        var lote = new List<Dictionary<string, object?>>();
        var numeroLote = 0;
        var linhaAtual = 0;

        while (reader.Read())
        {
            contexto.CancellationToken.ThrowIfCancellationRequested();

            var linha = new Dictionary<string, object?>();
            
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var nomeColuna = reader.GetName(i);
                var valor = reader.IsDBNull(i) ? null : reader.GetValue(i);
                linha[nomeColuna] = valor;
            }

            lote.Add(linha);
            linhaAtual++;

            if (lote.Count >= tamanhoLote)
            {
                yield return new LoteDados
                {
                    NumeroLote = ++numeroLote,
                    Linhas = lote.ToList(),
                    TabelaOrigem = tabelaJob.TabelaOrigem
                };

                contexto.ReportarProgresso(
                    $"Extraídas {linhaAtual} linhas de {tabelaJob.TabelaOrigem}",
                    0); // Percentual será calculado pelo motor

                lote.Clear();
            }
        }

        // Retorna lote parcial se houver
        if (lote.Count > 0)
        {
            yield return new LoteDados
            {
                NumeroLote = ++numeroLote,
                Linhas = lote.ToList(),
                TabelaOrigem = tabelaJob.TabelaOrigem
            };
        }
    }

    /// <summary>
    /// Constrói consulta SQL para extração de dados
    /// </summary>
    private string ConstruirConsultaExtracao(TabelaJob tabelaJob, object? checkpoint)
    {
        var colunas = tabelaJob.Mapeamentos
            .Select(m => m.ColunaOrigem)
            .ToList();

        var sql = $"SELECT {string.Join(", ", colunas)} FROM {tabelaJob.TabelaOrigem}";

        // Adiciona filtro de checkpoint para importação incremental
        if (checkpoint != null && !string.IsNullOrEmpty(tabelaJob.ColunaCheckpoint))
        {
            sql += $" WHERE {tabelaJob.ColunaCheckpoint} > @checkpoint";
        }

        // Adiciona ordenação pela coluna de checkpoint se existir
        if (!string.IsNullOrEmpty(tabelaJob.ColunaCheckpoint))
        {
            sql += $" ORDER BY {tabelaJob.ColunaCheckpoint}";
        }

        return sql;
    }

    /// <summary>
    /// Extrai schema da tabela de origem
    /// </summary>
    public async Task<DataTable> ExtrairSchemaAsync(
        IDbConnection conexao,
        string nomeTabela)
    {
        var sql = $"SELECT * FROM {nomeTabela} WHERE 1=0";
        
        using var reader = await _conectorOrigem.ExecutarConsultaAsync(conexao, sql);
        
        var schema = new DataTable();
        schema.Load(reader);
        
        return schema;
    }
}

/// <summary>
/// Representa um lote de dados extraídos
/// </summary>
public class LoteDados
{
    public int NumeroLote { get; set; }
    public List<Dictionary<string, object?>> Linhas { get; set; } = new();
    public string TabelaOrigem { get; set; } = string.Empty;
}
