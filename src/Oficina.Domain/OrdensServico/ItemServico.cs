using Oficina.Domain.Shared;

namespace Oficina.Domain.OrdensServico;

/// <summary>Servico solicitado dentro de uma OS (copia valor e tempo do catalogo no momento do registro).</summary>
public sealed class ItemServico : Entity
{
    public Guid ServicoId { get; private set; }
    public string Descricao { get; private set; } = null!;
    public Money Valor { get; private set; } = null!;
    public TimeSpan TempoEstimado { get; private set; }
    public bool Executado { get; private set; }

    private ItemServico() { } // EF

    public ItemServico(Guid servicoId, string descricao, Money valor, TimeSpan tempoEstimado)
    {
        if (servicoId == Guid.Empty) throw new DomainException("Servico invalido.");
        ServicoId = servicoId;
        Descricao = descricao;
        Valor = valor;
        TempoEstimado = tempoEstimado;
    }

    internal void MarcarExecutado() => Executado = true;
}
