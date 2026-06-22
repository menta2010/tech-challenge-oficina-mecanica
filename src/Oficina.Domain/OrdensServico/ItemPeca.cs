using Oficina.Domain.Shared;

namespace Oficina.Domain.OrdensServico;

/// <summary>Peca/insumo previsto numa OS. A baixa real de estoque e orquestrada pela camada de aplicacao.</summary>
public sealed class ItemPeca : Entity
{
    public Guid PecaId { get; private set; }
    public int Quantidade { get; private set; }
    public Money ValorUnitario { get; private set; } = null!;
    public bool Utilizado { get; private set; }

    public Money Subtotal => ValorUnitario * Quantidade;

    private ItemPeca() { } // EF

    public ItemPeca(Guid pecaId, int quantidade, Money valorUnitario)
    {
        if (pecaId == Guid.Empty) throw new DomainException("Peca invalida.");
        if (quantidade <= 0) throw new DomainException("Quantidade da peca deve ser positiva.");
        PecaId = pecaId;
        Quantidade = quantidade;
        ValorUnitario = valorUnitario;
    }

    internal void MarcarUtilizado() => Utilizado = true;
}
