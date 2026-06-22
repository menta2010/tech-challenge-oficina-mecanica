using Microsoft.EntityFrameworkCore;
using Oficina.Application.Abstractions;
using Oficina.Domain.Veiculos;

namespace Oficina.Infrastructure.Persistence.Repositories;

public sealed class VeiculoRepository : EfRepository<Veiculo>, IVeiculoRepository
{
    public VeiculoRepository(OficinaDbContext db) : base(db) { }

    public Task<Veiculo?> GetByPlacaAsync(Placa placa, CancellationToken ct = default)
        => Db.Veiculos.FirstOrDefaultAsync(v => v.Placa == placa, ct);

    public Task<List<Veiculo>> ListByClienteAsync(Guid clienteId, CancellationToken ct = default)
        => Db.Veiculos.Where(v => v.ClienteId == clienteId).ToListAsync(ct);
}
