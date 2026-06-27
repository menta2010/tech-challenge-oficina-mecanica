using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.OrdensServico;

namespace Oficina.Infrastructure.Persistence.Configurations;

public sealed class ItemPecaConfiguration : IEntityTypeConfiguration<ItemPeca>
{
    public void Configure(EntityTypeBuilder<ItemPeca> b)
    {
        b.ToTable("ItensPeca");
        b.HasKey(i => i.Id);
        b.Property(i => i.Id).ValueGeneratedNever();
        b.Property(i => i.PecaId).IsRequired();
        b.Property(i => i.Quantidade).IsRequired();
        b.Property(i => i.Utilizado).IsRequired();
        b.Property(i => i.ValorUnitario).HasConversion(Converters.Money).HasColumnType("numeric(18,2)").IsRequired();
        b.Ignore(i => i.Subtotal);
    }
}
