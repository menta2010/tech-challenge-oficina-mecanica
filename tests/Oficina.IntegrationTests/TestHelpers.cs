using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Oficina.Application.Lojas;
using Oficina.Domain.Identidade;

namespace Oficina.IntegrationTests;

internal static class TestHelpers
{
    public const string LojaCentro = "Unidade Centro";
    public const string LojaZonaSul = "Unidade Zona Sul";

    private record LoginDto(string Token, DateTime ExpiraEm, string Username, string Role);

    /// <summary>Faz login como admin (seed) e devolve um HttpClient autenticado.</summary>
    public static Task<HttpClient> CreateAuthenticatedClientAsync(this CustomWebApplicationFactory factory)
        => factory.LoginAsync("admin", "admin123");

    /// <summary>Atendente do seed, vinculado a Unidade Centro.</summary>
    public static Task<HttpClient> CreateAtendenteCentroClientAsync(this CustomWebApplicationFactory factory)
        => factory.LoginAsync("atendente.centro", "atendente123");

    private static async Task<HttpClient> LoginAsync(this CustomWebApplicationFactory factory, string usuario, string senha)
    {
        var client = factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/login", new { username = usuario, password = senha });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<LoginDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    /// <summary>
    /// Simula o token que a Lambda de autenticacao por CPF emite (role Cliente, sub = id do cliente),
    /// assinado com a mesma chave/issuer/audience configurados na API.
    /// </summary>
    public static HttpClient CreateClienteClient(this CustomWebApplicationFactory factory, Guid clienteId)
    {
        var config = factory.Services.GetRequiredService<IConfiguration>();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:SecretKey"]!));
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, clienteId.ToString()),
                new Claim(ClaimTypes.Role, Perfis.Cliente),
                new Claim("cpf", "52998224725")
            },
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        return client;
    }

    public static Task<Guid> ObterLojaCentroAsync(this HttpClient client) => client.ObterLojaAsync(LojaCentro);

    public static async Task<Guid> ObterLojaAsync(this HttpClient client, string nome)
    {
        var lojas = await client.GetFromJsonAsync<List<LojaResponse>>("/api/lojas");
        return lojas!.Single(l => l.Nome == nome).Id;
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
