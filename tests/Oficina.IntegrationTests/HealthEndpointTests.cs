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
    public async Task Get_Health_DeveRetornar200()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
