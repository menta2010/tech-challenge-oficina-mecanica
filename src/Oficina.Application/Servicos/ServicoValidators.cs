using FluentValidation;

namespace Oficina.Application.Servicos;

public sealed class CriarServicoValidator : AbstractValidator<CriarServicoRequest>
{
    public CriarServicoValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(160);
        RuleFor(x => x.ValorBase).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TempoEstimadoMinutos).GreaterThan(0);
        RuleFor(x => x.Descricao).MaximumLength(500);
    }
}

public sealed class AtualizarServicoValidator : AbstractValidator<AtualizarServicoRequest>
{
    public AtualizarServicoValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(160);
        RuleFor(x => x.ValorBase).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TempoEstimadoMinutos).GreaterThan(0);
        RuleFor(x => x.Descricao).MaximumLength(500);
    }
}
