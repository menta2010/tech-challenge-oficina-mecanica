using FluentAssertions;
using Microsoft.Extensions.Options;
using Oficina.Domain.Identidade;
using Oficina.Infrastructure.Identity;
using Xunit;

namespace Oficina.IntegrationTests;

// Testes unitarios puros (sem banco) da camada de Identidade da Infraestrutura.
public class InfraUnitTests
{
    [Fact]
    public void PasswordHasher_Hash_e_Verify()
    {
        var hasher = new Pbkdf2PasswordHasher();
        var hash = hasher.Hash("senha-forte-123");

        hasher.Verify(hash, "senha-forte-123").Should().BeTrue();
        hasher.Verify(hash, "errada").Should().BeFalse();
        hasher.Verify("formato-invalido", "x").Should().BeFalse();
    }

    [Fact]
    public void JwtTokenGenerator_DeveGerarTokenValido()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "oficina-api",
            Audience = "oficina-clients",
            SecretKey = "chave-de-teste-com-mais-de-32-caracteres-1234",
            ExpirationMinutes = 60
        });
        var gerador = new JwtTokenGenerator(options);
        var usuario = new Usuario("admin", "hash-fake", "Admin");

        var (token, expiraEm) = gerador.Generate(usuario);

        token.Should().NotBeNullOrWhiteSpace();
        expiraEm.Should().BeAfter(DateTime.UtcNow);
    }
}
