using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Xunit;

namespace Oficina.IntegrationTests;

/// <summary>
/// Sobe a API em memoria apontando para um PostgreSQL real em container (Testcontainers).
/// O schema e os seeds sao criados no startup pelo DbInitializer (EnsureCreated).
/// Requer Docker disponivel na maquina que roda os testes.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("oficina")
        .WithUsername("oficina")
        .WithPassword("oficina")
        .Build();

    public Task InitializeAsync() => _db.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _db.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _db.GetConnectionString()
            });
        });
    }
}

/// <summary>Compartilha um unico container/host entre as classes de teste.</summary>
[CollectionDefinition("api")]
public class ApiCollection : ICollectionFixture<CustomWebApplicationFactory> { }
