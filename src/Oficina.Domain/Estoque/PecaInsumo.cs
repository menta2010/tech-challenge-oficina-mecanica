using Oficina.Domain.Shared;

namespace Oficina.Domain.Estoque;

/// <summary>
/// Raiz de agregado Peca/Insumo com controle de estoque.
/// Atende ao CRUD de pecas e a baixa de estoque na execucao da OS.
/// </summary>
public sealed class PecaInsumo : Entity
{
    public string Nome { get; private set; } = null!;
    public Money ValorUnitario { get; private set; } = null!;
    public int QuantidadeEmEstoque { get; private set; }

    private PecaInsumo() { } // EF

    public PecaInsumo(string nome, Money valorUnitario, int quantidadeEmEstoque)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new DomainException("Nome da peca/insumo e obrigatorio.");
        if (quantidadeEmEstoque < 0) throw new DomainException("Quantidade em estoque nao pode ser negativa.");
        Nome = nome.Trim();
        ValorUnitario = valorUnitario ?? throw new DomainException("Valor unitario e obrigatorio.");
        QuantidadeEmEstoque = quantidadeEmEstoque;
    }

    /// <summary>Baixa de estoque ao usar a peca na execucao. Invariante: nunca negativa.</summary>
    public void Baixar(int quantidade)
    {
        if (quantidade <= 0) throw new DomainException("Quantidade de baixa deve ser positiva.");
        if (quantidade > QuantidadeEmEstoque)
            throw new DomainException($"Estoque insuficiente de '{Nome}'. Disponivel: {QuantidadeEmEstoque}, solicitado: {quantidade}.");
        QuantidadeEmEstoque -= quantidade;
    }

    public void Repor(int quantidade)
    {
        if (quantidade <= 0) throw new DomainException("Quantidade de reposicao deve ser positiva.");
        QuantidadeEmEstoque += quantidade;
    }
    public void Atualizar(string nome, Money valorUnitario)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new DomainException("Nome da peca/insumo e obrigatorio.");
        Nome = nome.Trim();
        ValorUnitario = valorUnitario ?? throw new DomainException("Valor unitario e obrigatorio.");
    }
}
