namespace Oficina.Application.Common;

/// <summary>Autenticado, mas sem acesso ao recurso (ex.: OS de outra loja ou de outro cliente) -> HTTP 403.</summary>
public sealed class ForbiddenException : Exception
{
    public ForbiddenException(string mensagem) : base(mensagem) { }
}
