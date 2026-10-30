using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Clientes;
using Oficina.Domain.Lojas;

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
        // Sem HasDefaultValue: em bool o EF trataria "false" como nao informado e o banco gravaria true
        b.Property(c => c.Ativo).IsRequired();
        b.Property(c => c.CriadoEm).IsRequired();

        // Busca da autenticacao por CPF (Lambda): igualdade no documento, indice unico
        b.HasIndex(c => c.Documento).IsUnique();
        // Politica de inativacao: o job seleciona apenas clientes ativos
        b.HasIndex(c => c.Ativo);
    }
}

public sealed class LojaConfiguration : IEntityTypeConfiguration<Loja>
{
    public void Configure(EntityTypeBuilder<Loja> b)
    {
        b.ToTable("Lojas");
        b.HasKey(l => l.Id);
        b.Property(l => l.Nome).IsRequired().HasMaxLength(120);
        b.Property(l => l.Cidade).IsRequired().HasMaxLength(120);
        b.Property(l => l.Ativa).IsRequired();
        b.HasIndex(l => l.Nome).IsUnique();
    }
}
