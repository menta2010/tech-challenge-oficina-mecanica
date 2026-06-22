using Microsoft.EntityFrameworkCore;
using Oficina.Application.Abstractions;
using Oficina.Domain.Clientes;

namespace Oficina.Infrastructure.Persistence.Repositories;

public sealed class ClienteRepository : EfRepository<Cliente>, IClienteRepository
{
    public ClienteRepository(OficinaDbContext db) : base(db) { }

    public Task<Cliente?> GetByDocumentoAsync(Documento documento, CancellationToken ct = default)
        => Db.Clientes.FirstOrDefaultAsync(c => c.Documento == documento, ct);

    public Task<bool> ExisteDocumentoAsync(Documento documento, CancellationToken ct = default)
        => Db.Clientes.AnyAsync(c => c.Documento == documento, ct);
}
