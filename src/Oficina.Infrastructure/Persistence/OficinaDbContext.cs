using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Clientes;
using Oficina.Domain.Estoque;
using Oficina.Domain.Identidade;
using Oficina.Domain.Lojas;
using Oficina.Domain.OrdensServico;
using Oficina.Domain.Servicos;
using Oficina.Domain.Veiculos;

namespace Oficina.Infrastructure.Persistence;

public sealed class OficinaDbContext : DbContext
{
    public OficinaDbContext(DbContextOptions<OficinaDbContext> options) : base(options) { }

    public DbSet<Loja> Lojas => Set<Loja>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Veiculo> Veiculos => Set<Veiculo>();
    public DbSet<Servico> Servicos => Set<Servico>();
    public DbSet<PecaInsumo> PecasInsumos => Set<PecaInsumo>();
    public DbSet<EstoqueLoja> EstoquesLoja => Set<EstoqueLoja>();
    public DbSet<OrdemServico> OrdensServico => Set<OrdemServico>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OficinaDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
