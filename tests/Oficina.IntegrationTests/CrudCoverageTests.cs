using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Oficina.Application.Clientes;
using Oficina.Application.Estoque;
using Oficina.Application.OrdensServico;
using Oficina.Application.Servicos;
using Oficina.Application.Veiculos;
using Xunit;

namespace Oficina.IntegrationTests;

// Cobre os caminhos de CRUD (obter/listar/atualizar/remover) e fluxos alternativos da OS,
// elevando a cobertura dos services/controllers/repositorios.
[Collection("api")]
public class CrudCoverageTests
{
    private readonly CustomWebApplicationFactory _factory;
    public CrudCoverageTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Cliente_e_Veiculo_Crud_Completo()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var cli = await Post<ClienteResponse>(client, "/api/clientes",
            new CreateClienteRequest("Ana", TestHelpers.GerarCpf(), "ana@ex.com", "1199"));
        (await client.GetAsync($"/api/clientes/{cli.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/api/clientes")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PutAsJsonAsync($"/api/clientes/{cli.Id}",
            new UpdateClienteRequest("Ana Maria", "ana2@ex.com", "2200"))).StatusCode.Should().Be(HttpStatusCode.OK);

        var vei = await Post<VeiculoResponse>(client, "/api/veiculos",
            new CreateVeiculoRequest(cli.Id, TestHelpers.GerarPlaca(), "VW", "Gol", 2020));
        (await client.GetAsync($"/api/veiculos/{vei.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync($"/api/veiculos?clienteId={cli.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PutAsJsonAsync($"/api/veiculos/{vei.Id}",
            new UpdateVeiculoRequest("Fiat", "Uno", 2018))).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.DeleteAsync($"/api/veiculos/{vei.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await client.DeleteAsync($"/api/clientes/{cli.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetAsync($"/api/clientes/{cli.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Servico_e_Peca_Crud_Completo()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var s = await Post<ServicoResponse>(client, "/api/servicos",
            new CreateServicoRequest("Lavagem", 50m, 30, "simples"));
        (await client.GetAsync($"/api/servicos/{s.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PutAsJsonAsync($"/api/servicos/{s.Id}",
            new UpdateServicoRequest("Lavagem completa", 60m, 40, "detalhada"))).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.DeleteAsync($"/api/servicos/{s.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var p = await Post<PecaInsumoResponse>(client, "/api/pecas-insumos",
            new CreatePecaInsumoRequest("Vela", 20m, 5));
        (await client.GetAsync($"/api/pecas-insumos/{p.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PutAsJsonAsync($"/api/pecas-insumos/{p.Id}",
            new UpdatePecaInsumoRequest("Vela NGK", 22m))).StatusCode.Should().Be(HttpStatusCode.OK);
        var reab = await client.PostAsJsonAsync($"/api/pecas-insumos/{p.Id}/reabastecer",
            new ReabastecerEstoqueRequest(10));
        reab.StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.DeleteAsync($"/api/pecas-insumos/{p.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task OS_RemoverItens_Cancelar_e_Listar()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var cli = await Post<ClienteResponse>(client, "/api/clientes",
            new CreateClienteRequest("Cli", TestHelpers.GerarCpf(), null, null));
        var vei = await Post<VeiculoResponse>(client, "/api/veiculos",
            new CreateVeiculoRequest(cli.Id, TestHelpers.GerarPlaca(), "VW", "Gol", 2020));
        var servicos = await client.GetFromJsonAsync<List<ServicoResponse>>("/api/servicos");
        var pecas = await client.GetFromJsonAsync<List<PecaInsumoResponse>>("/api/pecas-insumos");

        var os = await Post<OrdemServicoResponse>(client, "/api/ordens-servico",
            new CriarOrdemServicoRequest(cli.Id, vei.Id));
        await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/iniciar-diagnostico", new { });
        await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/servicos",
            new AdicionarServicoRequest(servicos![0].Id));
        os = await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/servicos",
            new AdicionarServicoRequest(servicos![0].Id));
        os = await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/pecas",
            new AdicionarPecaRequest(pecas![0].Id, 1));

        os = await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/finalizar-diagnostico", new { });
        os.Status.Should().Be("AguardandoAprovacao");

        // remove um servico (sobra 1) e a peca
        var itemServ = os.Servicos[^1].Id;
        var itemPeca = os.Pecas[0].Id;
        (await client.DeleteAsync($"/api/ordens-servico/{os.Id}/servicos/{itemServ}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.DeleteAsync($"/api/ordens-servico/{os.Id}/pecas/{itemPeca}")).StatusCode.Should().Be(HttpStatusCode.OK);

        // cancela
        os = await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/cancelar", new { });
        os.Status.Should().Be("Cancelada");

        // listagens e detalhe
        (await client.GetAsync("/api/ordens-servico")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/api/ordens-servico?status=Cancelada")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync($"/api/ordens-servico/{os.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<T> Post<T>(HttpClient client, string url, object body)
    {
        var resp = await client.PostAsJsonAsync(url, body);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<T>())!;
    }
}
