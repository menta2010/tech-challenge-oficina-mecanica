using Oficina.Domain.Shared;

namespace Oficina.Domain.Identidade;

/// <summary>Perfis de acesso emitidos no JWT (claim role).</summary>
public static class Perfis
{
    /// <summary>Gestao da rede: lojas, catalogo, relatorios, todas as OS.</summary>
    public const string Admin = "Admin";

    /// <summary>Funcionario de uma loja: opera as OS e o estoque da propria loja.</summary>
    public const string Atendente = "Atendente";

    /// <summary>Cliente autenticado pelo CPF (Lambda): abre e acompanha as proprias OS.</summary>
    public const string Cliente = "Cliente";

    public const string Funcionarios = Admin + "," + Atendente;
}

/// <summary>
/// Funcionario com acesso as APIs. A senha e armazenada apenas como hash (PBKDF2).
/// Atendentes pertencem a uma loja; administradores nao (LojaId nulo).
/// </summary>
public sealed class Usuario : Entity
{
    public string Username { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string Role { get; private set; } = null!;
    public Guid? LojaId { get; private set; }

    private Usuario() { } // EF

    public Usuario(string username, string passwordHash, string role, Guid? lojaId = null)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new DomainException("Username e obrigatorio.");
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new DomainException("Hash de senha e obrigatorio.");
        if (role is not (Perfis.Admin or Perfis.Atendente))
            throw new DomainException($"Perfil de funcionario invalido: '{role}'.");
        if (role == Perfis.Atendente && (lojaId is null || lojaId == Guid.Empty))
            throw new DomainException("Atendente precisa estar vinculado a uma loja.");

        Username = username.Trim();
        PasswordHash = passwordHash;
        Role = role;
        LojaId = role == Perfis.Admin ? null : lojaId;
    }
}
