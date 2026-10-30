namespace Oficina.Application.Veiculos;

public record CriarVeiculoRequest(Guid ClienteId, string Placa, string Marca, string Modelo, int Ano);
public record AtualizarVeiculoRequest(string Marca, string Modelo, int Ano);
public record VeiculoResponse(Guid Id, Guid ClienteId, string Placa, string Marca, string Modelo, int Ano);
