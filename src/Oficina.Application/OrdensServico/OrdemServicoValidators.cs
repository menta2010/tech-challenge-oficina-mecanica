using FluentValidation;

namespace Oficina.Application.OrdensServico;

/// <summary>
/// Validacao de borda. LojaId e ClienteId podem vir vazios: o caso de uso completa com o
/// contexto do token (loja do atendente / cliente autenticado pelo CPF) e valida o resultado.
/// </summary>
public sealed class CriarOrdemServicoValidator : AbstractValidator<CriarOrdemServicoRequest>
{
    public CriarOrdemServicoValidator()
    {
        RuleFor(x => x.VeiculoId).NotEmpty();
        RuleForEach(x => x.Servicos).SetValidator(new AdicionarServicoValidator());
        RuleForEach(x => x.Pecas).SetValidator(new AdicionarPecaValidator());
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
