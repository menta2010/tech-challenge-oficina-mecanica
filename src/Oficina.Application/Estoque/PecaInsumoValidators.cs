using FluentValidation;

namespace Oficina.Application.Estoque;

public sealed class CriarPecaInsumoValidator : AbstractValidator<CriarPecaInsumoRequest>
{
    public CriarPecaInsumoValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(160);
        RuleFor(x => x.ValorUnitario).GreaterThanOrEqualTo(0);
    }
}

public sealed class AtualizarPecaInsumoValidator : AbstractValidator<AtualizarPecaInsumoRequest>
{
    public AtualizarPecaInsumoValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(160);
        RuleFor(x => x.ValorUnitario).GreaterThanOrEqualTo(0);
    }
}

public sealed class ReabastecerEstoqueValidator : AbstractValidator<ReabastecerEstoqueRequest>
{
    public ReabastecerEstoqueValidator()
    {
        RuleFor(x => x.Quantidade).GreaterThan(0);
    }
}
