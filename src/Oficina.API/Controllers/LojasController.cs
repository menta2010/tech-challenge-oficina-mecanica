using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Application.Estoque;
using Oficina.Application.Lojas;
using Oficina.Domain.Identidade;

namespace Oficina.API.Controllers;

/// <summary>Unidades da rede e o estoque de cada uma. Cadastro: admin. Consulta e estoque: funcionarios.</summary>
[ApiController]
[Authorize(Roles = Perfis.Funcionarios)]
[Route("api/lojas")]
public class LojasController : ControllerBase
{
    private readonly LojaService _lojas;
    private readonly EstoqueService _estoque;

    public LojasController(LojaService lojas, EstoqueService estoque)
    {
        _lojas = lojas;
        _estoque = estoque;
    }

    [HttpPost]
    [Authorize(Roles = Perfis.Admin)]
    public async Task<IActionResult> Criar(CriarLojaRequest req, CancellationToken ct)
    {
        var r = await _lojas.CriarAsync(req, ct);
        return CreatedAtAction(nameof(Obter), new { id = r.Id }, r);
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) => Ok(await _lojas.ListarAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obter(Guid id, CancellationToken ct) => Ok(await _lojas.ObterAsync(id, ct));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Perfis.Admin)]
    public async Task<IActionResult> Atualizar(Guid id, AtualizarLojaRequest req, CancellationToken ct)
        => Ok(await _lojas.AtualizarAsync(id, req, ct));

    [HttpPost("{id:guid}/desativar")]
    [Authorize(Roles = Perfis.Admin)]
    public async Task<IActionResult> Desativar(Guid id, CancellationToken ct)
        => Ok(await _lojas.DesativarAsync(id, ct));

    /// <summary>Saldo de pecas/insumos da loja (atendente: apenas a propria loja).</summary>
    [HttpGet("{id:guid}/estoque")]
    public async Task<IActionResult> Estoque(Guid id, CancellationToken ct)
        => Ok(await _estoque.ListarAsync(id, ct));

    /// <summary>Entrada de pecas/insumos no estoque da loja.</summary>
    [HttpPost("{id:guid}/estoque/{pecaId:guid}/reabastecer")]
    public async Task<IActionResult> Reabastecer(Guid id, Guid pecaId, ReabastecerEstoqueRequest req, CancellationToken ct)
        => Ok(await _estoque.ReabastecerAsync(id, pecaId, req, ct));
}
