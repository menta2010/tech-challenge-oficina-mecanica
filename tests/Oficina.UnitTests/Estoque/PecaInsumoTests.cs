using FluentAssertions;
using Oficina.Domain.Estoque;
using Oficina.Domain.Shared;
using Xunit;

namespace Oficina.UnitTests.Estoque;

// Cobre o controle de estoque e a baixa (requisito de baixa quando peca for utilizada).
public class PecaInsumoTests
{
    private static PecaInsumo NovaPeca(int qtd = 10)
        => new("Filtro de oleo", Money.From(30m), qtd);

    [Fact]
    public void Baixar_ComEstoqueSuficiente_DeveReduzir()
    {
        var peca = NovaPeca(10);
        peca.Baixar(4);
        peca.QuantidadeEmEstoque.Should().Be(6);
    }

    [Fact]
    public void Baixar_AlemDoEstoque_DeveLancar()
    {
        var peca = NovaPeca(2);
        var acao = () => peca.Baixar(5);
        acao.Should().Throw<DomainException>().WithMessage("*insuficiente*");
    }

    [Fact]
    public void Criar_QuantidadeNegativa_DeveLancar()
    {
        var acao = () => new PecaInsumo("X", Money.From(1m), -1);
        acao.Should().Throw<DomainException>();
    }
}
