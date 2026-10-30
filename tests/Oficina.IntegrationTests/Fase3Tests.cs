using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Oficina.Application.Clientes;
using Oficina.Application.Lojas;
using Oficina.Application.OrdensServico;
using Oficina.Application.Servicos;
using Oficina.Application.Veiculos;
using Xunit;

namespace Oficina.IntegrationTests;

/// <summary>
/// Recursos da Fase 3: multiplas lojas e escopo por perfil (atendente x loja, cliente x OS),
/// exclusao logica explicita na listagem, inativacao de cliente, relatorio de tempo por status
/// e respostas de erro padronizadas (Problem Details + correlationId).
/// </summary>
[Collection("api")]
public class Fase3Tests
{
    private readonly CustomWebApplicationFactory _factory;
    public Fase3Tests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Listagem_ExclusaoLogica_IncluirEncerradasTrazAsEncerradasAoFinal()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync();
        var (cliente, _) = await CriarClienteComVeiculoAsync(admin);
        var entregue = await CriarOsAsync(admin, cliente, ateEntrega: true);
        var ativa = await CriarOsAsync(admin, cliente);

        var padrao = await admin.GetFromJsonAsync<List<OrdemServicoResponse>>("/api/ordens-servico");
        padrao!.Should().Contain(o => o.Id == ativa.Id);
        padrao.Should().NotContain(o => o.Id == entregue.Id);
        padrao.Should().OnlyContain(o => !o.Encerrada);

        var completa = await admin.GetFromJsonAsync<List<OrdemServicoResponse>>("/api/ordens-servico?incluirEncerradas=true");
        completa!.Should().Contain(o => o.Id == entregue.Id);
        completa.SkipWhile(o => !o.Encerrada).Should().OnlyContain(o => o.Encerrada,
            "as encerradas vem depois de todas as ativas");
    }

    [Fact]
    public async Task Listagem_OrdenaPorPrioridadeDeStatusEDepoisPelaMaisAntiga()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync();
        var lista = await admin.GetFromJsonAsync<List<OrdemServicoResponse>>("/api/ordens-servico");

        var prioridade = new Dictionary<string, int>
            { ["EmExecucao"] = 0, ["AguardandoAprovacao"] = 1, ["EmDiagnostico"] = 2, ["Recebida"] = 3 };
        lista!.Select(o => prioridade[o.Status]).Should().BeInAscendingOrder();
        foreach (var grupo in lista.GroupBy(o => o.Status))
            grupo.Select(o => o.CriadaEm).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Atendente_SoEnxergaEOperaAPropriaLoja()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync();
        var atendente = await _factory.CreateAtendenteCentroClientAsync();
        var (cliente, veiculo) = await CriarClienteComVeiculoAsync(admin);

        var zonaSul = await admin.ObterLojaAsync(TestHelpers.LojaZonaSul);
        var osZonaSul = await Post<OrdemServicoResponse>(admin, "/api/ordens-servico",
            new CriarOrdemServicoRequest(zonaSul, cliente.Id, veiculo.Id));

        (await atendente.GetAsync($"/api/ordens-servico/{osZonaSul.Id}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await atendente.PostAsJsonAsync($"/api/ordens-servico/{osZonaSul.Id}/iniciar-diagnostico", new { }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var centro = await atendente.ObterLojaCentroAsync();
        var lista = await atendente.GetFromJsonAsync<List<OrdemServicoResponse>>("/api/ordens-servico");
        lista!.Should().OnlyContain(o => o.LojaId == centro);

        // Sem informar a loja, a OS do atendente nasce na loja dele
        var osCentro = await Post<OrdemServicoResponse>(atendente, "/api/ordens-servico",
            new CriarOrdemServicoRequest(null, cliente.Id, veiculo.Id));
        osCentro.LojaId.Should().Be(centro);

        // Estoque de outra loja e cadastro de loja: negados
        (await atendente.GetAsync($"/api/lojas/{zonaSul}/estoque")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await atendente.PostAsJsonAsync("/api/lojas", new CriarLojaRequest("Loja X", "Campinas")))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Cliente_AutenticadoPorCpf_AbreEConsultaSomenteAsProprias()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync();
        var (cliente, veiculo) = await CriarClienteComVeiculoAsync(admin);
        var (outro, _) = await CriarClienteComVeiculoAsync(admin);
        var osDoOutro = await CriarOsAsync(admin, outro);

        var token = _factory.CreateClienteClient(cliente.Id);
        var minha = await Post<OrdemServicoResponse>(token, "/api/ordens-servico",
            new CriarOrdemServicoRequest(await admin.ObterLojaCentroAsync(), null, veiculo.Id));
        minha.ClienteId.Should().Be(cliente.Id);

        var lista = await token.GetFromJsonAsync<List<OrdemServicoResponse>>("/api/acompanhamento");
        lista!.Should().OnlyContain(o => o.ClienteId == cliente.Id);

        (await token.GetAsync($"/api/acompanhamento/{osDoOutro.Id}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        // Cliente nao opera transicoes nem acessa cadastros
        (await token.PostAsJsonAsync($"/api/ordens-servico/{minha.Id}/iniciar-diagnostico", new { }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await token.GetAsync("/api/clientes")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Cliente_InativadoEReativadoAoAbrirNovaOs()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync();
        var (cliente, _) = await CriarClienteComVeiculoAsync(admin);

        var inativo = await Post<ClienteResponse>(admin, $"/api/clientes/{cliente.Id}/inativar", new { });
        inativo.Ativo.Should().BeFalse();

        await CriarOsAsync(admin, cliente);

        var atual = await admin.GetFromJsonAsync<ClienteResponse>($"/api/clientes/{cliente.Id}");
        atual!.Ativo.Should().BeTrue();
    }

    [Fact]
    public async Task Relatorio_TempoPorStatus_RetornaPeriodosDasOsConcluidas()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync();
        var (cliente, _) = await CriarClienteComVeiculoAsync(admin);
        await CriarOsAsync(admin, cliente, ateEntrega: true);

        var rel = await admin.GetFromJsonAsync<List<TempoMedioStatusResponse>>("/api/ordens-servico/relatorios/tempo-por-status");
        rel!.Select(r => r.Status).Should().Contain(new[] { "EmDiagnostico", "EmExecucao" });
    }

    [Fact]
    public async Task Erro_UsaProblemDetailsComCorrelationIdDoCliente()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync();
        var req = new HttpRequestMessage(HttpMethod.Get, $"/api/ordens-servico/{Guid.NewGuid()}");
        req.Headers.Add("x-correlation-id", "teste-correlacao-123");

        var resp = await admin.SendAsync(req);

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        resp.Headers.GetValues("x-correlation-id").Should().ContainSingle("teste-correlacao-123");
        var corpo = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        corpo.GetProperty("status").GetInt32().Should().Be(404);
        corpo.GetProperty("correlationId").GetString().Should().Be("teste-correlacao-123");
    }

    [Fact]
    public async Task Erro_ValidacaoTrazListaDeCampos()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync();

        var resp = await admin.PostAsJsonAsync("/api/lojas", new CriarLojaRequest("", ""));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        corpo.GetProperty("erros").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData("valor com espaco")]
    [InlineData("<script>alert(1)</script>")]
    public async Task CorrelationId_InseguroEDescartadoEUmNovoEGerado(string recebido)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        req.Headers.TryAddWithoutValidation("x-correlation-id", recebido);

        var resp = await _factory.CreateClient().SendAsync(req);

        var devolvido = resp.Headers.GetValues("x-correlation-id").Single();
        devolvido.Should().NotBe(recebido);
        Guid.TryParse(devolvido, out _).Should().BeTrue();
    }

    // --- helpers ---

    private static async Task<(ClienteResponse, VeiculoResponse)> CriarClienteComVeiculoAsync(HttpClient client)
    {
        var cliente = await Post<ClienteResponse>(client, "/api/clientes",
            new CriarClienteRequest("Cliente F3", TestHelpers.GerarCpf(), "cliente@exemplo.com", null));
        var veiculo = await Post<VeiculoResponse>(client, "/api/veiculos",
            new CriarVeiculoRequest(cliente.Id, TestHelpers.GerarPlaca(), "VW", "Gol", 2020));
        return (cliente, veiculo);
    }

    private static async Task<OrdemServicoResponse> CriarOsAsync(HttpClient client, ClienteResponse cliente, bool ateEntrega = false)
    {
        var veiculos = await client.GetFromJsonAsync<List<VeiculoResponse>>($"/api/veiculos?clienteId={cliente.Id}");
        var servicos = await client.GetFromJsonAsync<List<ServicoResponse>>("/api/servicos");

        var os = await Post<OrdemServicoResponse>(client, "/api/ordens-servico",
            new CriarOrdemServicoRequest(await client.ObterLojaCentroAsync(), cliente.Id, veiculos![0].Id));
        if (!ateEntrega) return os;

        await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/iniciar-diagnostico", new { });
        os = await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/servicos",
            new AdicionarServicoRequest(servicos![0].Id));
        await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/finalizar-diagnostico", new { });
        await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/aprovar", new { });
        await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/servicos/{os.Servicos[0].Id}/executar", new { });
        await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/finalizar-execucao", new { });
        return await Post<OrdemServicoResponse>(client, $"/api/ordens-servico/{os.Id}/entregar", new { });
    }

    private static async Task<T> Post<T>(HttpClient client, string url, object body)
    {
        var resp = await client.PostAsJsonAsync(url, body);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<T>())!;
    }
}
