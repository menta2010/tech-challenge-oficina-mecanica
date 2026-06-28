using FluentAssertions;
using Oficina.Domain.Clientes;
using Oficina.Domain.Estoque;
using Oficina.Domain.Servicos;
using Oficina.Domain.Shared;
using Oficina.Domain.Veiculos;
using Xunit;

namespace Oficina.UnitTests;

// Cobre metodos de atualizacao e VOs que nao eram exercitados, elevando a cobertura do dominio.
public class DominioExtraTests
{
    [Fact]
    public void Cliente_Atualizar_DeveTrocarDados()
    {
        var c = new Cliente("Joao", Documento.Criar("11144477735"), "j@e.com", "11");
        c.Atualizar("Joao Silva", "novo@e.com", "22");
        c.Nome.Should().Be("Joao Silva");
        c.Email.Should().Be("novo@e.com");
    }

    [Fact]
    public void Cliente_Atualizar_NomeVazio_DeveLancar()
    {
        var c = new Cliente("Joao", Documento.Criar("11144477735"));
        var acao = () => c.Atualizar("", null, null);
        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void Veiculo_Atualizar_DeveTrocarDados()
    {
        var v = new Veiculo(Guid.NewGuid(), Placa.Criar("ABC1234"), "VW", "Gol", 2020);
        v.Atualizar("Fiat", "Uno", 2015);
        v.Marca.Should().Be("Fiat");
        v.Ano.Should().Be(2015);
    }

    [Fact]
    public void Veiculo_AnoInvalido_DeveLancar()
    {
        var acao = () => new Veiculo(Guid.NewGuid(), Placa.Criar("ABC1234"), "VW", "Gol", 1800);
        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void Servico_Atualizar_DeveTrocarDados()
    {
        var s = new Servico("Troca", Money.From(100m), TimeSpan.FromMinutes(30), "d");
        s.Atualizar("Troca premium", Money.From(150m), TimeSpan.FromMinutes(45), "x");
        s.Nome.Should().Be("Troca premium");
        s.ValorBase.Valor.Should().Be(150m);
    }

    [Fact]
    public void Peca_Atualizar_e_Repor()
    {
        var p = new PecaInsumo("Filtro", Money.From(30m), 10);
        p.Atualizar("Filtro novo", Money.From(35m));
        p.Nome.Should().Be("Filtro novo");
        p.Repor(5);
        p.QuantidadeEmEstoque.Should().Be(15);
    }

    [Fact]
    public void Peca_Repor_QuantidadeInvalida_DeveLancar()
    {
        var p = new PecaInsumo("Filtro", Money.From(30m), 10);
        var acao = () => p.Repor(0);
        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void Documento_Cnpj_DeveSerTipoCnpj()
    {
        Documento.Criar("11222333000181").Tipo.Should().Be(TipoDocumento.CNPJ);
    }

    [Fact]
    public void Money_Zero_e_Operadores()
    {
        Money.Zero.Valor.Should().Be(0m);
        (Money.From(10m) * 3).Valor.Should().Be(30m);
    }

    [Fact]
    public void Placa_Mercosul_Valida()
    {
        Placa.Criar("bra1a23").Valor.Should().Be("BRA1A23");
    }
}
