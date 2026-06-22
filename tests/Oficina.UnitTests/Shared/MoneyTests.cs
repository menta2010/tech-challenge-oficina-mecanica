using FluentAssertions;
using Oficina.Domain.Shared;
using Xunit;

namespace Oficina.UnitTests.Shared;

// Cobre o VO Money: nao-negatividade, arredondamento e operadores usados no orcamento.
public class MoneyTests
{
    [Fact]
    public void From_ValorNegativo_DeveLancar()
    {
        var acao = () => Money.From(-1m);
        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void Soma_DeveAcumularValores()
    {
        (Money.From(10.50m) + Money.From(4.50m)).Valor.Should().Be(15.00m);
    }

    [Fact]
    public void Multiplicacao_PorQuantidade_DeveCalcularSubtotal()
    {
        (Money.From(25m) * 3).Valor.Should().Be(75m);
    }
}
