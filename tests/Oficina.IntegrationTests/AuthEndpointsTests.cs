using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Oficina.IntegrationTests;

// Cobre autenticacao JWT: login valido, login invalido (401) e protecao de rota administrativa.
[Collection("api")]
public class AuthEndpointsTests
{
    private readonly CustomWebApplicationFactory _factory;
    public AuthEndpointsTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_ComCredenciaisValidas_DeveRetornarToken()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/login", new { username = "admin", password = "admin123" });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!.Should().ContainKey("token");
    }

    [Fact]
    public async Task Login_ComSenhaErrada_DeveRetornar401()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/login", new { username = "admin", password = "errada" });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task EndpointAdministrativo_SemToken_DeveRetornar401()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/clientes");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
