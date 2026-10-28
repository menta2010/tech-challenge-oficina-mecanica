using Oficina.Domain.OrdensServico;

namespace Oficina.Application.Abstractions;

/// <summary>Filtro da listagem de OS. LojaId/ClienteId nulos = sem restricao.</summary>
public sealed record FiltroOrdensServico(
    StatusOS? Status = null,
    bool IncluirEncerradas = false,
    Guid? LojaId = null,
    Guid? ClienteId = null);

public interface IOrdemServicoRepository : IRepository<OrdemServico>
{
    Task<List<OrdemServico>> ListarAsync(FiltroOrdensServico filtro, CancellationToken ct = default);
    Task<List<OrdemServico>> ListarComExecucaoConcluidaAsync(Guid? lojaId, CancellationToken ct = default);
}
