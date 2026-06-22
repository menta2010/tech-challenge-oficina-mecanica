namespace Oficina.Application.Estoque;

public record CreatePecaInsumoRequest(string Nome, decimal ValorUnitario, int QuantidadeEmEstoque);
public record UpdatePecaInsumoRequest(string Nome, decimal ValorUnitario);
public record ReabastecerEstoqueRequest(int Quantidade);
public record PecaInsumoResponse(Guid Id, string Nome, decimal ValorUnitario, int QuantidadeEmEstoque);
