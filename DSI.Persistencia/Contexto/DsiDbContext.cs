using DSI.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;

namespace DSI.Persistencia.Contexto;

/// <summary>
/// Contexto do banco de dados SQLite para o sistema DSI
/// </summary>
public class DsiDbContext : DbContext
{
    public DsiDbContext(DbContextOptions<DsiDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Conexões configuradas
    /// </summary>
    public DbSet<Conexao> Conexoes => Set<Conexao>();

    /// <summary>
    /// Jobs configurados
    /// </summary>
    public DbSet<Job> Jobs => Set<Job>();

    /// <summary>
    /// Tabelas de jobs
    /// </summary>
    public DbSet<TabelaJob> TabelasJobs => Set<TabelaJob>();

    /// <summary>
    /// Mapeamentos de colunas
    /// </summary>
    public DbSet<Mapeamento> Mapeamentos => Set<Mapeamento>();

    /// <summary>
    /// Regras de transformação
    /// </summary>
    public DbSet<Regra> Regras => Set<Regra>();

    /// <summary>
    /// Execuções (runs) de jobs
    /// </summary>
    public DbSet<Execucao> Execucoes => Set<Execucao>();

    /// <summary>
    /// Estatísticas de execução por tabela
    /// </summary>
    public DbSet<EstatisticaTabelaExecucao> EstatisticasTabelas => Set<EstatisticaTabelaExecucao>();

    /// <summary>
    /// Erros de execução
    /// </summary>
    public DbSet<ErroExecucao> ErrosExecucao => Set<ErroExecucao>();

    /// <summary>
    /// Checkpoints de importações incrementais
    /// </summary>
    public DbSet<Checkpoint> Checkpoints => Set<Checkpoint>();

    /// <summary>
    /// Lookups (De/Para)
    /// </summary>
    public DbSet<Lookup> Lookups => Set<Lookup>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplicar todas as configurações de entidades
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DsiDbContext).Assembly);
    }
}
