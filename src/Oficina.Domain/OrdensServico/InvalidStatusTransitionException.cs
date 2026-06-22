using Oficina.Domain.Shared;

namespace Oficina.Domain.OrdensServico;

/// <summary>Lancada quando uma acao e tentada num status que nao a permite.</summary>
public sealed class InvalidStatusTransitionException : DomainException
{
    public InvalidStatusTransitionException(string acao, StatusOS atual, StatusOS esperado)
        : base($"Nao e possivel '{acao}': status atual '{atual}', esperado '{esperado}'.") { }
}
