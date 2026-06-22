using Oficina.Domain.Shared;

namespace Oficina.Domain.OrdensServico;

/// <summary>
/// Orcamento gerado automaticamente ao finalizar o diagnostico, com previsao de entrega.
/// Atende ao requisito "orcamento gerado automaticamente com base nos servicos e pecas".
/// </summary>
public sealed class Orcamento
{
    public Money ValorServicos { get; private set; } = null!;
    public Money ValorPecas { get; private set; } = null!;
    public Money ValorTotal { get; private set; } = null!;
    public DateTime PrevisaoEntrega { get; private set; }
    public DateTime GeradoEm { get; private set; }

    private Orcamento() { } // EF

    public Orcamento(Money valorServicos, Money valorPecas, DateTime previsaoEntrega)
    {
        ValorServicos = valorServicos;
        ValorPecas = valorPecas;
        ValorTotal = valorServicos + valorPecas;
        PrevisaoEntrega = previsaoEntrega;
        GeradoEm = DateTime.UtcNow;
    }
}
