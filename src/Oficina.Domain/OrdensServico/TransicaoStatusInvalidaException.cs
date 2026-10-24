using Oficina.Domain.Shared;

namespace Oficina.Domain.OrdensServico;

/// <summary>Lancada quando uma acao e tentada num status que nao a permite.</summary>
public sealed class TransicaoStatusInvalidaException : DomainException
{
    public TransicaoStatusInvalidaException(string acao, StatusOS atual, StatusOS esperado)
        : base($"Nao e possivel '{acao}': status atual '{atual}', esperado '{esperado}'.") { }
}
