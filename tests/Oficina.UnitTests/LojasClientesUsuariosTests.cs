using FluentAssertions;
using Oficina.Domain.Clientes;
using Oficina.Domain.Identidade;
using Oficina.Domain.Lojas;
using Oficina.Domain.Shared;
using Xunit;

namespace Oficina.UnitTests;

// Novas regras da Fase 3: lojas, status do cliente e perfis de funcionario.
public class LojasClientesUsuariosTests
{
    [Fact]
    public void Loja_NasceAtiva_EPodeSerDesativada()
    {
        var loja = new Loja("Unidade Centro", "Sao Paulo");
        loja.Ativa.Should().BeTrue();

        loja.Desativar();

        loja.Ativa.Should().BeFalse();
    }

    [Fact]
    public void Loja_SemNome_DeveLancar()
    {
        var acao = () => new Loja(" ", "Sao Paulo");
        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cliente_NasceAtivo()
    {
        var cliente = new Cliente("Maria", Documento.Criar("52998224725"));

        cliente.Ativo.Should().BeTrue();
        cliente.InativadoEm.Should().BeNull();
    }

    [Fact]
    public void Cliente_InativarEReativar()
    {
        var cliente = new Cliente("Maria", Documento.Criar("52998224725"));

        cliente.Inativar();
        cliente.Ativo.Should().BeFalse();
        cliente.InativadoEm.Should().NotBeNull();

        cliente.Reativar();
        cliente.Ativo.Should().BeTrue();
        cliente.InativadoEm.Should().BeNull();
    }

    [Fact]
    public void Cliente_InativarDuasVezes_MantemDataOriginal()
    {
        var cliente = new Cliente("Maria", Documento.Criar("52998224725"));
        cliente.Inativar();
        var data = cliente.InativadoEm;

        cliente.Inativar();

        cliente.InativadoEm.Should().Be(data);
    }

    [Fact]
    public void Atendente_SemLoja_DeveLancar()
    {
        var acao = () => new Usuario("joao", "hash", Perfis.Atendente);
        acao.Should().Throw<DomainException>().WithMessage("*loja*");
    }

    [Fact]
    public void Admin_NaoFicaPresoAUmaLoja()
    {
        var admin = new Usuario("admin", "hash", Perfis.Admin, Guid.NewGuid());
        admin.LojaId.Should().BeNull();
    }

    [Fact]
    public void Usuario_ComPerfilCliente_DeveLancar()
    {
        // Cliente nao e usuario: ele se autentica pelo CPF na Lambda
        var acao = () => new Usuario("maria", "hash", Perfis.Cliente);
        acao.Should().Throw<DomainException>();
    }
}
