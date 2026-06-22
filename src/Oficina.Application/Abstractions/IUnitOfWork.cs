namespace Oficina.Application.Abstractions;

/// <summary>Confirma as mudancas de uma unidade de trabalho (transacao logica do caso de uso).</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
