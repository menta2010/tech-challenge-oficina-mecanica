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

        b.Property(o => o.ClienteId).IsRequired();
        b.Property(o => o.VeiculoId).IsRequired();
        b.Property(o => o.Status).HasConversion<string>().HasMaxLength(30).IsRequired();

        b.Property(o => o.CriadaEm).IsRequired();
        b.Property(o => o.DiagnosticoIniciadoEm);
        b.Property(o => o.ExecucaoIniciadaEm);
        b.Property(o => o.ExecucaoFinalizadaEm);
        b.Property(o => o.EntregueEm);
        b.Property(o => o.CanceladaEm);

        b.Ignore(o => o.TempoExecucao);   // calculado, nao persistido

        b.HasIndex(o => o.Status);
        b.HasIndex(o => o.ClienteId);

        // Orcamento como objeto owned (mesma tabela, colunas prefixadas)
        b.OwnsOne(o => o.Orcamento, ob =>
        {
            ob.Property(x => x.ValorServicos).HasConversion(Converters.Money).HasColumnType("numeric(18,2)").HasColumnName("Orcamento_ValorServicos");
            ob.Property(x => x.ValorPecas).HasConversion(Converters.Money).HasColumnType("numeric(18,2)").HasColumnName("Orcamento_ValorPecas");
            ob.Property(x => x.ValorTotal).HasConversion(Converters.Money).HasColumnType("numeric(18,2)").HasColumnName("Orcamento_ValorTotal");
            ob.Property(x => x.PrevisaoEntrega).HasColumnName("Orcamento_PrevisaoEntrega");
            ob.Property(x => x.GeradoEm).HasColumnName("Orcamento_GeradoEm");
        });

        // Itens de servico como entidades owned (tabela propria, acesso por field)
        b.OwnsMany(o => o.Servicos, sb =>
        {
            sb.ToTable("ItensServico");
            sb.WithOwner().HasForeignKey("OrdemServicoId");
            sb.HasKey(i => i.Id);
            sb.Property(i => i.ServicoId).IsRequired();
            sb.Property(i => i.Descricao).HasMaxLength(300);
            sb.Property(i => i.TempoEstimado).IsRequired();
            sb.Property(i => i.Executado).IsRequired();
            sb.Property(i => i.Valor).HasConversion(Converters.Money).HasColumnType("numeric(18,2)").IsRequired();
            sb.UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        b.Navigation(o => o.Servicos).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Itens de peca como entidades owned
        b.OwnsMany(o => o.Pecas, pb =>
        {
            pb.ToTable("ItensPeca");
            pb.WithOwner().HasForeignKey("OrdemServicoId");
            pb.HasKey(i => i.Id);
            pb.Property(i => i.PecaId).IsRequired();
            pb.Property(i => i.Quantidade).IsRequired();
            pb.Property(i => i.Utilizado).IsRequired();
            pb.Property(i => i.ValorUnitario).HasConversion(Converters.Money).HasColumnType("numeric(18,2)").IsRequired();
            pb.Ignore(i => i.Subtotal);
            pb.UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        b.Navigation(o => o.Pecas).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
