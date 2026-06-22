using Oficina.Application.Abstractions;
using Oficina.Domain.Servicos;

namespace Oficina.Infrastructure.Persistence.Repositories;

public sealed class ServicoRepository : EfRepository<Servico>, IServicoRepository
{
    public ServicoRepository(OficinaDbContext db) : base(db) { }
}
