namespace Oficina.Application.Clientes;

public record CreateClienteRequest(string Nome, string Documento, string? Email, string? Telefone);
public record UpdateClienteRequest(string Nome, string? Email, string? Telefone);
public record ClienteResponse(Guid Id, string Nome, string Documento, string TipoDocumento, string? Email, string? Telefone);
