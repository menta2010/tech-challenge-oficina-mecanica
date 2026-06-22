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

/// <summary>
/// Fluxo ponta-a-ponta da OS: criar cliente/veiculo, criar OS, diagnostico, orcamento,
/// aprovacao, execucao com baixa de estoque, finalizacao, entrega, e consulta publica.
/// Cobre o nucleo do dominio integrado a API e ao banco.
/// </summary>
[Collection("api")]
public class OrdemServicoFlowTests
{
    private readonly CustomWebApplicationFactory _factory;
    public OrdemServicoFlowTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task FluxoCompleto_DaCriacaoAEntrega_DeveFuncionarEBaixarEstoque()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        // Cliente + Veiculo
        var cliente = await Post<ClienteResponse>(client, "/api/clientes",
            new CreateClienteRequest("Joao Cliente", TestHelpers.GerarCpf(), null, null));
        var veiculo = await Post<VeiculoResponse>(client, "/api/veiculos",
            new CreateVeiculoRequest(cliente.Id, TestHelpers.GerarPlaca(), "VW", "Gol", 2020));

        // Catalogo (seed): pega um servico e uma peca com estoque
        var servicos = await client.GetFromJsonAsync<List<ServicoResponse>>("/api/servicos");
        var pecas = await client.GetFromJsonAsync<List<PecaInsumoResponse>>("/api/pecas-insumos");
        var servico = servicos!.First();
        var peca = pecas!.First(p => p.QuantidadeEmEstoque > 0);
        var estoqueInicial = peca.QuantidadeEmEstoque;

        // Criar OS
        var os = await Post<OrdemServicoResponse>(client, "/api/ordens-servico",
            new CriarOrdemServicoRequest(cliente.Id, veiculo.Id));
        os.Status.Should().Be("Recebida");

        // Diagnostico
        os = await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/iniciar-diagnostico", new { });
        os.Status.Should().Be("EmDiagnostico");

        os = await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/servicos",
            new AdicionarServicoRequest(servico.Id));
        os = await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/pecas",
            new AdicionarPecaRequest(peca.Id, 1));
        os.Servicos.Should().HaveCount(1);
        os.Pecas.Should().HaveCount(1);

        // Finalizar diagnostico -> orcamento gerado
        os = await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/finalizar-diagnostico", new { });
        os.Status.Should().Be("AguardandoAprovacao");
        os.Orcamento.Should().NotBeNull();
        os.Orcamento!.ValorTotal.Should().BeGreaterThan(0);

        // Aprovar -> execucao
        os = await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/aprovar", new { });
        os.Status.Should().Be("EmExecucao");

        // Executar servico e usar peca (baixa estoque)
        var itemServicoId = os.Servicos.First().Id;
        var itemPecaId = os.Pecas.First().Id;
        await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/servicos/{itemServicoId}/executar", new { });
        os = await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/pecas/{itemPecaId}/usar", new { });
        os.Pecas.First().Utilizado.Should().BeTrue();

        // Estoque deve ter sido baixado em 1
        var pecaApos = await client.GetFromJsonAsync<PecaInsumoResponse>($"/api/pecas-insumos/{peca.Id}");
        pecaApos!.QuantidadeEmEstoque.Should().Be(estoqueInicial - 1);

        // Finalizar e entregar
        os = await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/finalizar-execucao", new { });
        os.Status.Should().Be("Finalizada");
        os = await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/entregar", new { });
        os.Status.Should().Be("Entregue");

        // Consulta publica (sem autenticacao)
        var publico = _factory.CreateClient();
        var acomp = await publico.GetAsync($"/api/acompanhamento/{os.Id}");
        acomp.StatusCode.Should().Be(HttpStatusCode.OK);
        var andamento = await acomp.Content.ReadFromJsonAsync<AcompanhamentoResponse>();
        andamento!.Status.Should().Be("Entregue");

        // Relatorio de tempo medio
        var rel = await client.GetAsync("/api/ordens-servico/relatorios/tempo-medio");
        rel.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UsarPeca_SemEstoque_DeveRetornar400()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var cliente = await Post<ClienteResponse>(client, "/api/clientes",
            new CreateClienteRequest("Sem Estoque", TestHelpers.GerarCpf(), null, null));
        var veiculo = await Post<VeiculoResponse>(client, "/api/veiculos",
            new CreateVeiculoRequest(cliente.Id, TestHelpers.GerarPlaca(), "Fiat", "Uno", 2015));

        // Peca propria com estoque 1
        var peca = await Post<PecaInsumoResponse>(client, "/api/pecas-insumos",
            new CreatePecaInsumoRequest("Peca rara", 10m, 1));
        var servicos = await client.GetFromJsonAsync<List<ServicoResponse>>("/api/servicos");

        var os = await Post<OrdemServicoResponse>(client, "/api/ordens-servico",
            new CriarOrdemServicoRequest(cliente.Id, veiculo.Id));
        await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/iniciar-diagnostico", new { });
        await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/servicos",
            new AdicionarServicoRequest(servicos!.First().Id));
        // pede 2 de uma peca que so tem 1
        os = await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/pecas",
            new AdicionarPecaRequest(peca.Id, 2));
        await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/finalizar-diagnostico", new { });
        await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/aprovar", new { });

        var itemPecaId = os.Pecas.First().Id;
        var resp = await client.PostAsJsonAsync($"/api/ordens-servico/{os.Id}/pecas/{itemPecaId}/usar", new { });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest); // estoque insuficiente
    }

    private static async Task<T> Post<T>(HttpClient client, string url, object body)
    {
        var resp = await client.PostAsJsonAsync(url, body);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<T>())!;
    }
}
