using FluentValidation;

namespace Oficina.Application.Clientes;

// Validacao de borda: campos obrigatorios/tamanho. A correcao do CPF/CNPJ
// e garantida pelo Value Object Documento no dominio.
public sealed class CreateClienteValidator : AbstractValidator<CreateClienteRequest>
{
    public CreateClienteValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Documento).NotEmpty();
        RuleFor(x => x.Email).MaximumLength(200);
        RuleFor(x => x.Telefone).MaximumLength(40);
    }
}

public sealed class UpdateClienteValidator : AbstractValidator<UpdateClienteRequest>
{
    public UpdateClienteValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
    }
}
