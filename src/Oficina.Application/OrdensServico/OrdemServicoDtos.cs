namespace Oficina.Application.OrdensServico;

// Abertura da OS: loja, cliente, veiculo e, opcionalmente, os servicos e pecas ja identificados.
// Quando servicos/pecas sao informados, a OS ja entra em diagnostico com os itens registrados.
// LojaId e ClienteId sao opcionais quando o token ja os define (atendente / cliente autenticado).
public record CriarOrdemServicoRequest(
    Guid? LojaId,
    Guid? ClienteId,
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
public record HistoricoStatusResponse(string Status, DateTime Inicio, DateTime? Fim, double? DuracaoMinutos);

public record OrdemServicoResponse(
    Guid Id,
    Guid LojaId,
    Guid ClienteId,
    Guid VeiculoId,
    string Status,
    bool Encerrada,
    IReadOnlyList<ItemServicoResponse> Servicos,
    IReadOnlyList<ItemPecaResponse> Pecas,
    OrcamentoResponse? Orcamento,
    IReadOnlyList<HistoricoStatusResponse> Historico,
    DateTime CriadaEm,
    DateTime? ExecucaoIniciadaEm,
    DateTime? ExecucaoFinalizadaEm,
    DateTime? EntregueEm,
    DateTime? CanceladaEm,
    double? TempoExecucaoMinutos);

// Consulta de andamento pelo cliente - visao reduzida
public record AcompanhamentoResponse(Guid Id, string Status, DateTime? PrevisaoEntrega, DateTime CriadaEm);

// Relatorios gerenciais
public record TempoMedioExecucaoResponse(int OrdensConsideradas, double TempoMedioMinutos);
public record TempoMedioStatusResponse(string Status, int Periodos, double TempoMedioMinutos);
