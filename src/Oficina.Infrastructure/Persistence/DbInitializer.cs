using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Estoque;
using Oficina.Domain.Identidade;
using Oficina.Infrastructure.Identity;
using Oficina.Domain.Servicos;
using Oficina.Domain.Shared;

namespace Oficina.Infrastructure.Persistence;

/// <summary>
/// Cria/atualiza o schema e popula dados iniciais (catalogo de servicos e estoque).
/// Estrategia MVP: aplica migrations se existirem; caso contrario, EnsureCreated.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(OficinaDbContext db, CancellationToken ct = default)
    {
        if ((await db.Database.GetPendingMigrationsAsync(ct)).Any()
            || db.Database.GetMigrations().Any())
        {
            await db.Database.MigrateAsync(ct);
        }
        else
        {
            await db.Database.EnsureCreatedAsync(ct);
        }

        await SeedAsync(db, ct);
    }

    private static async Task SeedAsync(OficinaDbContext db, CancellationToken ct)
    {
        if (!await db.Servicos.AnyAsync(ct))
        {
            db.Servicos.AddRange(
                new Servico("Troca de oleo", Money.From(120m), TimeSpan.FromMinutes(40), "Troca de oleo e filtro"),
                new Servico("Alinhamento", Money.From(90m), TimeSpan.FromMinutes(50), "Alinhamento e balanceamento"),
                new Servico("Revisao de freios", Money.From(180m), TimeSpan.FromHours(1), "Inspecao e troca de pastilhas")
            );
        }

        if (!await db.PecasInsumos.AnyAsync(ct))
        {
            db.PecasInsumos.AddRange(
                new PecaInsumo("Filtro de oleo", Money.From(35m), 50),
                new PecaInsumo("Oleo 5W30 (litro)", Money.From(45m), 100),
                new PecaInsumo("Pastilha de freio (par)", Money.From(160m), 20)
            );
        }

        if (!await db.Usuarios.AnyAsync(ct))
        {
            var hasher = new Pbkdf2PasswordHasher();
            // senha do seed via variavel de ambiente; fallback apenas para ambiente de desenvolvimento
            var senhaAdmin = Environment.GetEnvironmentVariable("SEED_ADMIN_PASSWORD") ?? "admin123";
            db.Usuarios.Add(new Usuario("admin", hasher.Hash(senhaAdmin), "Admin"));
        }

        await db.SaveChangesAsync(ct);
    }
}
