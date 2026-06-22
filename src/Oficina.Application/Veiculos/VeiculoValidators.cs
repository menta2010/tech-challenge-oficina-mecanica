using FluentValidation;

namespace Oficina.Application.Veiculos;

public sealed class CreateVeiculoValidator : AbstractValidator<CreateVeiculoRequest>
{
    public CreateVeiculoValidator()
    {
        RuleFor(x => x.ClienteId).NotEmpty();
        RuleFor(x => x.Placa).NotEmpty();
        RuleFor(x => x.Marca).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Modelo).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Ano).InclusiveBetween(1900, DateTime.UtcNow.Year + 1);
    }
}

public sealed class UpdateVeiculoValidator : AbstractValidator<UpdateVeiculoRequest>
{
    public UpdateVeiculoValidator()
    {
        RuleFor(x => x.Marca).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Modelo).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Ano).InclusiveBetween(1900, DateTime.UtcNow.Year + 1);
    }
}
