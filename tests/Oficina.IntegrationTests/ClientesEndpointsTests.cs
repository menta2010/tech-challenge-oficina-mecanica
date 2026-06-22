using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Oficina.Application.Clientes;
using Xunit;

// Cobre o CRUD de clientes via API: criacao valida, CPF invalido (400) e documento duplicado (409).
namespace Oficina.IntegrationTests;

[Collection("api")]
public class ClientesEndpointsTests
{
    private readonly CustomWebApplicationFactory _factory;
    public ClientesEndpointsTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Criar_ClienteValido_DeveRetornar201()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var req = new CreateClienteRequest("Maria Souza", TestHelpers.GerarCpf(), "maria@ex.com", "11999999999");
        var resp = await client.PostAsJsonAsync("/api/clientes", req);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await resp.Content.ReadFromJsonAsync<ClienteResponse>();
        body!.Nome.Should().Be("Maria Souza");
        body.TipoDocumento.Should().Be("CPF");
    }

    [Fact]
    public async Task Criar_CpfInvalido_DeveRetornar400()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var req = new CreateClienteRequest("Fulano", "12345678900", null, null);
        var resp = await client.PostAsJsonAsync("/api/clientes", req);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Criar_DocumentoDuplicado_DeveRetornar409()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var cpf = TestHelpers.GerarCpf();
        var req = new CreateClienteRequest("Cliente A", cpf, null, null);

        (await client.PostAsJsonAsync("/api/clientes", req)).StatusCode.Should().Be(HttpStatusCode.Created);
        var dup = await client.PostAsJsonAsync("/api/clientes", req with { Nome = "Cliente B" });
        dup.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
