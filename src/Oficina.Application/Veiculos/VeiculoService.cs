using FluentValidation;
using Oficina.Application.Abstractions;
using Oficina.Application.Common;
using Oficina.Domain.Veiculos;

namespace Oficina.Application.Veiculos;

/// <summary>Casos de uso de Veiculo (CRUD). Atende ao cadastro de veiculo e validacao de placa.</summary>
public sealed class VeiculoService
{
    private readonly IVeiculoRepository _repo;
    private readonly IClienteRepository _clientes;
    private readonly IUnitOfWork _uow;
    private readonly IValidator<CreateVeiculoRequest> _createValidator;
    private readonly IValidator<UpdateVeiculoRequest> _updateValidator;

    public VeiculoService(IVeiculoRepository repo, IClienteRepository clientes, IUnitOfWork uow,
        IValidator<CreateVeiculoRequest> createValidator, IValidator<UpdateVeiculoRequest> updateValidator)
    {
        _repo = repo;
        _clientes = clientes;
        _uow = uow;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<VeiculoResponse> CriarAsync(CreateVeiculoRequest req, CancellationToken ct = default)
    {
        await _createValidator.ValidateAndThrowAsync(req, ct);

        if (await _clientes.GetByIdAsync(req.ClienteId, ct) is null)
            throw new NotFoundException("Cliente do veiculo nao encontrado.");

        var placa = Placa.Criar(req.Placa); // valida placa (DomainException -> 400)
        if (await _repo.GetByPlacaAsync(placa, ct) is not null)
            throw new ConflictException("Ja existe um veiculo com esta placa.");

        var veiculo = new Veiculo(req.ClienteId, placa, req.Marca, req.Modelo, req.Ano);
        await _repo.AddAsync(veiculo, ct);
        await _uow.SaveChangesAsync(ct);
        return Map(veiculo);
    }

    public async Task<VeiculoResponse> AtualizarAsync(Guid id, UpdateVeiculoRequest req, CancellationToken ct = default)
    {
        await _updateValidator.ValidateAndThrowAsync(req, ct);
        var veiculo = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Veiculo nao encontrado.");
        veiculo.Atualizar(req.Marca, req.Modelo, req.Ano);
        _repo.Update(veiculo);
        await _uow.SaveChangesAsync(ct);
        return Map(veiculo);
    }

    public async Task<VeiculoResponse> ObterAsync(Guid id, CancellationToken ct = default)
    {
        var veiculo = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Veiculo nao encontrado.");
        return Map(veiculo);
    }

    public async Task<IReadOnlyList<VeiculoResponse>> ListarAsync(Guid? clienteId, CancellationToken ct = default)
    {
        var lista = clienteId is null
            ? await _repo.ListAsync(ct)
            : await _repo.ListByClienteAsync(clienteId.Value, ct);
        return lista.Select(Map).ToList();
    }

    public async Task RemoverAsync(Guid id, CancellationToken ct = default)
    {
        var veiculo = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Veiculo nao encontrado.");
        _repo.Remove(veiculo);
        await _uow.SaveChangesAsync(ct);
    }

    private static VeiculoResponse Map(Veiculo v)
        => new(v.Id, v.ClienteId, v.Placa.Valor, v.Marca, v.Modelo, v.Ano);
}
