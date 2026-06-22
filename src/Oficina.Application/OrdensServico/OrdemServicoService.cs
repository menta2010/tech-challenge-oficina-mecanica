using FluentValidation;
using Oficina.Application.Abstractions;
using Oficina.Application.Common;
using Oficina.Domain.OrdensServico;

namespace Oficina.Application.OrdensServico;

/// <summary>
/// Casos de uso do nucleo do dominio: ciclo de vida da OS.
/// Orquestra os agregados OrdemServico, Servico, PecaInsumo (baixa de estoque) e Cliente/Veiculo.
/// </summary>
public sealed class OrdemServicoService
{
    private readonly IOrdemServicoRepository _repo;
    private readonly IClienteRepository _clientes;
    private readonly IVeiculoRepository _veiculos;
    private readonly IServicoRepository _servicos;
    private readonly IPecaInsumoRepository _pecas;
    private readonly IUnitOfWork _uow;
    private readonly IValidator<CriarOrdemServicoRequest> _criarValidator;
    private readonly IValidator<AdicionarServicoRequest> _addServicoValidator;
    private readonly IValidator<AdicionarPecaRequest> _addPecaValidator;

    public OrdemServicoService(
        IOrdemServicoRepository repo, IClienteRepository clientes, IVeiculoRepository veiculos,
        IServicoRepository servicos, IPecaInsumoRepository pecas, IUnitOfWork uow,
        IValidator<CriarOrdemServicoRequest> criarValidator,
        IValidator<AdicionarServicoRequest> addServicoValidator,
        IValidator<AdicionarPecaRequest> addPecaValidator)
    {
        _repo = repo; _clientes = clientes; _veiculos = veiculos; _servicos = servicos;
        _pecas = pecas; _uow = uow; _criarValidator = criarValidator;
        _addServicoValidator = addServicoValidator; _addPecaValidator = addPecaValidator;
    }

    // --- Criacao ---
    public async Task<OrdemServicoResponse> CriarAsync(CriarOrdemServicoRequest req, CancellationToken ct = default)
    {
        await _criarValidator.ValidateAndThrowAsync(req, ct);

        if (await _clientes.GetByIdAsync(req.ClienteId, ct) is null)
            throw new NotFoundException("Cliente nao encontrado.");
        var veiculo = await _veiculos.GetByIdAsync(req.VeiculoId, ct)
                      ?? throw new NotFoundException("Veiculo nao encontrado.");
        if (veiculo.ClienteId != req.ClienteId)
            throw new ConflictException("O veiculo informado nao pertence a este cliente.");

        var os = new OrdemServico(req.ClienteId, req.VeiculoId);
        await _repo.AddAsync(os, ct);
        await _uow.SaveChangesAsync(ct);
        return Map(os);
    }

    // --- Diagnostico ---
    public Task<OrdemServicoResponse> IniciarDiagnosticoAsync(Guid id, CancellationToken ct = default)
        => MutarAsync(id, os => os.IniciarDiagnostico(), ct);

    public async Task<OrdemServicoResponse> AdicionarServicoAsync(Guid id, AdicionarServicoRequest req, CancellationToken ct = default)
    {
        await _addServicoValidator.ValidateAndThrowAsync(req, ct);
        var servico = await _servicos.GetByIdAsync(req.ServicoId, ct)
                      ?? throw new NotFoundException("Servico nao encontrado no catalogo.");
        return await MutarAsync(id, os =>
            os.RegistrarServico(servico.Id, servico.Nome, servico.ValorBase, servico.TempoEstimado), ct);
    }

    public async Task<OrdemServicoResponse> AdicionarPecaAsync(Guid id, AdicionarPecaRequest req, CancellationToken ct = default)
    {
        await _addPecaValidator.ValidateAndThrowAsync(req, ct);
        var peca = await _pecas.GetByIdAsync(req.PecaId, ct)
                   ?? throw new NotFoundException("Peca/insumo nao encontrado.");
        return await MutarAsync(id, os =>
            os.RegistrarPeca(peca.Id, req.Quantidade, peca.ValorUnitario), ct);
    }

    public Task<OrdemServicoResponse> RemoverServicoAsync(Guid id, Guid itemId, CancellationToken ct = default)
        => MutarAsync(id, os => os.RemoverServico(itemId), ct);

    public Task<OrdemServicoResponse> RemoverPecaAsync(Guid id, Guid itemId, CancellationToken ct = default)
        => MutarAsync(id, os => os.RemoverPeca(itemId), ct);

    public Task<OrdemServicoResponse> FinalizarDiagnosticoAsync(Guid id, CancellationToken ct = default)
        => MutarAsync(id, os => os.FinalizarDiagnostico(), ct);

    // --- Orcamento ---
    public Task<OrdemServicoResponse> AprovarAsync(Guid id, CancellationToken ct = default)
        => MutarAsync(id, os => os.AprovarOrcamento(), ct);

    public Task<OrdemServicoResponse> CancelarAsync(Guid id, CancellationToken ct = default)
        => MutarAsync(id, os => os.Cancelar(), ct);

    // --- Execucao ---
    public Task<OrdemServicoResponse> ExecutarServicoAsync(Guid id, Guid itemId, CancellationToken ct = default)
        => MutarAsync(id, os => os.ExecutarServico(itemId), ct);

    /// <summary>
    /// Registra o uso de uma peca e baixa o estoque (cross-agregado).
    /// Atende ao requisito "baixa de estoque quando peca/insumo for utilizado".
    /// </summary>
    public async Task<OrdemServicoResponse> UsarPecaAsync(Guid id, Guid itemId, CancellationToken ct = default)
    {
        var os = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Ordem de servico nao encontrada.");
        var item = os.Pecas.FirstOrDefault(p => p.Id == itemId)
                   ?? throw new NotFoundException("Item de peca nao encontrado na OS.");
        if (item.Utilizado)
            throw new ConflictException("Esta peca ja foi utilizada/baixada.");

        var peca = await _pecas.GetByIdAsync(item.PecaId, ct)
                   ?? throw new NotFoundException("Peca/insumo do item nao encontrada no estoque.");

        peca.Baixar(item.Quantidade);     // pode lancar DomainException (estoque insuficiente) -> 400
        os.RegistrarUsoPeca(itemId);       // valida status EmExecucao

        _pecas.Update(peca);
        _repo.Update(os);
        await _uow.SaveChangesAsync(ct);   // ambos confirmados na mesma unidade de trabalho
        return Map(os);
    }

    public Task<OrdemServicoResponse> FinalizarExecucaoAsync(Guid id, CancellationToken ct = default)
        => MutarAsync(id, os => os.FinalizarExecucao(), ct);

    // --- Entrega ---
    public Task<OrdemServicoResponse> EntregarAsync(Guid id, CancellationToken ct = default)
        => MutarAsync(id, os => os.EntregarVeiculo(), ct);

    // --- Consultas ---
    public async Task<OrdemServicoResponse> ObterAsync(Guid id, CancellationToken ct = default)
    {
        var os = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Ordem de servico nao encontrada.");
        return Map(os);
    }

    public async Task<IReadOnlyList<OrdemServicoResponse>> ListarAsync(StatusOS? status, CancellationToken ct = default)
    {
        var lista = status is null
            ? await _repo.ListAsync(ct)
            : await _repo.ListByStatusAsync(status.Value, ct);
        return lista.Select(Map).ToList();
    }

    /// <summary>Consulta publica de andamento pelo cliente (nao altera estado).</summary>
    public async Task<AcompanhamentoResponse> AcompanharAsync(Guid id, CancellationToken ct = default)
    {
        var os = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Ordem de servico nao encontrada.");
        return new AcompanhamentoResponse(os.Id, os.Status.ToString(), os.Orcamento?.PrevisaoEntrega, os.CriadaEm);
    }

    /// <summary>Tempo medio de execucao (entre inicio e fim) das OSs finalizadas/entregues.</summary>
    public async Task<TempoMedioExecucaoResponse> TempoMedioExecucaoAsync(CancellationToken ct = default)
    {
        var ordens = await _repo.ListFinalizadasOuEntreguesAsync(ct);
        var tempos = ordens.Where(o => o.TempoExecucao.HasValue)
                           .Select(o => o.TempoExecucao!.Value.TotalMinutes)
                           .ToList();
        return tempos.Count == 0
            ? new TempoMedioExecucaoResponse(0, 0)
            : new TempoMedioExecucaoResponse(tempos.Count, Math.Round(tempos.Average(), 2));
    }

    // --- internos ---
    private async Task<OrdemServicoResponse> MutarAsync(Guid id, Action<OrdemServico> acao, CancellationToken ct)
    {
        var os = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Ordem de servico nao encontrada.");
        acao(os);
        _repo.Update(os);
        await _uow.SaveChangesAsync(ct);
        return Map(os);
    }

    private static OrdemServicoResponse Map(OrdemServico os) => new(
        os.Id, os.ClienteId, os.VeiculoId, os.Status.ToString(),
        os.Servicos.Select(s => new ItemServicoResponse(s.Id, s.ServicoId, s.Descricao, s.Valor.Valor, s.Executado)).ToList(),
        os.Pecas.Select(p => new ItemPecaResponse(p.Id, p.PecaId, p.Quantidade, p.ValorUnitario.Valor, p.Subtotal.Valor, p.Utilizado)).ToList(),
        os.Orcamento is null ? null : new OrcamentoResponse(
            os.Orcamento.ValorServicos.Valor, os.Orcamento.ValorPecas.Valor, os.Orcamento.ValorTotal.Valor,
            os.Orcamento.PrevisaoEntrega, os.Orcamento.GeradoEm),
        os.CriadaEm, os.ExecucaoIniciadaEm, os.ExecucaoFinalizadaEm, os.EntregueEm, os.CanceladaEm,
        os.TempoExecucao?.TotalMinutes);
}
