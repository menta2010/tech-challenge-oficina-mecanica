using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Estoque;
using Oficina.Domain.Identidade;
using Oficina.Domain.Lojas;
using Oficina.Domain.Servicos;
using Oficina.Domain.Shared;
using Oficina.Infrastructure.Identity;

namespace Oficina.Infrastructure.Persistence;

/// <summary>
/// Aplica as migrations do EF Core (schema versionado) e popula os dados iniciais:
/// duas lojas, catalogo de servicos/pecas, estoque inicial e usuarios admin e atendente.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(OficinaDbContext db, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);
        await SeedAsync(db, ct);
    }

    private static async Task SeedAsync(OficinaDbContext db, CancellationToken ct)
    {
        if (await db.Lojas.AnyAsync(ct)) return; // seed roda uma unica vez

        var centro = new Loja("Unidade Centro", "Sao Paulo");
        var zonaSul = new Loja("Unidade Zona Sul", "Sao Paulo");
        db.Lojas.AddRange(centro, zonaSul);

        db.Servicos.AddRange(
            new Servico("Troca de oleo", Money.From(120m), TimeSpan.FromMinutes(40), "Troca de oleo e filtro"),
            new Servico("Alinhamento", Money.From(90m), TimeSpan.FromMinutes(50), "Alinhamento e balanceamento"),
            new Servico("Revisao de freios", Money.From(180m), TimeSpan.FromHours(1), "Inspecao e troca de pastilhas"));

        var pecas = new[]
        {
            new PecaInsumo("Filtro de oleo", Money.From(35m)),
            new PecaInsumo("Oleo 5W30 (litro)", Money.From(45m)),
            new PecaInsumo("Pastilha de freio (par)", Money.From(160m)),
        };
        db.PecasInsumos.AddRange(pecas);
        foreach (var peca in pecas)
        {
            db.EstoquesLoja.Add(new EstoqueLoja(centro.Id, peca.Id, 50));
            db.EstoquesLoja.Add(new EstoqueLoja(zonaSul.Id, peca.Id, 20));
        }

        // Senhas do seed via variavel de ambiente (Secret no Kubernetes); fallback so para desenvolvimento
        var hasher = new Pbkdf2PasswordHasher();
        db.Usuarios.Add(new Usuario("admin",
            hasher.Hash(Environment.GetEnvironmentVariable("SEED_ADMIN_PASSWORD") ?? "admin123"), Perfis.Admin));
        db.Usuarios.Add(new Usuario("atendente.centro",
            hasher.Hash(Environment.GetEnvironmentVariable("SEED_ATENDENTE_PASSWORD") ?? "atendente123"), Perfis.Atendente, centro.Id));

        await db.SaveChangesAsync(ct);
    }
}
