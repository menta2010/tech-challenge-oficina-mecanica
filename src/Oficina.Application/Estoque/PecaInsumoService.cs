using FluentValidation;
using Oficina.Application.Abstractions;
using Oficina.Application.Common;
using Oficina.Domain.Estoque;
using Oficina.Domain.Shared;

namespace Oficina.Application.Estoque;

/// <summary>Casos de uso de Peca/Insumo. Atende ao CRUD com controle de estoque (incluindo reabastecimento).</summary>
public sealed class PecaInsumoService
{
    private readonly IPecaInsumoRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IValidator<CreatePecaInsumoRequest> _createValidator;
    private readonly IValidator<UpdatePecaInsumoRequest> _updateValidator;
    private readonly IValidator<ReabastecerEstoqueRequest> _reabastecerValidator;

    public PecaInsumoService(IPecaInsumoRepository repo, IUnitOfWork uow,
        IValidator<CreatePecaInsumoRequest> createValidator,
        IValidator<UpdatePecaInsumoRequest> updateValidator,
        IValidator<ReabastecerEstoqueRequest> reabastecerValidator)
    {
        _repo = repo;
        _uow = uow;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _reabastecerValidator = reabastecerValidator;
    }

    public async Task<PecaInsumoResponse> CriarAsync(CreatePecaInsumoRequest req, CancellationToken ct = default)
    {
        await _createValidator.ValidateAndThrowAsync(req, ct);
        var peca = new PecaInsumo(req.Nome, Money.From(req.ValorUnitario), req.QuantidadeEmEstoque);
        await _repo.AddAsync(peca, ct);
        await _uow.SaveChangesAsync(ct);
        return Map(peca);
    }

    public async Task<PecaInsumoResponse> AtualizarAsync(Guid id, UpdatePecaInsumoRequest req, CancellationToken ct = default)
    {
        await _updateValidator.ValidateAndThrowAsync(req, ct);
        var peca = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Peca/insumo nao encontrado.");
        peca.Atualizar(req.Nome, Money.From(req.ValorUnitario));
        _repo.Update(peca);
        await _uow.SaveChangesAsync(ct);
        return Map(peca);
    }

    public async Task<PecaInsumoResponse> ReabastecerAsync(Guid id, ReabastecerEstoqueRequest req, CancellationToken ct = default)
    {
        await _reabastecerValidator.ValidateAndThrowAsync(req, ct);
        var peca = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Peca/insumo nao encontrado.");
        peca.Repor(req.Quantidade);
        _repo.Update(peca);
        await _uow.SaveChangesAsync(ct);
        return Map(peca);
    }

    public async Task<PecaInsumoResponse> ObterAsync(Guid id, CancellationToken ct = default)
    {
        var peca = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Peca/insumo nao encontrado.");
        return Map(peca);
    }

    public async Task<IReadOnlyList<PecaInsumoResponse>> ListarAsync(CancellationToken ct = default)
        => (await _repo.ListAsync(ct)).Select(Map).ToList();

    public async Task RemoverAsync(Guid id, CancellationToken ct = default)
    {
        var peca = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Peca/insumo nao encontrado.");
        _repo.Remove(peca);
        await _uow.SaveChangesAsync(ct);
    }

    private static PecaInsumoResponse Map(PecaInsumo p)
        => new(p.Id, p.Nome, p.ValorUnitario.Valor, p.QuantidadeEmEstoque);
}
