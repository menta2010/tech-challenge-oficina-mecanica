using FluentValidation;
using Oficina.Application.Abstractions;
using Oficina.Application.Common;
using Oficina.Domain.Estoque;

namespace Oficina.Application.Estoque;

/// <summary>Saldo de pecas/insumos por loja: consulta e reabastecimento. A baixa acontece no uso da peca na OS.</summary>
public sealed class EstoqueService
{
    private readonly IEstoqueLojaRepository _estoques;
    private readonly ILojaRepository _lojas;
    private readonly IPecaInsumoRepository _pecas;
    private readonly IUsuarioAtual _usuario;
    private readonly IUnitOfWork _uow;
    private readonly IValidator<ReabastecerEstoqueRequest> _reabastecerValidator;

    public EstoqueService(IEstoqueLojaRepository estoques, ILojaRepository lojas, IPecaInsumoRepository pecas,
        IUsuarioAtual usuario, IUnitOfWork uow, IValidator<ReabastecerEstoqueRequest> reabastecerValidator)
    {
        _estoques = estoques;
        _lojas = lojas;
        _pecas = pecas;
        _usuario = usuario;
        _uow = uow;
        _reabastecerValidator = reabastecerValidator;
    }

    public async Task<IReadOnlyList<EstoqueLojaResponse>> ListarAsync(Guid lojaId, CancellationToken ct = default)
    {
        await GarantirAcessoALojaAsync(lojaId, ct);
        var nomes = (await _pecas.ListAsync(ct)).ToDictionary(p => p.Id, p => p.Nome);
        return (await _estoques.ListarPorLojaAsync(lojaId, ct))
            .Select(e => Map(e, nomes.GetValueOrDefault(e.PecaInsumoId, "?")))
            .ToList();
    }

    /// <summary>Entrada de pecas na loja. Cria o registro de saldo na primeira entrada.</summary>
    public async Task<EstoqueLojaResponse> ReabastecerAsync(Guid lojaId, Guid pecaId, ReabastecerEstoqueRequest req, CancellationToken ct = default)
    {
        await _reabastecerValidator.ValidateAndThrowAsync(req, ct);
        await GarantirAcessoALojaAsync(lojaId, ct);
        var peca = await _pecas.ObterOuFalharAsync(pecaId, "Peca/insumo nao encontrado.", ct);

        var estoque = await _estoques.ObterAsync(lojaId, pecaId, ct);
        if (estoque is null)
        {
            estoque = new EstoqueLoja(lojaId, pecaId);
            await _estoques.AddAsync(estoque, ct);
        }

        estoque.Repor(req.Quantidade);
        await _uow.SaveChangesAsync(ct);
        return Map(estoque, peca.Nome);
    }

    private async Task GarantirAcessoALojaAsync(Guid lojaId, CancellationToken ct)
    {
        await _lojas.ObterOuFalharAsync(lojaId, "Loja nao encontrada.", ct);
        if (_usuario.EhAtendente && _usuario.LojaId != lojaId)
            throw new ForbiddenException("Atendente so acessa o estoque da propria loja.");
    }

    private static EstoqueLojaResponse Map(EstoqueLoja e, string peca) => new(e.LojaId, e.PecaInsumoId, peca, e.Quantidade);
}
