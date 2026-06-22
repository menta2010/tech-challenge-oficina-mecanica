namespace Oficina.Application.Common;

/// <summary>Credenciais invalidas -> HTTP 401.</summary>
public sealed class UnauthorizedException : Exception
{
    public UnauthorizedException(string mensagem) : base(mensagem) { }
}
