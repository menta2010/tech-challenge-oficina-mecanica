namespace Oficina.Infrastructure.Identity;

/// <summary>Configuracao do JWT, vinda da secao "Jwt" do appsettings.</summary>
public sealed class JwtOptions
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}
