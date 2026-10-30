namespace Oficina.Application.Clientes;

public record CriarClienteRequest(string Nome, string Documento, string? Email, string? Telefone);
public record AtualizarClienteRequest(string Nome, string? Email, string? Telefone);
public record ClienteResponse(Guid Id, string Nome, string Documento, string TipoDocumento, string? Email, string? Telefone, bool Ativo, DateTime? InativadoEm);
