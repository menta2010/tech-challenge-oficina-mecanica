using Microsoft.EntityFrameworkCore;
using Oficina.Application.Abstractions;
using Oficina.Domain.Identidade;

namespace Oficina.Infrastructure.Persistence.Repositories;

public sealed class UsuarioRepository : EfRepository<Usuario>, IUsuarioRepository
{
    public UsuarioRepository(OficinaDbContext db) : base(db) { }

    public Task<Usuario?> GetByUsernameAsync(string username, CancellationToken ct = default)
        => Db.Set<Usuario>().FirstOrDefaultAsync(u => u.Username == username, ct);
}
