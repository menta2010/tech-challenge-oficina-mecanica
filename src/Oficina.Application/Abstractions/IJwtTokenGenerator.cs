using Oficina.Domain.Identidade;

namespace Oficina.Application.Abstractions;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiraEm) Generate(Usuario usuario);
}
