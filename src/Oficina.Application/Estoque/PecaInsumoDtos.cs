namespace Oficina.Application.Estoque;

public record CriarPecaInsumoRequest(string Nome, decimal ValorUnitario);
public record AtualizarPecaInsumoRequest(string Nome, decimal ValorUnitario);
public record PecaInsumoResponse(Guid Id, string Nome, decimal ValorUnitario);

public record ReabastecerEstoqueRequest(int Quantidade);
public record EstoqueLojaResponse(Guid LojaId, Guid PecaInsumoId, string Peca, int Quantidade);
