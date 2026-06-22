using FluentValidation;

namespace Oficina.Application.OrdensServico;

public sealed class CriarOrdemServicoValidator : AbstractValidator<CriarOrdemServicoRequest>
{
    public CriarOrdemServicoValidator()
    {
        RuleFor(x => x.ClienteId).NotEmpty();
        RuleFor(x => x.VeiculoId).NotEmpty();
    }
}

public sealed class AdicionarServicoValidator : AbstractValidator<AdicionarServicoRequest>
{
    public AdicionarServicoValidator() => RuleFor(x => x.ServicoId).NotEmpty();
}

public sealed class AdicionarPecaValidator : AbstractValidator<AdicionarPecaRequest>
{
    public AdicionarPecaValidator()
    {
        RuleFor(x => x.PecaId).NotEmpty();
        RuleFor(x => x.Quantidade).GreaterThan(0);
    }
}
