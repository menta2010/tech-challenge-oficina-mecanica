using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Estoque;
using Oficina.Domain.Lojas;

namespace Oficina.Infrastructure.Persistence.Configurations;

public sealed class PecaInsumoConfiguration : IEntityTypeConfiguration<PecaInsumo>
{
    public void Configure(EntityTypeBuilder<PecaInsumo> b)
    {
        b.ToTable("PecasInsumos");
        b.HasKey(p => p.Id);
        b.Property(p => p.Nome).IsRequired().HasMaxLength(160);
        b.Property(p => p.ValorUnitario)
            .HasConversion(Converters.Money)
            .HasColumnType("numeric(18,2)")
            .IsRequired();
    }
}

/// <summary>Saldo por loja: uma linha por (loja, peca). Quantidade nunca negativa (CHECK).</summary>
public sealed class EstoqueLojaConfiguration : IEntityTypeConfiguration<EstoqueLoja>
{
    public void Configure(EntityTypeBuilder<EstoqueLoja> b)
    {
        b.ToTable("EstoquesLoja", t => t.HasCheckConstraint("CK_EstoquesLoja_Quantidade", "\"Quantidade\" >= 0"));
        b.HasKey(e => e.Id);
        b.Property(e => e.Quantidade).IsRequired();
        b.HasIndex(e => new { e.LojaId, e.PecaInsumoId }).IsUnique();

        b.HasOne<Loja>().WithMany().HasForeignKey(e => e.LojaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PecaInsumo>().WithMany().HasForeignKey(e => e.PecaInsumoId).OnDelete(DeleteBehavior.Cascade);
    }
}
