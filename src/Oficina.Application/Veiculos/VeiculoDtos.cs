namespace Oficina.Application.Veiculos;

public record CreateVeiculoRequest(Guid ClienteId, string Placa, string Marca, string Modelo, int Ano);
public record UpdateVeiculoRequest(string Marca, string Modelo, int Ano);
public record VeiculoResponse(Guid Id, Guid ClienteId, string Placa, string Marca, string Modelo, int Ano);
