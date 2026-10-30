using FluentAssertions;
using Oficina.Domain.Estoque;
using Oficina.Domain.Shared;
using Xunit;

namespace Oficina.UnitTests.Estoque;

// Saldo por loja: baixa no uso da peca, reposicao e invariante de saldo nao negativo.
public class EstoqueLojaTests
{
    private static EstoqueLoja NovoEstoque(int qtd = 10) => new(Guid.NewGuid(), Guid.NewGuid(), qtd);

    [Fact]
    public void Baixar_ComEstoqueSuficiente_DeveReduzir()
    {
        var estoque = NovoEstoque(10);
        estoque.Baixar(4);
        estoque.Quantidade.Should().Be(6);
    }

    [Fact]
    public void Baixar_AlemDoEstoque_DeveLancar()
    {
        var estoque = NovoEstoque(2);
        var acao = () => estoque.Baixar(5);
        acao.Should().Throw<DomainException>().WithMessage("*insuficiente*");
    }

    [Fact]
    public void Repor_DeveSomar()
    {
        var estoque = NovoEstoque(10);
        estoque.Repor(5);
        estoque.Quantidade.Should().Be(15);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Repor_QuantidadeInvalida_DeveLancar(int quantidade)
    {
        var acao = () => NovoEstoque().Repor(quantidade);
        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void Criar_QuantidadeNegativa_DeveLancar()
    {
        var acao = () => new EstoqueLoja(Guid.NewGuid(), Guid.NewGuid(), -1);
        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void Criar_SemLoja_DeveLancar()
    {
        var acao = () => new EstoqueLoja(Guid.Empty, Guid.NewGuid());
        acao.Should().Throw<DomainException>();
    }
}
