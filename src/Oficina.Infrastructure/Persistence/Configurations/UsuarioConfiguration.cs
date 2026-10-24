using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Identidade;
using Oficina.Domain.Lojas;

namespace Oficina.Infrastructure.Persistence.Configurations;

public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("Usuarios");
        b.HasKey(u => u.Id);
        b.Property(u => u.Username).IsRequired().HasMaxLength(80);
        b.Property(u => u.PasswordHash).IsRequired().HasMaxLength(300);
        b.Property(u => u.Role).IsRequired().HasMaxLength(40);
        b.HasIndex(u => u.Username).IsUnique();

        b.HasOne<Loja>().WithMany().HasForeignKey(u => u.LojaId).OnDelete(DeleteBehavior.Restrict);
    }
}
