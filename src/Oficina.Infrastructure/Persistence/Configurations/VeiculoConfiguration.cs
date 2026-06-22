using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Veiculos;

namespace Oficina.Infrastructure.Persistence.Configurations;

public sealed class VeiculoConfiguration : IEntityTypeConfiguration<Veiculo>
{
    public void Configure(EntityTypeBuilder<Veiculo> b)
    {
        b.ToTable("Veiculos");
        b.HasKey(v => v.Id);
        b.Property(v => v.ClienteId).IsRequired();
        b.Property(v => v.Marca).IsRequired().HasMaxLength(80);
        b.Property(v => v.Modelo).IsRequired().HasMaxLength(120);
        b.Property(v => v.Ano).IsRequired();
        b.Property(v => v.Placa)
            .HasConversion(Converters.Placa)
            .HasColumnName("Placa")
            .HasMaxLength(7)
            .IsRequired();
        b.HasIndex(v => v.Placa).IsUnique();
        b.HasIndex(v => v.ClienteId);
    }
}
