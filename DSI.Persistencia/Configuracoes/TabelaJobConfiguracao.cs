using DSI.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DSI.Persistencia.Configuracoes;

/// <summary>
/// Configuração da entidade TabelaJob para o EF Core
/// </summary>
public class TabelaJobConfiguracao : IEntityTypeConfiguration<TabelaJob>
{
    public void Configure(EntityTypeBuilder<TabelaJob> builder)
    {
        builder.ToTable("TabelasJobs");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.JobId)
            .IsRequired();

        builder.Property(t => t.TabelaOrigem)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.TabelaDestino)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.OrdemExecucao)
            .IsRequired();

        builder.Property(t => t.ColunaCheckpoint)
            .HasMaxLength(100);

        builder.Property(t => t.UltimoCheckpoint)
            .HasMaxLength(500);

        // Relacionamentos
        builder.HasMany(t => t.Mapeamentos)
            .WithOne()
            .HasForeignKey(m => m.TabelaJobId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índices
        builder.HasIndex(t => t.JobId);
        builder.HasIndex(t => new { t.JobId, t.OrdemExecucao });
    }
}
