namespace Oficina.Application.Common;

/// <summary>Recurso nao encontrado -> traduzido para HTTP 404.</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string mensagem) : base(mensagem) { }
}
