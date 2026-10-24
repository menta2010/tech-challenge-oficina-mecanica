namespace Oficina.Application.Servicos;

public record CriarServicoRequest(string Nome, decimal ValorBase, int TempoEstimadoMinutos, string? Descricao);
public record AtualizarServicoRequest(string Nome, decimal ValorBase, int TempoEstimadoMinutos, string? Descricao);
public record ServicoResponse(Guid Id, string Nome, decimal ValorBase, int TempoEstimadoMinutos, string? Descricao);
