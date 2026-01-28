using DSI.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DSI.Persistencia.Configuracoes;

/// <summary>
/// Configuração da entidade Job para o EF Core
/// </summary>
public class JobConfiguracao : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("Jobs");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.Nome)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(j => j.ConexaoOrigemId)
            .IsRequired();

        builder.Property(j => j.ConexaoDestinoId)
            .IsRequired();

        builder.Property(j => j.Modo)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(j => j.TamanhoLote)
            .IsRequired()
            .HasDefaultValue(1000);

        builder.Property(j => j.PoliticaErro)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(j => j.EstrategiaConflito)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(j => j.CriadoEm)
            .IsRequired();

        builder.Property(j => j.AtualizadoEm)
            .IsRequired();

        // Relacionamentos
        builder.HasMany(j => j.Tabelas)
            .WithOne()
            .HasForeignKey(t => t.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índices
        builder.HasIndex(j => j.Nome);
        builder.HasIndex(j => j.ConexaoOrigemId);
        builder.HasIndex(j => j.ConexaoDestinoId);
    }
}
