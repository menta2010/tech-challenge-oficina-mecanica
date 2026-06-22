using Oficina.Domain.Clientes;

namespace Oficina.Application.Abstractions;

public interface IClienteRepository : IRepository<Cliente>
{
    Task<Cliente?> GetByDocumentoAsync(Documento documento, CancellationToken ct = default);
    Task<bool> ExisteDocumentoAsync(Documento documento, CancellationToken ct = default);
}
