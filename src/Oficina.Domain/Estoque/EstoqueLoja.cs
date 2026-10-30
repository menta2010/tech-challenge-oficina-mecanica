using Oficina.Domain.Shared;

namespace Oficina.Domain.Estoque;

/// <summary>
/// Saldo de uma peca/insumo em uma loja. O catalogo (nome, preco) e global em PecaInsumo;
/// a quantidade fisica e por unidade (ver ADR-003). Invariante: saldo nunca negativo.
/// </summary>
public sealed class EstoqueLoja : Entity
{
    public Guid LojaId { get; private set; }
    public Guid PecaInsumoId { get; private set; }
    public int Quantidade { get; private set; }

    private EstoqueLoja() { } // EF

    public EstoqueLoja(Guid lojaId, Guid pecaInsumoId, int quantidadeInicial = 0)
    {
        if (lojaId == Guid.Empty) throw new DomainException("Loja e obrigatoria.");
        if (pecaInsumoId == Guid.Empty) throw new DomainException("Peca/insumo e obrigatorio.");
        if (quantidadeInicial < 0) throw new DomainException("Quantidade em estoque nao pode ser negativa.");
        LojaId = lojaId;
        PecaInsumoId = pecaInsumoId;
        Quantidade = quantidadeInicial;
    }

    /// <summary>Baixa ao usar a peca na execucao da OS.</summary>
    public void Baixar(int quantidade)
    {
        if (quantidade <= 0) throw new DomainException("Quantidade de baixa deve ser positiva.");
        if (quantidade > Quantidade)
            throw new DomainException($"Estoque insuficiente. Disponivel: {Quantidade}, solicitado: {quantidade}.");
        Quantidade -= quantidade;
    }

    public void Repor(int quantidade)
    {
        if (quantidade <= 0) throw new DomainException("Quantidade de reposicao deve ser positiva.");
        Quantidade += quantidade;
    }
}
