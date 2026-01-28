using DSI.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DSI.Persistencia.Configuracoes;

/// <summary>
/// Configuração da entidade Execucao para o EF Core
/// </summary>
public class ExecucaoConfiguracao : IEntityTypeConfiguration<Execucao>
{
    public void Configure(EntityTypeBuilder<Execucao> builder)
    {
        builder.ToTable("Execucoes");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.JobId)
            .IsRequired();

        builder.Property(e => e.IniciadoEm)
            .IsRequired();

        builder.Property(e => e.FinalizadoEm);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.ResumoJson)
            .IsRequired()
            .HasMaxLength(8000)
            .HasDefaultValue("{}");

        // Relacionamentos
        builder.HasMany(e => e.EstatisticasTabelas)
            .WithOne()
            .HasForeignKey(et => et.ExecucaoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Erros)
            .WithOne()
            .HasForeignKey(er => er.ExecucaoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índices
        builder.HasIndex(e => e.JobId);
        builder.HasIndex(e => e.IniciadoEm);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => new { e.JobId, e.IniciadoEm });
    }
}
