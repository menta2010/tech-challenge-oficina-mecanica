using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.OrdensServico;

namespace Oficina.Infrastructure.Persistence.Configurations;

public sealed class OrdemServicoConfiguration : IEntityTypeConfiguration<OrdemServico>
{
    public void Configure(EntityTypeBuilder<OrdemServico> b)
    {
        b.ToTable("OrdensServico");
        b.HasKey(o => o.Id);
        b.Property(o => o.Id).ValueGeneratedNever();

        b.Property(o => o.ClienteId).IsRequired();
        b.Property(o => o.VeiculoId).IsRequired();
        b.Property(o => o.Status).HasConversion<string>().HasMaxLength(30).IsRequired();

        b.Property(o => o.CriadaEm).IsRequired();
        b.Property(o => o.DiagnosticoIniciadoEm);
        b.Property(o => o.ExecucaoIniciadaEm);
        b.Property(o => o.ExecucaoFinalizadaEm);
        b.Property(o => o.EntregueEm);
        b.Property(o => o.CanceladaEm);

        b.Ignore(o => o.TempoExecucao);

        b.HasIndex(o => o.Status);
        b.HasIndex(o => o.ClienteId);

        // Orcamento: owned na mesma tabela (colunas prefixadas) - funciona bem (nao e colecao)
        b.OwnsOne(o => o.Orcamento, ob =>
        {
            ob.Property(x => x.ValorServicos).HasConversion(Converters.Money).HasColumnType("numeric(18,2)").HasColumnName("Orcamento_ValorServicos");
            ob.Property(x => x.ValorPecas).HasConversion(Converters.Money).HasColumnType("numeric(18,2)").HasColumnName("Orcamento_ValorPecas");
            ob.Property(x => x.ValorTotal).HasConversion(Converters.Money).HasColumnType("numeric(18,2)").HasColumnName("Orcamento_ValorTotal");
            ob.Property(x => x.PrevisaoEntrega).HasColumnName("Orcamento_PrevisaoEntrega");
            ob.Property(x => x.GeradoEm).HasColumnName("Orcamento_GeradoEm");
        });

        // Itens como entidades normais (relacao 1-N). Mesmo schema das tabelas anteriores.
        b.HasMany(o => o.Servicos).WithOne().HasForeignKey("OrdemServicoId").OnDelete(DeleteBehavior.Cascade);
        b.Navigation(o => o.Servicos).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.HasMany(o => o.Pecas).WithOne().HasForeignKey("OrdemServicoId").OnDelete(DeleteBehavior.Cascade);
        b.Navigation(o => o.Pecas).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
