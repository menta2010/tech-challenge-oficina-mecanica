using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Clientes;

namespace Oficina.Infrastructure.Persistence.Configurations;

public sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> b)
    {
        b.ToTable("Clientes");
        b.HasKey(c => c.Id);
        b.Property(c => c.Nome).IsRequired().HasMaxLength(200);
        b.Property(c => c.Email).HasMaxLength(200);
        b.Property(c => c.Telefone).HasMaxLength(40);
        b.Property(c => c.Documento)
            .HasConversion(Converters.Documento)
            .HasColumnName("Documento")
            .HasMaxLength(14)
            .IsRequired();
        b.HasIndex(c => c.Documento).IsUnique();
    }
}
