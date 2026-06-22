namespace Oficina.Application.Abstractions;

/// <summary>Hash e verificacao de senha (implementado com PBKDF2 na Infrastructure).</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string storedHash, string password);
}
