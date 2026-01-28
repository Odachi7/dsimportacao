using DSI.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DSI.Persistencia.Configuracoes;

/// <summary>
/// Configuração da entidade Mapeamento para o EF Core
/// </summary>
public class MapeamentoConfiguracao : IEntityTypeConfiguration<Mapeamento>
{
    public void Configure(EntityTypeBuilder<Mapeamento> builder)
    {
        builder.ToTable("Mapeamentos");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.TabelaJobId)
            .IsRequired();

        builder.Property(m => m.ColunaOrigem)
            .HasMaxLength(200);

        builder.Property(m => m.ColunaDestino)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.TipoDestino)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.Ignorada)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(m => m.ValorConstante)
            .HasMaxLength(500);

        // Relacionamentos
        builder.HasMany(m => m.Regras)
            .WithOne()
            .HasForeignKey(r => r.MapeamentoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índices
        builder.HasIndex(m => m.TabelaJobId);
    }
}
