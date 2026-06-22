using FluentAssertions;
using Xunit;

namespace Oficina.UnitTests;

/// <summary>
/// Smoke test da fundacao: garante que o projeto de testes esta ligado
/// corretamente as camadas Domain/Application e que o runner executa.
/// Sera substituido por testes de regras de dominio no item 2 do backlog.
/// </summary>
public class SmokeTests
{
    [Fact]
    public void Fundacao_DeveExecutarTestes()
    {
        var resultado = 1 + 1;
        resultado.Should().Be(2);
    }
}
