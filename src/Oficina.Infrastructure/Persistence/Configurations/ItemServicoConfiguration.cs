using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.OrdensServico;

namespace Oficina.Infrastructure.Persistence.Configurations;

public sealed class ItemServicoConfiguration : IEntityTypeConfiguration<ItemServico>
{
    public void Configure(EntityTypeBuilder<ItemServico> b)
    {
        b.ToTable("ItensServico");
        b.HasKey(i => i.Id);
        b.Property(i => i.Id).ValueGeneratedNever();
        b.Property(i => i.ServicoId).IsRequired();
        b.Property(i => i.Descricao).HasMaxLength(300);
        b.Property(i => i.TempoEstimado).IsRequired();
        b.Property(i => i.Executado).IsRequired();
        b.Property(i => i.Valor).HasConversion(Converters.Money).HasColumnType("numeric(18,2)").IsRequired();
    }
}
