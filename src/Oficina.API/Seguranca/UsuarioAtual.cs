using System.Security.Claims;
using Oficina.Application.Abstractions;
using Oficina.Domain.Identidade;

namespace Oficina.API.Seguranca;

/// <summary>
/// Le o usuario do JWT da requisicao (emitido pela Lambda de autenticacao ou pelo login local).
/// Claims: role (Admin | Atendente | Cliente), loja_id (atendente) e sub (id do cliente, quando Cliente).
/// </summary>
public sealed class UsuarioAtual : IUsuarioAtual
{
    public const string ClaimLoja = "loja_id";

    private readonly IHttpContextAccessor _http;
    public UsuarioAtual(IHttpContextAccessor http) => _http = http;

    private ClaimsPrincipal? Principal => _http.HttpContext?.User;

    public string? Perfil => Principal?.FindFirst(ClaimTypes.Role)?.Value;

    public Guid? LojaId => LerGuid(ClaimLoja);

    public Guid? ClienteId => EhCliente ? LerGuid(ClaimTypes.NameIdentifier) : null;

    public bool EhAdmin => Perfil == Perfis.Admin;
    public bool EhAtendente => Perfil == Perfis.Atendente;
    public bool EhCliente => Perfil == Perfis.Cliente;

    private Guid? LerGuid(string claim)
        => Guid.TryParse(Principal?.FindFirst(claim)?.Value, out var id) ? id : null;
}

/// <summary>Token de servico do webhook de aprovacao de orcamento.</summary>
public sealed class WebhookOptions
{
    public string? Token { get; set; }

    public bool TokenValido(string? recebido)
        => !string.IsNullOrWhiteSpace(Token)
           && recebido is not null
           && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
               System.Text.Encoding.UTF8.GetBytes(Token), System.Text.Encoding.UTF8.GetBytes(recebido));
}
