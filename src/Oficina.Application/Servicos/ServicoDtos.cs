namespace Oficina.Application.Servicos;

public record CreateServicoRequest(string Nome, decimal ValorBase, int TempoEstimadoMinutos, string? Descricao);
public record UpdateServicoRequest(string Nome, decimal ValorBase, int TempoEstimadoMinutos, string? Descricao);
public record ServicoResponse(Guid Id, string Nome, decimal ValorBase, int TempoEstimadoMinutos, string? Descricao);
