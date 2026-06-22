using Oficina.Domain.OrdensServico;

namespace Oficina.Application.Abstractions;

public interface IOrdemServicoRepository : IRepository<OrdemServico>
{
    Task<List<OrdemServico>> ListByStatusAsync(StatusOS status, CancellationToken ct = default);
    Task<List<OrdemServico>> ListFinalizadasOuEntreguesAsync(CancellationToken ct = default);
}
