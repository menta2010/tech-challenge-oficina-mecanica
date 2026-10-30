using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Oficina.Application.Abstractions;
using Oficina.Domain.Identidade;

namespace Oficina.Infrastructure.Identity;

/// <summary>
/// Emite o JWT de funcionario (login local/dev). Em nuvem, o login e feito pela Lambda de
/// autenticacao, que emite um token com as mesmas claims e a mesma chave.
/// </summary>
public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    public const string ClaimLoja = "loja_id";

    private readonly JwtOptions _options;
    public JwtTokenGenerator(IOptions<JwtOptions> options) => _options = options.Value;

    public (string Token, DateTime ExpiraEm) Generate(Usuario usuario)
    {
        var expiraEm = DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, usuario.Username),
            new(ClaimTypes.Role, usuario.Role)
        };
        if (usuario.LojaId is { } lojaId)
            claims.Add(new Claim(ClaimLoja, lojaId.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiraEm,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraEm);
    }
}
