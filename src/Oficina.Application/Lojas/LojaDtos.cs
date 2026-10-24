namespace Oficina.Application.Lojas;

public record CriarLojaRequest(string Nome, string Cidade);
public record AtualizarLojaRequest(string Nome, string Cidade);
public record LojaResponse(Guid Id, string Nome, string Cidade, bool Ativa);
