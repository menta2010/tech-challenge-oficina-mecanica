namespace Oficina.Domain.Shared;

/// <summary>
/// Value Object monetario (BRL). Garante valor nao-negativo e 2 casas decimais.
/// Atende as regras de valores de servicos, pecas e orcamento.
/// </summary>
public sealed record Money
{
    public decimal Valor { get; }

    private Money(decimal valor) => Valor = valor;

    public static Money Zero => new(0m);

    public static Money From(decimal valor)
    {
        if (valor < 0)
            throw new DomainException("Valor monetario nao pode ser negativo.");
        return new Money(decimal.Round(valor, 2, MidpointRounding.AwayFromZero));
    }

    public static Money operator +(Money a, Money b) => new(a.Valor + b.Valor);
    public static Money operator *(Money a, int quantidade) => new(a.Valor * quantidade);

    public override string ToString() => Valor.ToString("0.00");
}
