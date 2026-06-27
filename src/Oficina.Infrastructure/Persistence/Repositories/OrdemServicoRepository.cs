using Microsoft.EntityFrameworkCore;
using Oficina.Application.Abstractions;
using Oficina.Domain.OrdensServico;

namespace Oficina.Infrastructure.Persistence.Repositories;

public sealed class OrdemServicoRepository : EfRepository<OrdemServico>, IOrdemServicoRepository
{
    public OrdemServicoRepository(OficinaDbContext db) : base(db) { }

    private IQueryable<OrdemServico> ComItens()
        => Db.OrdensServico.Include(o => o.Servicos).Include(o => o.Pecas);

    public override Task<OrdemServico?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => ComItens().FirstOrDefaultAsync(o => o.Id == id, ct);

    public override Task<List<OrdemServico>> ListAsync(CancellationToken ct = default)
        => ComItens().OrderByDescending(o => o.CriadaEm).ToListAsync(ct);

    public Task<List<OrdemServico>> ListByStatusAsync(StatusOS status, CancellationToken ct = default)
        => ComItens().Where(o => o.Status == status).ToListAsync(ct);

    public Task<List<OrdemServico>> ListFinalizadasOuEntreguesAsync(CancellationToken ct = default)
        => Db.OrdensServico
              .Where(o => o.Status == StatusOS.Finalizada || o.Status == StatusOS.Entregue)
              .ToListAsync(ct);
}
