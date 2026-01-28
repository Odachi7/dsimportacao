using DSI.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DSI.Persistencia.Configuracoes;

/// <summary>
/// Configuração da entidade ErroExecucao para o EF Core
/// </summary>
public class ErroExecucaoConfiguracao : IEntityTypeConfiguration<ErroExecucao>
{
    public void Configure(EntityTypeBuilder<ErroExecucao> builder)
    {
        builder.ToTable("ErrosExecucao");

        builder.HasKey(er => er.Id);

        builder.Property(er => er.ExecucaoId)
            .IsRequired();

        builder.Property(er => er.TabelaJobId)
            .IsRequired();

        builder.Property(er => er.ChaveLinha)
            .HasMaxLength(200);

        builder.Property(er => er.Coluna)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(er => er.TipoRegra)
            .HasConversion<int?>();

        builder.Property(er => er.ValorOriginal)
            .HasMaxLength(1000);

        builder.Property(er => er.Mensagem)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(er => er.OcorridoEm)
            .IsRequired();

        // Índices
        builder.HasIndex(er => er.ExecucaoId);
        builder.HasIndex(er => er.TabelaJobId);
        builder.HasIndex(er => er.OcorridoEm);
    }
}
