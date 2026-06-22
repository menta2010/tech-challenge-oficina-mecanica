using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Oficina.IntegrationTests;

internal static class TestHelpers
{
    private record LoginDto(string Token, DateTime ExpiraEm, string Username, string Role);

    /// <summary>Faz login como admin (seed) e devolve um HttpClient autenticado.</summary>
    public static async Task<HttpClient> CreateAuthenticatedClientAsync(this CustomWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/login", new { username = "admin", password = "admin123" });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<LoginDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    private static readonly Random Rng = new();

    /// <summary>Gera um CPF valido (com digitos verificadores corretos) para evitar colisoes entre testes.</summary>
    public static string GerarCpf()
    {
        var n = new int[9];
        for (int i = 0; i < 9; i++) n[i] = Rng.Next(0, 10);

        int Dig(int[] nums)
        {
            int soma = 0, peso = nums.Length + 1;
            foreach (var d in nums) soma += d * peso--;
            int r = soma % 11;
            return r < 2 ? 0 : 11 - r;
        }

        int d1 = Dig(n);
        int d2 = Dig(n.Append(d1).ToArray());
        return string.Concat(n.Select(x => x.ToString())) + d1 + d2;
    }

    /// <summary>Gera uma placa no padrao antigo (AAA0000).</summary>
    public static string GerarPlaca()
    {
        const string letras = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var l = new string(Enumerable.Range(0, 3).Select(_ => letras[Rng.Next(letras.Length)]).ToArray());
        var d = Rng.Next(0, 10000).ToString("D4");
        return l + d;
    }
}
