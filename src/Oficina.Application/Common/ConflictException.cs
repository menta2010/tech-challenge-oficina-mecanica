namespace Oficina.Application.Common;

/// <summary>Conflito de estado (ex.: documento/placa ja cadastrados) -> HTTP 409.</summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string mensagem) : base(mensagem) { }
}
