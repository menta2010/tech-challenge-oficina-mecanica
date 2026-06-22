using Microsoft.EntityFrameworkCore;
using Oficina.Application.Abstractions;
using Oficina.Domain.OrdensServico;

namespace Oficina.Infrastructure.Persistence.Repositories;

public sealed class OrdemServicoRepository : EfRepository<OrdemServico>, IOrdemServicoRepository
{
    public OrdemServicoRepository(OficinaDbContext db) : base(db) { }

    // Owned types (itens/orcamento) sao incluidos automaticamente pelo EF.
    public override Task<OrdemServico?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Db.OrdensServico.FirstOrDefaultAsync(o => o.Id == id, ct);

    public override Task<List<OrdemServico>> ListAsync(CancellationToken ct = default)
        => Db.OrdensServico.OrderByDescending(o => o.CriadaEm).ToListAsync(ct);

    public Task<List<OrdemServico>> ListByStatusAsync(StatusOS status, CancellationToken ct = default)
        => Db.OrdensServico.Where(o => o.Status == status).ToListAsync(ct);

    public Task<List<OrdemServico>> ListFinalizadasOuEntreguesAsync(CancellationToken ct = default)
        => Db.OrdensServico
              .Where(o => o.Status == StatusOS.Finalizada || o.Status == StatusOS.Entregue)
              .ToListAsync(ct);
}
