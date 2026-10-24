using FluentValidation;

namespace Oficina.Application.Clientes;

// Validacao de borda: campos obrigatorios/tamanho. A correcao do CPF/CNPJ
// e garantida pelo Value Object Documento no dominio.
public sealed class CriarClienteValidator : AbstractValidator<CriarClienteRequest>
{
    public CriarClienteValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Documento).NotEmpty();
        RuleFor(x => x.Email).MaximumLength(200);
        RuleFor(x => x.Telefone).MaximumLength(40);
    }
}

public sealed class AtualizarClienteValidator : AbstractValidator<AtualizarClienteRequest>
{
    public AtualizarClienteValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
    }
}
