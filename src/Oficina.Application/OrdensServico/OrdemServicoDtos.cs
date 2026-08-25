namespace Oficina.Application.OrdensServico;

// Abertura da OS: cliente, veiculo e, opcionalmente, os servicos e pecas ja identificados.
// Quando servicos/pecas sao informados, a OS ja entra em diagnostico com os itens registrados.
public record CriarOrdemServicoRequest(
    Guid ClienteId,
    Guid VeiculoId,
    IReadOnlyList<AdicionarServicoRequest>? Servicos = null,
    IReadOnlyList<AdicionarPecaRequest>? Pecas = null);
public record AdicionarServicoRequest(Guid ServicoId);
public record AdicionarPecaRequest(Guid PecaId, int Quantidade);

// Notificacao externa (webhook) de aprovacao/recusa do orcamento pelo cliente
public record RespostaOrcamentoRequest(bool Aprovado, string? Motivo);

public record ItemServicoResponse(Guid Id, Guid ServicoId, string Descricao, decimal Valor, bool Executado);
public record ItemPecaResponse(Guid Id, Guid PecaId, int Quantidade, decimal ValorUnitario, decimal Subtotal, bool Utilizado);
public record OrcamentoResponse(decimal ValorServicos, decimal ValorPecas, decimal ValorTotal, DateTime PrevisaoEntrega, DateTime GeradoEm);

public record OrdemServicoResponse(
    Guid Id,
    Guid ClienteId,
    Guid VeiculoId,
    string Status,
    IReadOnlyList<ItemServicoResponse> Servicos,
    IReadOnlyList<ItemPecaResponse> Pecas,
    OrcamentoResponse? Orcamento,
    DateTime CriadaEm,
    DateTime? ExecucaoIniciadaEm,
    DateTime? ExecucaoFinalizadaEm,
    DateTime? EntregueEm,
    DateTime? CanceladaEm,
    double? TempoExecucaoMinutos);

// Consulta publica de andamento (cliente, via API) - visao reduzida
public record AcompanhamentoResponse(Guid Id, string Status, DateTime? PrevisaoEntrega, DateTime CriadaEm);

// Relatorio gerencial
public record TempoMedioExecucaoResponse(int OrdensConsideradas, double TempoMedioMinutos);
