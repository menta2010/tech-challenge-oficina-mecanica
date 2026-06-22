using Oficina.Domain.Shared;

namespace Oficina.Domain.Clientes;

/// <summary>Raiz de agregado Cliente. Atende ao CRUD de clientes e identificacao por documento.</summary>
public sealed class Cliente : Entity
{
    public string Nome { get; private set; } = null!;
    public Documento Documento { get; private set; } = null!;
    public string? Email { get; private set; }
    public string? Telefone { get; private set; }

    private Cliente() { } // EF

    public Cliente(string nome, Documento documento, string? email = null, string? telefone = null)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("Nome do cliente e obrigatorio.");
        Nome = nome.Trim();
        Documento = documento ?? throw new DomainException("Documento e obrigatorio.");
        Email = email;
        Telefone = telefone;
    }

    public void Atualizar(string nome, string? email, string? telefone)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("Nome do cliente e obrigatorio.");
        Nome = nome.Trim();
        Email = email;
        Telefone = telefone;
    }
}
