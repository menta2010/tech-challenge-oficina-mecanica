using Microsoft.EntityFrameworkCore;
using Oficina.Application.Abstractions;
using Oficina.Domain.Estoque;
using Oficina.Domain.Lojas;
using Oficina.Domain.OrdensServico;

namespace Oficina.Infrastructure.Persistence.Repositories;

public sealed class OrdemServicoRepository : EfRepository<OrdemServico>, IOrdemServicoRepository
{
    public OrdemServicoRepository(OficinaDbContext db) : base(db) { }

    private IQueryable<OrdemServico> Completas()
        => Db.OrdensServico
            .Include(o => o.Servicos)
            .Include(o => o.Pecas)
            .Include(o => o.Historico)
            .AsSplitQuery();

    public override Task<OrdemServico?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Completas().FirstOrDefaultAsync(o => o.Id == id, ct);

    public override Task<List<OrdemServico>> ListAsync(CancellationToken ct = default)
        => Completas().OrderByDescending(o => o.CriadaEm).ToListAsync(ct);

    public Task<List<OrdemServico>> ListarAsync(FiltroOrdensServico filtro, CancellationToken ct = default)
    {
        var query = Completas();

        // Exclusao logica: por padrao so as OS nao encerradas (coluna indexada "Encerrada")
        if (!filtro.IncluirEncerradas) query = query.Where(o => !o.Encerrada);
        if (filtro.Status is { } status) query = query.Where(o => o.Status == status);
        if (filtro.LojaId is { } lojaId) query = query.Where(o => o.LojaId == lojaId);
        if (filtro.ClienteId is { } clienteId) query = query.Where(o => o.ClienteId == clienteId);

        return query.ToListAsync(ct);
    }

    public Task<List<OrdemServico>> ListarComExecucaoConcluidaAsync(Guid? lojaId, CancellationToken ct = default)
    {
        var query = Db.OrdensServico
            .Include(o => o.Historico)
            .Where(o => o.Status == StatusOS.Finalizada || o.Status == StatusOS.Entregue);
        if (lojaId is { } loja) query = query.Where(o => o.LojaId == loja);
        return query.ToListAsync(ct);
    }
}

public sealed class LojaRepository : EfRepository<Loja>, ILojaRepository
{
    public LojaRepository(OficinaDbContext db) : base(db) { }

    public override Task<List<Loja>> ListAsync(CancellationToken ct = default)
        => Db.Lojas.OrderBy(l => l.Nome).ToListAsync(ct);
}

public sealed class EstoqueLojaRepository : EfRepository<EstoqueLoja>, IEstoqueLojaRepository
{
    public EstoqueLojaRepository(OficinaDbContext db) : base(db) { }

    public Task<EstoqueLoja?> ObterAsync(Guid lojaId, Guid pecaInsumoId, CancellationToken ct = default)
        => Db.EstoquesLoja.FirstOrDefaultAsync(e => e.LojaId == lojaId && e.PecaInsumoId == pecaInsumoId, ct);

    public Task<List<EstoqueLoja>> ListarPorLojaAsync(Guid lojaId, CancellationToken ct = default)
        => Db.EstoquesLoja.Where(e => e.LojaId == lojaId).ToListAsync(ct);
}
