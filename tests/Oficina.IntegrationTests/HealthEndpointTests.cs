using System.Net;
using FluentAssertions;
using Xunit;

namespace Oficina.IntegrationTests;

[Collection("api")]
public class HealthEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;
    public HealthEndpointTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Get_HealthLive_DeveRetornarHealthy()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("Healthy");
    }

    [Fact]
    public async Task Get_HealthReady_DeveValidarPostgresERetornarHealthy()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("Healthy");
    }
}
