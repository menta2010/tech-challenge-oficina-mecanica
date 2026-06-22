using Oficina.Domain.Veiculos;

namespace Oficina.Application.Abstractions;

public interface IVeiculoRepository : IRepository<Veiculo>
{
    Task<Veiculo?> GetByPlacaAsync(Placa placa, CancellationToken ct = default);
    Task<List<Veiculo>> ListByClienteAsync(Guid clienteId, CancellationToken ct = default);
}
