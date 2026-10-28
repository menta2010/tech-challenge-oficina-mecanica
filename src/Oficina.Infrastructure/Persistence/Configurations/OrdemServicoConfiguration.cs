using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Clientes;
using Oficina.Domain.Lojas;
using Oficina.Domain.OrdensServico;
using Oficina.Domain.Veiculos;

namespace Oficina.Infrastructure.Persistence.Configurations;

public sealed class OrdemServicoConfiguration : IEntityTypeConfiguration<OrdemServico>
{
    public void Configure(EntityTypeBuilder<OrdemServico> b)
    {
        b.ToTable("OrdensServico");
        b.HasKey(o => o.Id);
        b.Property(o => o.Id).ValueGeneratedNever();

        b.Property(o => o.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        b.Property(o => o.Encerrada).IsRequired();

        b.Property(o => o.CriadaEm).IsRequired();
        b.Ignore(o => o.TempoExecucao);

        // Integridade referencial (antes eram so colunas Guid soltas)
        b.HasOne<Loja>().WithMany().HasForeignKey(o => o.LojaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Cliente>().WithMany().HasForeignKey(o => o.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Veiculo>().WithMany().HasForeignKey(o => o.VeiculoId).OnDelete(DeleteBehavior.Restrict);

        // Listagem operacional (exclusao logica): WHERE Encerrada = false AND LojaId = ? ORDER BY Status, CriadaEm
        b.HasIndex(o => new { o.Encerrada, o.LojaId, o.Status, o.CriadaEm })
            .HasDatabaseName("IX_OrdensServico_Listagem");
        b.HasIndex(o => o.ClienteId);

        // Orcamento: owned na mesma tabela (colunas prefixadas)
        b.OwnsOne(o => o.Orcamento, ob =>
        {
            ob.Property(x => x.ValorServicos).HasConversion(Converters.Money).HasColumnType("numeric(18,2)").HasColumnName("Orcamento_ValorServicos");
            ob.Property(x => x.ValorPecas).HasConversion(Converters.Money).HasColumnType("numeric(18,2)").HasColumnName("Orcamento_ValorPecas");
            ob.Property(x => x.ValorTotal).HasConversion(Converters.Money).HasColumnType("numeric(18,2)").HasColumnName("Orcamento_ValorTotal");
            ob.Property(x => x.PrevisaoEntrega).HasColumnName("Orcamento_PrevisaoEntrega");
            ob.Property(x => x.GeradoEm).HasColumnName("Orcamento_GeradoEm");
        });

        // Itens e historico como entidades filhas (relacao 1-N)
        b.HasMany(o => o.Servicos).WithOne().HasForeignKey("OrdemServicoId").OnDelete(DeleteBehavior.Cascade);
        b.Navigation(o => o.Servicos).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.HasMany(o => o.Pecas).WithOne().HasForeignKey("OrdemServicoId").OnDelete(DeleteBehavior.Cascade);
        b.Navigation(o => o.Pecas).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.HasMany(o => o.Historico).WithOne().HasForeignKey("OrdemServicoId").OnDelete(DeleteBehavior.Cascade);
        b.Navigation(o => o.Historico).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class HistoricoStatusOSConfiguration : IEntityTypeConfiguration<HistoricoStatusOS>
{
    public void Configure(EntityTypeBuilder<HistoricoStatusOS> b)
    {
        b.ToTable("HistoricoStatusOS");
        b.HasKey(h => h.Id);
        b.Property(h => h.Id).ValueGeneratedNever();
        b.Property(h => h.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        b.Property(h => h.Inicio).IsRequired();
        b.Ignore(h => h.Duracao);
        b.HasIndex("OrdemServicoId", nameof(HistoricoStatusOS.Inicio));
    }
}
