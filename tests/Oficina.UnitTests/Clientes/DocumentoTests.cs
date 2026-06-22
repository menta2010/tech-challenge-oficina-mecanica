using FluentAssertions;
using Oficina.Domain.Clientes;
using Oficina.Domain.Shared;
using Xunit;

namespace Oficina.UnitTests.Clientes;

// Cobre a validacao de CPF/CNPJ (requisito de validacao de dados sensiveis).
public class DocumentoTests
{
    [Theory]
    [InlineData("111.444.777-35")]
    [InlineData("11144477735")]
    [InlineData("123.456.789-09")]
    public void Criar_CpfValido_DeveAceitar(string entrada)
    {
        var doc = Documento.Criar(entrada);
        doc.Tipo.Should().Be(TipoDocumento.CPF);
        doc.Numero.Should().MatchRegex("^[0-9]{11}$");
    }

    [Theory]
    [InlineData("11.222.333/0001-81")]
    [InlineData("11444777000161")]
    public void Criar_CnpjValido_DeveAceitar(string entrada)
    {
        var doc = Documento.Criar(entrada);
        doc.Tipo.Should().Be(TipoDocumento.CNPJ);
        doc.Numero.Should().MatchRegex("^[0-9]{14}$");
    }

    [Theory]
    [InlineData("12345678900")]   // digito verificador errado
    [InlineData("11111111111")]   // todos iguais
    [InlineData("123")]           // tamanho invalido
    [InlineData("")]
    public void Criar_DocumentoInvalido_DeveLancar(string entrada)
    {
        var acao = () => Documento.Criar(entrada);
        acao.Should().Throw<DomainException>();
    }
}
