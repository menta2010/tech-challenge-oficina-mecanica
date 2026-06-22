using Oficina.Application.Abstractions;
using Oficina.Application.Common;

namespace Oficina.Application.Identidade;

/// <summary>Caso de uso de autenticacao: valida credenciais e emite o JWT.</summary>
public sealed class AuthService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenGenerator _tokens;

    public AuthService(IUsuarioRepository usuarios, IPasswordHasher hasher, IJwtTokenGenerator tokens)
    {
        _usuarios = usuarios;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        var usuario = await _usuarios.GetByUsernameAsync(req.Username ?? string.Empty, ct);
        if (usuario is null || !_hasher.Verify(usuario.PasswordHash, req.Password ?? string.Empty))
            throw new UnauthorizedException("Usuario ou senha invalidos.");

        var (token, expiraEm) = _tokens.Generate(usuario);
        return new LoginResponse(token, expiraEm, usuario.Username, usuario.Role);
    }
}
