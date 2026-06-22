using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Estoque;

namespace Oficina.Infrastructure.Persistence.Configurations;

public sealed class PecaInsumoConfiguration : IEntityTypeConfiguration<PecaInsumo>
{
    public void Configure(EntityTypeBuilder<PecaInsumo> b)
    {
        b.ToTable("PecasInsumos");
        b.HasKey(p => p.Id);
        b.Property(p => p.Nome).IsRequired().HasMaxLength(160);
        b.Property(p => p.QuantidadeEmEstoque).IsRequired();
        b.Property(p => p.ValorUnitario)
            .HasConversion(Converters.Money)
            .HasColumnType("numeric(18,2)")
            .IsRequired();
    }
}
