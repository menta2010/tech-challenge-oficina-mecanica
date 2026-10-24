using FluentValidation;
using Oficina.Application.Abstractions;
using Oficina.Application.Common;
using Oficina.Domain.Estoque;
using Oficina.Domain.Shared;

namespace Oficina.Application.Estoque;

/// <summary>Casos de uso do catalogo de pecas/insumos (global). O saldo por loja fica no EstoqueService.</summary>
public sealed class PecaInsumoService
{
    private const string NaoEncontrado = "Peca/insumo nao encontrado.";
    private readonly IPecaInsumoRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IValidator<CriarPecaInsumoRequest> _criarValidator;
    private readonly IValidator<AtualizarPecaInsumoRequest> _atualizarValidator;

    public PecaInsumoService(IPecaInsumoRepository repo, IUnitOfWork uow,
        IValidator<CriarPecaInsumoRequest> criarValidator,
        IValidator<AtualizarPecaInsumoRequest> atualizarValidator)
    {
        _repo = repo;
        _uow = uow;
        _criarValidator = criarValidator;
        _atualizarValidator = atualizarValidator;
    }

    public async Task<PecaInsumoResponse> CriarAsync(CriarPecaInsumoRequest req, CancellationToken ct = default)
    {
        await _criarValidator.ValidateAndThrowAsync(req, ct);
        var peca = new PecaInsumo(req.Nome, Money.From(req.ValorUnitario));
        await _repo.AddAsync(peca, ct);
        await _uow.SaveChangesAsync(ct);
        return Map(peca);
    }

    public async Task<PecaInsumoResponse> AtualizarAsync(Guid id, AtualizarPecaInsumoRequest req, CancellationToken ct = default)
    {
        await _atualizarValidator.ValidateAndThrowAsync(req, ct);
        var peca = await _repo.ObterOuFalharAsync(id, NaoEncontrado, ct);
        peca.Atualizar(req.Nome, Money.From(req.ValorUnitario));
        await _uow.SaveChangesAsync(ct);
        return Map(peca);
    }

    public async Task<PecaInsumoResponse> ObterAsync(Guid id, CancellationToken ct = default)
        => Map(await _repo.ObterOuFalharAsync(id, NaoEncontrado, ct));

    public async Task<IReadOnlyList<PecaInsumoResponse>> ListarAsync(CancellationToken ct = default)
        => (await _repo.ListAsync(ct)).Select(Map).ToList();

    public async Task RemoverAsync(Guid id, CancellationToken ct = default)
    {
        var peca = await _repo.ObterOuFalharAsync(id, NaoEncontrado, ct);
        _repo.Remove(peca);
        await _uow.SaveChangesAsync(ct);
    }

    private static PecaInsumoResponse Map(PecaInsumo p) => new(p.Id, p.Nome, p.ValorUnitario.Valor);
}
