using DSI.Conectores.Abstracoes.Interfaces;
using DSI.Dominio.Entidades;
using DSI.Motor.ETL;
using DSI.Persistencia.Contexto;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace DSI.Aplicacao.Servicos;

/// <summary>
/// Serviço para preview de jobs (dry-run sem persistir dados)
/// </summary>
public class ServicoPreview
{
    private readonly DsiDbContext _context;

    public ServicoPreview(DsiDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Executa preview de um job retornando amostra de dados transformados
    /// </summary>
    public async Task<ResultadoPreviewDto> ExecutarPreviewAsync(
        Guid jobId,
        IConector conectorOrigem,
        int quantidadeLinhas = 100)
    {
        // Carrega job completo
        var job = await _context.Jobs
            .Include(j => j.Tabelas)
                .ThenInclude(t => t.Mapeamentos)
                    .ThenInclude(m => m.Regras)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job == null)
            throw new InvalidOperationException($"Job {jobId} não encontrado");

        var resultado = new ResultadoPreviewDto
        {
            JobId = jobId,
            NomeJob = job.Nome
        };

        // Obtém string de conexão
        var conexaoOrigem = await ObterConexaoAsync(job.ConexaoOrigemId);
        
        // Cria conexão
        using var conn = conectorOrigem.CriarConexao(conexaoOrigem.StringConexaoCriptografada);
        conn.Open();

        // Preview de cada tabela
        foreach (var tabelaJob in job.Tabelas.OrderBy(t => t.OrdemExecucao))
        {
            var previewTabela = await PreviewTabelaAsync(
                conectorOrigem,
                conn,
                tabelaJob,
                quantidadeLinhas);
            
            resultado.PreviewsTabelas.Add(previewTabela);
        }

        return resultado;
    }

    /// <summary>
    /// Obtém schema da tabela de origem
    /// </summary>
    public async Task<SchemaPreviewDto> ObterSchemaOrigemAsync(
        string stringConexao,
        string nomeTabela,
        IConector conector)
    {
        var infoTabela = await conector.DescobrirSchemaTabelaAsync(stringConexao, nomeTabela);
        
        return new SchemaPreviewDto
        {
            NomeTabela = infoTabela.Nome,
            Colunas = infoTabela.Colunas.Select(c => new ColunaSchemaDto
            {
                Nome = c.Nome,
                TipoDados = c.TipoDados,
                TamanhoMaximo = c.Tamanho,
                Obrigatoria = !c.AceitaNulo,
                ChavePrimaria = c.EhChavePrimaria
            }).ToList()
        };
    }

    /// <summary>
    /// Valida configuração de um job
    /// </summary>
    public async Task<ResultadoValidacaoDto> ValidarJobAsync(Guid jobId)
    {
        var job = await _context.Jobs
            .Include(j => j.Tabelas)
                .ThenInclude(t => t.Mapeamentos)
                    .ThenInclude(m => m.Regras)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job == null)
            throw new InvalidOperationException($"Job {jobId} não encontrado");

        var resultado = new ResultadoValidacaoDto { JobId = jobId };

        // Validações básicas
        if (job.Tabelas.Count == 0)
        {
            resultado.Erros.Add("Job não possui tabelas configuradas");
        }

        foreach (var tabela in job.Tabelas)
        {
            if (tabela.Mapeamentos.Count == 0)
            {
                resultado.Avisos.Add($"Tabela '{tabela.TabelaOrigem}' não possui mapeamentos");
            }

            // Valida que todas as colunas de destino existem
            var colunasDuplicadas = tabela.Mapeamentos
                .GroupBy(m => m.ColunaDestino)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var coluna in colunasDuplicadas)
            {
                resultado.Erros.Add($"Tabela '{tabela.TabelaDestino}': coluna '{coluna}' mapeada mais de uma vez");
            }
        }

        resultado.Valido = resultado.Erros.Count == 0;
        return resultado;
    }

    // Métodos privados

    private async Task<PreviewTabelaDto> PreviewTabelaAsync(
        IConector conector,
        IDbConnection conexao,
        TabelaJob tabelaJob,
        int quantidadeLinhas)
    {
        var preview = new PreviewTabelaDto
        {
            TabelaOrigem = tabelaJob.TabelaOrigem,
            TabelaDestino = tabelaJob.TabelaDestino
        };

        try
        {
            // Monta SQL para buscar dados
            var colunas = tabelaJob.Mapeamentos
                .Select(m => m.ColunaOrigem)
                .Distinct();

            var sql = $"SELECT {string.Join(", ", colunas)} FROM {tabelaJob.TabelaOrigem}";
            
            // Adiciona LIMIT conforme banco
            sql += $" LIMIT {quantidadeLinhas}"; // TODO: Ajustar para SQL Server (TOP)

            // Executa consulta
            using var reader = await conector.ExecutarConsultaAsync(conexao, sql);

            // Lê linhas
            int contador = 0;
            while (reader.Read() && contador < quantidadeLinhas)
            {
                var linha = new Dictionary<string, object?>();
                
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var nomeColuna = reader.GetName(i);
                    var valor = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    linha[nomeColuna] = valor;
                }

                // Aplica transformações (mapeamentos)
                var linhaTransformada = AplicarMapeamentos(linha, tabelaJob.Mapeamentos.ToList());
                
                preview.LinhasAmostra.Add(new LinhaPreviewDto
                {
                    DadosOriginais = linha,
                    DadosTransformados = linhaTransformada
                });

                contador++;
            }

            preview.TotalLinhasLidas = contador;
        }
        catch (Exception ex)
        {
            preview.Erro = $"Erro ao fazer preview: {ex.Message}";
        }

        return preview;
    }

    private Dictionary<string, object?> AplicarMapeamentos(
        Dictionary<string, object?> linhaOriginal,
        List<Mapeamento> mapeamentos)
    {
        var resultado = new Dictionary<string, object?>();

        foreach (var mapeamento in mapeamentos)
        {
            var valorOriginal = linhaOriginal.GetValueOrDefault(mapeamento.ColunaOrigem);
            
            // Aqui aplicaríamos as regras, mas para preview simplificado apenas mapeamos
            // TODO: Integrar com motor de regras
            resultado[mapeamento.ColunaDestino] = valorOriginal;
        }

        return resultado;
    }

    private async Task<Conexao> ObterConexaoAsync(Guid conexaoId)
    {
        var conexao = await _context.Conexoes.FindAsync(conexaoId);
        if (conexao == null)
            throw new InvalidOperationException($"Conexão {conexaoId} não encontrada");
        
        return conexao;
    }
}

// DTOs

public class ResultadoPreviewDto
{
    public Guid JobId { get; set; }
    public string NomeJob { get; set; } = string.Empty;
    public List<PreviewTabelaDto> PreviewsTabelas { get; set; } = new();
}

public class PreviewTabelaDto
{
    public string TabelaOrigem { get; set; } = string.Empty;
    public string TabelaDestino { get; set; } = string.Empty;
    public int TotalLinhasLidas { get; set; }
    public List<LinhaPreviewDto> LinhasAmostra { get; set; } = new();
    public string? Erro { get; set; }
}

public class LinhaPreviewDto
{
    public Dictionary<string, object?> DadosOriginais { get; set; } = new();
    public Dictionary<string, object?> DadosTransformados { get; set; } = new();
}

public class SchemaPreviewDto
{
    public string NomeTabela { get; set; } = string.Empty;
    public List<ColunaSchemaDto> Colunas { get; set; } = new();
}

public class ColunaSchemaDto
{
    public string Nome { get; set; } = string.Empty;
    public string TipoDados { get; set; } = string.Empty;
    public int? TamanhoMaximo { get; set; }
    public bool Obrigatoria { get; set; }
    public bool ChavePrimaria { get; set; }
}

public class ResultadoValidacaoDto
{
    public Guid JobId { get; set; }
    public bool Valido { get; set; }
    public List<string> Erros { get; set; } = new();
    public List<string> Avisos { get; set; } = new();
}
