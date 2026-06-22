using FluentValidation;

namespace Oficina.Application.Estoque;

public sealed class CreatePecaInsumoValidator : AbstractValidator<CreatePecaInsumoRequest>
{
    public CreatePecaInsumoValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(160);
        RuleFor(x => x.ValorUnitario).GreaterThanOrEqualTo(0);
        RuleFor(x => x.QuantidadeEmEstoque).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdatePecaInsumoValidator : AbstractValidator<UpdatePecaInsumoRequest>
{
    public UpdatePecaInsumoValidator()
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
