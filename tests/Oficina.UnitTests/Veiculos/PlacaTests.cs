using FluentAssertions;
using Oficina.Domain.Shared;
using Oficina.Domain.Veiculos;
using Xunit;

namespace Oficina.UnitTests.Veiculos;

// Cobre a validacao de placa (antiga e Mercosul).
public class PlacaTests
{
    [Theory]
    [InlineData("ABC1234", "ABC1234")]
    [InlineData("abc-1234", "ABC1234")]
    [InlineData("BRA1A23", "BRA1A23")]
    public void Criar_PlacaValida_DeveNormalizar(string entrada, string esperado)
    {
        Placa.Criar(entrada).Valor.Should().Be(esperado);
    }

    [Theory]
    [InlineData("AB1234")]
    [InlineData("ABCD123")]
    [InlineData("1234ABC")]
    [InlineData("")]
    public void Criar_PlacaInvalida_DeveLancar(string entrada)
    {
        var acao = () => Placa.Criar(entrada);
        acao.Should().Throw<DomainException>();
    }
}
