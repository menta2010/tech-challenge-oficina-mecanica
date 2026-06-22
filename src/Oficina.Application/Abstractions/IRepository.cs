using Oficina.Domain.Shared;

namespace Oficina.Application.Abstractions;

/// <summary>Contrato de persistencia generico por agregado. Implementado na Infrastructure (EF Core).</summary>
public interface IRepository<T> where T : Entity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<T>> ListAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
}
