using DSI.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DSI.Persistencia.Configuracoes;

/// <summary>
/// Configuração da entidade Regra para o EF Core
/// </summary>
public class RegraConfiguracao : IEntityTypeConfiguration<Regra>
{
    public void Configure(EntityTypeBuilder<Regra> builder)
    {
        builder.ToTable("Regras");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.MapeamentoId)
            .IsRequired();

        builder.Property(r => r.TipoRegra)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(r => r.ParametrosJson)
            .IsRequired()
            .HasMaxLength(4000)
            .HasDefaultValue("{}");

        builder.Property(r => r.AcaoQuandoFalhar)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(r => r.Ordem)
            .IsRequired();

        // Índices
        builder.HasIndex(r => r.MapeamentoId);
        builder.HasIndex(r => new { r.MapeamentoId, r.Ordem });
    }
}
