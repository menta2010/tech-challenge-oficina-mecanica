using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Oficina.Infrastructure.Persistence;

/// <summary>
/// Fabrica usada apenas em tempo de design pelo 'dotnet ef' (migrations).
/// Le a connection string da variavel de ambiente ou usa um default local.
/// </summary>
public sealed class OficinaDbContextFactory : IDesignTimeDbContextFactory<OficinaDbContext>
{
    public OficinaDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
                   ?? "Host=localhost;Port=5432;Database=oficina;Username=oficina;Password=oficina";

        var options = new DbContextOptionsBuilder<OficinaDbContext>()
            .UseNpgsql(conn)
            .Options;

        return new OficinaDbContext(options);
    }
}
