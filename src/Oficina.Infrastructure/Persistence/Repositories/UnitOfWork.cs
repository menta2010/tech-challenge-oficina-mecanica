using Oficina.Application.Abstractions;

namespace Oficina.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly OficinaDbContext _db;
    public UnitOfWork(OficinaDbContext db) => _db = db;

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
