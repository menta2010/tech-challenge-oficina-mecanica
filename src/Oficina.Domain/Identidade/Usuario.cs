using Oficina.Domain.Shared;

namespace Oficina.Domain.Identidade;

/// <summary>
/// Usuario administrativo do sistema (acesso as APIs protegidas por JWT).
/// A senha e armazenada apenas como hash.
/// </summary>
public sealed class Usuario : Entity
{
    public string Username { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string Role { get; private set; } = null!;

    private Usuario() { } // EF

    public Usuario(string username, string passwordHash, string role)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new DomainException("Username e obrigatorio.");
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new DomainException("Hash de senha e obrigatorio.");
        if (string.IsNullOrWhiteSpace(role)) throw new DomainException("Role e obrigatoria.");
        Username = username.Trim();
        PasswordHash = passwordHash;
        Role = role.Trim();
    }
}
