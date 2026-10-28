using FluentValidation;
using Oficina.Application.Abstractions;
using Oficina.Application.Common;
using Oficina.Domain.Lojas;

namespace Oficina.Application.Lojas;

/// <summary>Cadastro das unidades da rede (expansao para multiplas lojas).</summary>
public sealed class LojaService
{
    private const string NaoEncontrada = "Loja nao encontrada.";
    private readonly ILojaRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IValidator<CriarLojaRequest> _criarValidator;
    private readonly IValidator<AtualizarLojaRequest> _atualizarValidator;

    public LojaService(ILojaRepository repo, IUnitOfWork uow,
        IValidator<CriarLojaRequest> criarValidator, IValidator<AtualizarLojaRequest> atualizarValidator)
    {
        _repo = repo;
        _uow = uow;
        _criarValidator = criarValidator;
        _atualizarValidator = atualizarValidator;
    }

    public async Task<LojaResponse> CriarAsync(CriarLojaRequest req, CancellationToken ct = default)
    {
        await _criarValidator.ValidateAndThrowAsync(req, ct);
        var loja = new Loja(req.Nome, req.Cidade);
        await _repo.AddAsync(loja, ct);
        await _uow.SaveChangesAsync(ct);
        return Map(loja);
    }

    public async Task<LojaResponse> AtualizarAsync(Guid id, AtualizarLojaRequest req, CancellationToken ct = default)
    {
        await _atualizarValidator.ValidateAndThrowAsync(req, ct);
        var loja = await _repo.ObterOuFalharAsync(id, NaoEncontrada, ct);
        loja.Atualizar(req.Nome, req.Cidade);
        await _uow.SaveChangesAsync(ct);
        return Map(loja);
    }

    public async Task<LojaResponse> ObterAsync(Guid id, CancellationToken ct = default)
        => Map(await _repo.ObterOuFalharAsync(id, NaoEncontrada, ct));

    public async Task<IReadOnlyList<LojaResponse>> ListarAsync(CancellationToken ct = default)
        => (await _repo.ListAsync(ct)).Select(Map).ToList();

    public async Task<LojaResponse> DesativarAsync(Guid id, CancellationToken ct = default)
    {
        var loja = await _repo.ObterOuFalharAsync(id, NaoEncontrada, ct);
        loja.Desativar();
        await _uow.SaveChangesAsync(ct);
        return Map(loja);
    }

    private static LojaResponse Map(Loja l) => new(l.Id, l.Nome, l.Cidade, l.Ativa);
}
