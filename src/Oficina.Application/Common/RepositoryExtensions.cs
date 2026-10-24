using Oficina.Application.Abstractions;
using Oficina.Domain.Shared;

namespace Oficina.Application.Common;

/// <summary>
/// Centraliza o padrao "buscar ou responder 404", antes repetido em todos os services
/// (ponto de atencao da correcao da Fase 2: duplicacao de codigo).
/// </summary>
public static class RepositoryExtensions
{
    public static async Task<T> ObterOuFalharAsync<T>(
        this IRepository<T> repo, Guid id, string mensagemNaoEncontrado, CancellationToken ct = default)
        where T : Entity
        => await repo.GetByIdAsync(id, ct) ?? throw new NotFoundException(mensagemNaoEncontrado);
}
