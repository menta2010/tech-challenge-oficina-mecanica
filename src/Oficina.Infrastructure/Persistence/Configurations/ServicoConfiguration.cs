using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Servicos;

namespace Oficina.Infrastructure.Persistence.Configurations;

public sealed class ServicoConfiguration : IEntityTypeConfiguration<Servico>
{
    public void Configure(EntityTypeBuilder<Servico> b)
    {
        b.ToTable("Servicos");
        b.HasKey(s => s.Id);
        b.Property(s => s.Nome).IsRequired().HasMaxLength(160);
        b.Property(s => s.Descricao).HasMaxLength(500);
        b.Property(s => s.TempoEstimado).IsRequired();
        b.Property(s => s.ValorBase)
            .HasConversion(Converters.Money)
            .HasColumnType("numeric(18,2)")
            .IsRequired();
    }
}
