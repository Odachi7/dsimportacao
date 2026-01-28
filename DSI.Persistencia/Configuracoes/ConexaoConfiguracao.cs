using DSI.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DSI.Persistencia.Configuracoes;

/// <summary>
/// Configuração da entidade Conexao para o EF Core
/// </summary>
public class ConexaoConfiguracao : IEntityTypeConfiguration<Conexao>
{
    public void Configure(EntityTypeBuilder<Conexao> builder)
    {
        builder.ToTable("Conexoes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nome)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.TipoBanco)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(c => c.ModoConexao)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(c => c.StringConexaoCriptografada)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(c => c.CriadoEm)
            .IsRequired();

        builder.Property(c => c.AtualizadoEm)
            .IsRequired();

        // Índice para busca por nome
        builder.HasIndex(c => c.Nome);
    }
}
