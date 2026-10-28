using FluentValidation;

namespace Oficina.Application.Lojas;

public sealed class CriarLojaValidator : AbstractValidator<CriarLojaRequest>
{
    public CriarLojaValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Cidade).NotEmpty().MaximumLength(120);
    }
}

public sealed class AtualizarLojaValidator : AbstractValidator<AtualizarLojaRequest>
{
    public AtualizarLojaValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Cidade).NotEmpty().MaximumLength(120);
    }
}
