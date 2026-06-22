using Microsoft.EntityFrameworkCore;
using Oficina.Application.Abstractions;
using Oficina.Domain.Shared;

namespace Oficina.Infrastructure.Persistence.Repositories;

/// <summary>Implementacao EF Core generica das operacoes basicas de agregado.</summary>
public class EfRepository<T> : IRepository<T> where T : Entity
{
    protected readonly OficinaDbContext Db;
    public EfRepository(OficinaDbContext db) => Db = db;

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await Db.Set<T>().FindAsync(new object?[] { id }, ct);

    public virtual async Task<List<T>> ListAsync(CancellationToken ct = default)
        => await Db.Set<T>().ToListAsync(ct);

    public async Task AddAsync(T entity, CancellationToken ct = default)
        => await Db.Set<T>().AddAsync(entity, ct);

    public void Update(T entity) => Db.Set<T>().Update(entity);

    public void Remove(T entity) => Db.Set<T>().Remove(entity);
}
