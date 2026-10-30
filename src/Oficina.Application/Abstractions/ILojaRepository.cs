using Oficina.Domain.Estoque;
using Oficina.Domain.Lojas;

namespace Oficina.Application.Abstractions;

public interface ILojaRepository : IRepository<Loja> { }

public interface IEstoqueLojaRepository : IRepository<EstoqueLoja>
{
    Task<EstoqueLoja?> ObterAsync(Guid lojaId, Guid pecaInsumoId, CancellationToken ct = default);
    Task<List<EstoqueLoja>> ListarPorLojaAsync(Guid lojaId, CancellationToken ct = default);
}
