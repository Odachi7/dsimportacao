using DSI.Dominio.Entidades;
using DSI.Persistencia.Contexto;
using Microsoft.EntityFrameworkCore;

namespace DSI.Motor.ETL;

/// <summary>
/// Gerenciador de checkpoints para importações incrementais
/// Armazena e recupera pontos de checkpoint para retomada de importações
/// </summary>
public class GerenciadorCheckpoint
{
    private readonly DsiDbContext _context;

    public GerenciadorCheckpoint(DsiDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Salva checkpoint de uma tabela
    /// </summary>
    public async Task SalvarCheckpointAsync(
        Guid execucaoId,
        string tabela,
        object valorCheckpoint)
    {
        // Busca job da execução
        var execucao = await _context.Execucoes.FindAsync(execucaoId);
        if (execucao == null)
            throw new InvalidOperationException($"Execução {execucaoId} não encontrada");

        var checkpoint = await _context.Checkpoints
            .FirstOrDefaultAsync(c => c.JobId == execucao.JobId && c.NomeTabela == tabela);

        if (checkpoint == null)
        {
            checkpoint = new Checkpoint
            {
                Id = Guid.NewGuid(),
                JobId = execucao.JobId,
                NomeTabela = tabela,
                ValorCheckpoint = System.Text.Json.JsonSerializer.Serialize(valorCheckpoint),
                DataHora = DateTime.Now
            };
            _context.Checkpoints.Add(checkpoint);
        }
        else
        {
            checkpoint.ValorCheckpoint = System.Text.Json.JsonSerializer.Serialize(valorCheckpoint);
            checkpoint.DataHora = DateTime.Now;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Recupera último checkpoint de uma tabela
    /// </summary>
    public async Task<object?> RecuperarCheckpointAsync(
        Guid jobId,
        string tabela)
    {
        var checkpoint = await _context.Checkpoints
            .Where(c => c.JobId == jobId && c.NomeTabela == tabela)
            .OrderByDescending(c => c.DataHora)
            .FirstOrDefaultAsync();

        if (checkpoint == null)
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<object>(checkpoint.ValorCheckpoint);
        }
        catch
        {
            // Se falhar deserialização, retorna string raw
            return checkpoint.ValorCheckpoint;
        }
    }

    /// <summary>
    /// Limpa checkpoints antigos
    /// </summary>
    public async Task LimparCheckpointsAntigosAsync(int diasRetencao)
    {
        var dataLimite = DateTime.Now.AddDays(-diasRetencao);
        
        var checkpointsAntigos = await _context.Checkpoints
            .Where(c => c.DataHora < dataLimite)
            .ToListAsync();

        _context.Checkpoints.RemoveRange(checkpointsAntigos);
        await _context.SaveChangesAsync();
    }
}
