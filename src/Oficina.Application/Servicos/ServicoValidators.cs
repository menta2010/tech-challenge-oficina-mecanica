using FluentValidation;

namespace Oficina.Application.Servicos;

public sealed class CreateServicoValidator : AbstractValidator<CreateServicoRequest>
{
    public CreateServicoValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(160);
        RuleFor(x => x.ValorBase).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TempoEstimadoMinutos).GreaterThan(0);
        RuleFor(x => x.Descricao).MaximumLength(500);
    }
}

public sealed class UpdateServicoValidator : AbstractValidator<UpdateServicoRequest>
{
    public UpdateServicoValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(160);
        RuleFor(x => x.ValorBase).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TempoEstimadoMinutos).GreaterThan(0);
        RuleFor(x => x.Descricao).MaximumLength(500);
    }
}
