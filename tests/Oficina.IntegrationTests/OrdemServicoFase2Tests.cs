using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Application.Clientes;
using Oficina.Application.OrdensServico;
using Oficina.Application.Veiculos;
using Xunit;

namespace Oficina.IntegrationTests;

/// <summary>
/// Recursos da Fase 2: aprovacao/recusa do orcamento por webhook externo (com token de servico)
/// e listagem operacional ordenada por status com exclusao logica das OSs terminais.
/// </summary>
[Collection("api")]
public class OrdemServicoFase2Tests
{
    private readonly CustomWebApplicationFactory _factory;
    public OrdemServicoFase2Tests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Webhook_AprovarComTokenValido_DeveColocarEmExecucao()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var os = await CriarOsEmAguardandoAprovacaoAsync(client);

        var token = _factory.Services.GetRequiredService<IConfiguration>()["ExternalApproval:Token"];
        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/ordens-servico/{os.Id}/orcamento/resposta")
        {
            Content = JsonContent.Create(new RespostaOrcamentoRequest(true, "Aprovado pelo cliente"))
        };
        req.Headers.Add("X-Webhook-Token", token);

        var resp = await _factory.CreateClient().SendAsync(req); // publico (sem JWT)
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var atualizada = await resp.Content.ReadFromJsonAsync<OrdemServicoResponse>();
        atualizada!.Status.Should().Be("EmExecucao");
    }

    [Fact]
    public async Task Webhook_RecusarComTokenValido_DeveCancelar()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var os = await CriarOsEmAguardandoAprovacaoAsync(client);

        var token = _factory.Services.GetRequiredService<IConfiguration>()["ExternalApproval:Token"];
        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/ordens-servico/{os.Id}/orcamento/resposta")
        {
            Content = JsonContent.Create(new RespostaOrcamentoRequest(false, "Cliente recusou"))
        };
        req.Headers.Add("X-Webhook-Token", token);

        var resp = await _factory.CreateClient().SendAsync(req);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var atualizada = await resp.Content.ReadFromJsonAsync<OrdemServicoResponse>();
        atualizada!.Status.Should().Be("Cancelada");
    }

    [Fact]
    public async Task Webhook_TokenInvalido_DeveRetornar401()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var os = await CriarOsEmAguardandoAprovacaoAsync(client);

        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/ordens-servico/{os.Id}/orcamento/resposta")
        {
            Content = JsonContent.Create(new RespostaOrcamentoRequest(true, null))
        };
        req.Headers.Add("X-Webhook-Token", "token-errado");

        var resp = await _factory.CreateClient().SendAsync(req);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Listagem_Padrao_NaoTrazEntreguesECancelaEmOrdemDePrioridade()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        // OS entregue (terminal) -> nao deve aparecer na listagem padrao
        var entregue = await CriarFluxoAteEntregaAsync(client);

        // OS aguardando aprovacao (ativa) -> deve aparecer
        var ativa = await CriarOsEmAguardandoAprovacaoAsync(client);

        var lista = await client.GetFromJsonAsync<List<OrdemServicoResponse>>("/api/ordens-servico");
        lista!.Should().Contain(o => o.Id == ativa.Id);
        lista!.Should().NotContain(o => o.Id == entregue.Id);

        // Prioridade: nenhuma OS terminal na listagem operacional
        lista!.Should().OnlyContain(o =>
            o.Status != "Finalizada" && o.Status != "Entregue" && o.Status != "Cancelada");

        // Filtro explicito por status terminal ainda funciona
        var entregues = await client.GetFromJsonAsync<List<OrdemServicoResponse>>("/api/ordens-servico?status=Entregue");
        entregues!.Should().Contain(o => o.Id == entregue.Id);
    }

    // --- helpers ---
    private async Task<OrdemServicoResponse> CriarOsEmAguardandoAprovacaoAsync(HttpClient client)
    {
        var cliente = await Post<ClienteResponse>(client, "/api/clientes",
            new CreateClienteRequest("Cli F2", TestHelpers.GerarCpf(), null, null));
        var veiculo = await Post<VeiculoResponse>(client, "/api/veiculos",
            new CreateVeiculoRequest(cliente.Id, TestHelpers.GerarPlaca(), "VW", "Gol", 2020));
        var servicos = await client.GetFromJsonAsync<List<Oficina.Application.Servicos.ServicoResponse>>("/api/servicos");

        var os = await Post<OrdemServicoResponse>(client, "/api/ordens-servico",
            new CriarOrdemServicoRequest(cliente.Id, veiculo.Id));
        await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/iniciar-diagnostico", new { });
        await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/servicos",
            new AdicionarServicoRequest(servicos![0].Id));
        os = await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/finalizar-diagnostico", new { });
        os.Status.Should().Be("AguardandoAprovacao");
        return os;
    }

    private async Task<OrdemServicoResponse> CriarFluxoAteEntregaAsync(HttpClient client)
    {
        var os = await CriarOsEmAguardandoAprovacaoAsync(client);
        await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/aprovar", new { });
        var det = await client.GetFromJsonAsync<OrdemServicoResponse>($"/api/ordens-servico/{os.Id}");
        await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/servicos/{det!.Servicos[0].Id}/executar", new { });
        await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/finalizar-execucao", new { });
        os = await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/entregar", new { });
        os.Status.Should().Be("Entregue");
        return os;
    }

    private static async Task<T> Post<T>(HttpClient client, string url, object body)
    {
        var resp = await client.PostAsJsonAsync(url, body);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<T>())!;
    }
}
