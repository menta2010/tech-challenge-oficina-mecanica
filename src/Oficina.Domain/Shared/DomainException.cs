namespace Oficina.Domain.Shared;

/// <summary>
/// Excecao para violacoes de regra de negocio/invariante do dominio.
/// A camada de API a traduz para HTTP 400/409.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string mensagem) : base(mensagem) { }
}
