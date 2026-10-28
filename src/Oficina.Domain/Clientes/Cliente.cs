using Oficina.Domain.Shared;

namespace Oficina.Domain.Clientes;

/// <summary>
/// Raiz de agregado Cliente. Identificado por documento (CPF/CNPJ).
/// O status (Ativo) controla se o cliente pode se autenticar pelo CPF.
/// </summary>
public sealed class Cliente : Entity
{
    public string Nome { get; private set; } = null!;
    public Documento Documento { get; private set; } = null!;
    public string? Email { get; private set; }
    public string? Telefone { get; private set; }
    public bool Ativo { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime? InativadoEm { get; private set; }

    private Cliente() { } // EF

    public Cliente(string nome, Documento documento, string? email = null, string? telefone = null)
    {
        Documento = documento ?? throw new DomainException("Documento e obrigatorio.");
        Atualizar(nome, email, telefone);
        Ativo = true;
        CriadoEm = DateTime.UtcNow;
    }

    public void Atualizar(string nome, string? email, string? telefone)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("Nome do cliente e obrigatorio.");
        Nome = nome.Trim();
        Email = email;
        Telefone = telefone;
    }

    /// <summary>Inativa o cliente (manual ou pela politica de inatividade). Idempotente.</summary>
    public void Inativar()
    {
        if (!Ativo) return;
        Ativo = false;
        InativadoEm = DateTime.UtcNow;
    }

    public void Reativar()
    {
        Ativo = true;
        InativadoEm = null;
    }
}
