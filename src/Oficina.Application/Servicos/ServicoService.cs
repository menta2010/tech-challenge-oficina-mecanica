using FluentValidation;
using Oficina.Application.Abstractions;
using Oficina.Application.Common;
using Oficina.Domain.Servicos;
using Oficina.Domain.Shared;

namespace Oficina.Application.Servicos;

/// <summary>Casos de uso de Servico (catalogo). Atende ao CRUD de servicos.</summary>
public sealed class ServicoService
{
    private readonly IServicoRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IValidator<CreateServicoRequest> _createValidator;
    private readonly IValidator<UpdateServicoRequest> _updateValidator;

    public ServicoService(IServicoRepository repo, IUnitOfWork uow,
        IValidator<CreateServicoRequest> createValidator, IValidator<UpdateServicoRequest> updateValidator)
    {
        _repo = repo;
        _uow = uow;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<ServicoResponse> CriarAsync(CreateServicoRequest req, CancellationToken ct = default)
    {
        await _createValidator.ValidateAndThrowAsync(req, ct);
        var servico = new Servico(req.Nome, Money.From(req.ValorBase),
            TimeSpan.FromMinutes(req.TempoEstimadoMinutos), req.Descricao);
        await _repo.AddAsync(servico, ct);
        await _uow.SaveChangesAsync(ct);
        return Map(servico);
    }

    public async Task<ServicoResponse> AtualizarAsync(Guid id, UpdateServicoRequest req, CancellationToken ct = default)
    {
        await _updateValidator.ValidateAndThrowAsync(req, ct);
        var servico = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Servico nao encontrado.");
        servico.Atualizar(req.Nome, Money.From(req.ValorBase), TimeSpan.FromMinutes(req.TempoEstimadoMinutos), req.Descricao);
        _repo.Update(servico);
        await _uow.SaveChangesAsync(ct);
        return Map(servico);
    }

    public async Task<ServicoResponse> ObterAsync(Guid id, CancellationToken ct = default)
    {
        var servico = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Servico nao encontrado.");
        return Map(servico);
    }

    public async Task<IReadOnlyList<ServicoResponse>> ListarAsync(CancellationToken ct = default)
        => (await _repo.ListAsync(ct)).Select(Map).ToList();

    public async Task RemoverAsync(Guid id, CancellationToken ct = default)
    {
        var servico = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Servico nao encontrado.");
        _repo.Remove(servico);
        await _uow.SaveChangesAsync(ct);
    }

    private static ServicoResponse Map(Servico s)
        => new(s.Id, s.Nome, s.ValorBase.Valor, (int)s.TempoEstimado.TotalMinutes, s.Descricao);
}
