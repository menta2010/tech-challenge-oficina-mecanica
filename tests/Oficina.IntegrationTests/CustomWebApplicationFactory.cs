using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Infrastructure.Persistence;
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

        // Substitui o DbContext registrado pelo app para apontar ao banco do Testcontainers.
        // Feito em ConfigureServices (roda DEPOIS do registro do app), garantindo a troca.
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<OficinaDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<OficinaDbContext>(options =>
                options.UseNpgsql(_db.GetConnectionString()));
        });
    }
}

/// <summary>Compartilha um unico container/host entre as classes de teste.</summary>
[CollectionDefinition("api")]
public class ApiCollection : ICollectionFixture<CustomWebApplicationFactory> { }
