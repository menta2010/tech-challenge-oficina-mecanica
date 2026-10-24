using Oficina.Domain.Shared;

namespace Oficina.Domain.Lojas;

/// <summary>
/// Unidade da rede de oficinas. Ordens de servico, funcionarios e estoque pertencem a uma loja.
/// O cliente nao pertence a uma loja: ele pode ser atendido em qualquer unidade (ver ADR-004).
/// </summary>
public sealed class Loja : Entity
{
    public string Nome { get; private set; } = null!;
    public string Cidade { get; private set; } = null!;
    public bool Ativa { get; private set; }

    private Loja() { } // EF

    public Loja(string nome, string cidade)
    {
        Validar(nome, cidade);
        Nome = nome.Trim();
        Cidade = cidade.Trim();
        Ativa = true;
    }

    public void Atualizar(string nome, string cidade)
    {
        Validar(nome, cidade);
        Nome = nome.Trim();
        Cidade = cidade.Trim();
    }

    public void Desativar() => Ativa = false;
    public void Ativar() => Ativa = true;

    private static void Validar(string nome, string cidade)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new DomainException("Nome da loja e obrigatorio.");
        if (string.IsNullOrWhiteSpace(cidade)) throw new DomainException("Cidade da loja e obrigatoria.");
    }
}
