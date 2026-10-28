namespace Oficina.Application.Abstractions;

/// <summary>
/// Quem esta chamando a API, extraido do JWT. Usado pelos casos de uso para limitar o escopo:
/// Atendente ve apenas a propria loja; Cliente ve apenas as proprias OS.
/// </summary>
public interface IUsuarioAtual
{
    string? Perfil { get; }
    Guid? LojaId { get; }

    /// <summary>Id do cliente quando o token foi emitido pela autenticacao por CPF.</summary>
    Guid? ClienteId { get; }

    bool EhAdmin { get; }
    bool EhAtendente { get; }
    bool EhCliente { get; }
}
