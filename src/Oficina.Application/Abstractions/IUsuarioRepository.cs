using Oficina.Domain.Identidade;

namespace Oficina.Application.Abstractions;

public interface IUsuarioRepository : IRepository<Usuario>
{
    Task<Usuario?> GetByUsernameAsync(string username, CancellationToken ct = default);
}
