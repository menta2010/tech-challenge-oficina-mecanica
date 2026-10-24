using Oficina.Domain.Shared;

namespace Oficina.Domain.Estoque;

/// <summary>
/// Item do catalogo de pecas/insumos, comum a todas as lojas.
/// O saldo fisico fica em EstoqueLoja, um registro por loja.
/// </summary>
public sealed class PecaInsumo : Entity
{
    public string Nome { get; private set; } = null!;
    public Money ValorUnitario { get; private set; } = null!;

    private PecaInsumo() { } // EF

    public PecaInsumo(string nome, Money valorUnitario)
        => Atualizar(nome, valorUnitario);

    public void Atualizar(string nome, Money valorUnitario)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new DomainException("Nome da peca/insumo e obrigatorio.");
        Nome = nome.Trim();
        ValorUnitario = valorUnitario ?? throw new DomainException("Valor unitario e obrigatorio.");
    }
}
