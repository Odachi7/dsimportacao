using DSI.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DSI.Persistencia.Configuracoes;

/// <summary>
/// Configuração da entidade EstatisticaTabelaExecucao para o EF Core
/// </summary>
public class EstatisticaTabelaExecucaoConfiguracao : IEntityTypeConfiguration<EstatisticaTabelaExecucao>
{
    public void Configure(EntityTypeBuilder<EstatisticaTabelaExecucao> builder)
    {
        builder.ToTable("EstatisticasTabelas");

        builder.HasKey(et => et.Id);

        builder.Property(et => et.ExecucaoId)
            .IsRequired();

        builder.Property(et => et.TabelaJobId)
            .IsRequired();

        builder.Property(et => et.LinhasLidas)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(et => et.LinhasInseridas)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(et => et.LinhasAtualizadas)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(et => et.LinhasPuladas)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(et => et.LinhasComErro)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(et => et.DuracaoMs)
            .IsRequired()
            .HasDefaultValue(0);

        // Índices
        builder.HasIndex(et => et.ExecucaoId);
        builder.HasIndex(et => et.TabelaJobId);
    }
}
