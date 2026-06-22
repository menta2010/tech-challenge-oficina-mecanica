using Oficina.Application.Abstractions;
using Oficina.Domain.Estoque;

namespace Oficina.Infrastructure.Persistence.Repositories;

public sealed class PecaInsumoRepository : EfRepository<PecaInsumo>, IPecaInsumoRepository
{
    public PecaInsumoRepository(OficinaDbContext db) : base(db) { }
}
