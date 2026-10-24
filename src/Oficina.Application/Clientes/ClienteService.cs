using FluentValidation;
using Oficina.Application.Abstractions;
using Oficina.Application.Common;
using Oficina.Domain.Clientes;

namespace Oficina.Application.Clientes;

/// <summary>
/// Casos de uso de Cliente (CRUD). Atende ao requisito de identificacao por CPF/CNPJ
/// e ao CRUD de clientes. A unicidade de documento e garantida aqui.
/// </summary>
public sealed class ClienteService
{
    private readonly IClienteRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IValidator<CriarClienteRequest> _createValidator;
    private readonly IValidator<AtualizarClienteRequest> _updateValidator;

    public ClienteService(IClienteRepository repo, IUnitOfWork uow,
        IValidator<CriarClienteRequest> createValidator,
        IValidator<AtualizarClienteRequest> updateValidator)
    {
        _repo = repo;
        _uow = uow;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<ClienteResponse> CriarAsync(CriarClienteRequest req, CancellationToken ct = default)
    {
        await _createValidator.ValidateAndThrowAsync(req, ct);

        var documento = Documento.Criar(req.Documento); // valida CPF/CNPJ (DomainException -> 400)
        if (await _repo.ExisteDocumentoAsync(documento, ct))
            throw new ConflictException("Ja existe um cliente com este documento.");

        var cliente = new Cliente(req.Nome, documento, req.Email, req.Telefone);
        await _repo.AddAsync(cliente, ct);
        await _uow.SaveChangesAsync(ct);
        return Map(cliente);
    }

    public async Task<ClienteResponse> AtualizarAsync(Guid id, AtualizarClienteRequest req, CancellationToken ct = default)
    {
        await _updateValidator.ValidateAndThrowAsync(req, ct);
        var cliente = await _repo.ObterOuFalharAsync(id, "Cliente nao encontrado.", ct);
        cliente.Atualizar(req.Nome, req.Email, req.Telefone);
        _repo.Update(cliente);
        await _uow.SaveChangesAsync(ct);
        return Map(cliente);
    }

    public async Task<ClienteResponse> ObterAsync(Guid id, CancellationToken ct = default)
    {
        var cliente = await _repo.ObterOuFalharAsync(id, "Cliente nao encontrado.", ct);
        return Map(cliente);
    }

    public async Task<IReadOnlyList<ClienteResponse>> ListarAsync(CancellationToken ct = default)
        => (await _repo.ListAsync(ct)).Select(Map).ToList();

    public async Task RemoverAsync(Guid id, CancellationToken ct = default)
    {
        var cliente = await _repo.ObterOuFalharAsync(id, "Cliente nao encontrado.", ct);
        _repo.Remove(cliente);
        await _uow.SaveChangesAsync(ct);
    }

    /// <summary>Inativacao manual: o cliente deixa de conseguir se autenticar pelo CPF.</summary>
    public Task<ClienteResponse> InativarAsync(Guid id, CancellationToken ct = default)
        => AlterarStatusAsync(id, c => c.Inativar(), ct);

    public Task<ClienteResponse> ReativarAsync(Guid id, CancellationToken ct = default)
        => AlterarStatusAsync(id, c => c.Reativar(), ct);

    private async Task<ClienteResponse> AlterarStatusAsync(Guid id, Action<Cliente> acao, CancellationToken ct)
    {
        var cliente = await _repo.ObterOuFalharAsync(id, "Cliente nao encontrado.", ct);
        acao(cliente);
        await _uow.SaveChangesAsync(ct);
        return Map(cliente);
    }

    private static ClienteResponse Map(Cliente c)
        => new(c.Id, c.Nome, c.Documento.Numero, c.Documento.Tipo.ToString(), c.Email, c.Telefone, c.Ativo, c.InativadoEm);
}
