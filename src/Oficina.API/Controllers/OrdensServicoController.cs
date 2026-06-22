using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Application.OrdensServico;
using Oficina.Domain.OrdensServico;

namespace Oficina.API.Controllers;

/// <summary>
/// Fluxos da Ordem de Servico (nucleo). Endpoints administrativos protegidos por JWT.
/// Cada acao corresponde a uma transicao da maquina de estados da OS.
/// </summary>
[ApiController]
[Authorize]
[Route("api/ordens-servico")]
public class OrdensServicoController : ControllerBase
{
    private readonly OrdemServicoService _service;
    public OrdensServicoController(OrdemServicoService service) => _service = service;

    /// <summary>Cria a OS para um cliente/veiculo identificados (status inicial: Recebida).</summary>
    [HttpPost]
    public async Task<IActionResult> Criar(CriarOrdemServicoRequest req, CancellationToken ct)
    {
        var r = await _service.CriarAsync(req, ct);
        return CreatedAtAction(nameof(Obter), new { id = r.Id }, r);
    }

    /// <summary>Lista OSs (opcionalmente por status). Atende a listagem de ordens de servico.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] StatusOS? status, CancellationToken ct)
        => Ok(await _service.ListarAsync(status, ct));

    /// <summary>Detalha uma OS (itens, orcamento, datas).</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obter(Guid id, CancellationToken ct) => Ok(await _service.ObterAsync(id, ct));

    // Diagnostico
    [HttpPost("{id:guid}/iniciar-diagnostico")]
    public async Task<IActionResult> IniciarDiagnostico(Guid id, CancellationToken ct)
        => Ok(await _service.IniciarDiagnosticoAsync(id, ct));

    [HttpPost("{id:guid}/servicos")]
    public async Task<IActionResult> AdicionarServico(Guid id, AdicionarServicoRequest req, CancellationToken ct)
        => Ok(await _service.AdicionarServicoAsync(id, req, ct));

    [HttpDelete("{id:guid}/servicos/{itemId:guid}")]
    public async Task<IActionResult> RemoverServico(Guid id, Guid itemId, CancellationToken ct)
        => Ok(await _service.RemoverServicoAsync(id, itemId, ct));

    [HttpPost("{id:guid}/pecas")]
    public async Task<IActionResult> AdicionarPeca(Guid id, AdicionarPecaRequest req, CancellationToken ct)
        => Ok(await _service.AdicionarPecaAsync(id, req, ct));

    [HttpDelete("{id:guid}/pecas/{itemId:guid}")]
    public async Task<IActionResult> RemoverPeca(Guid id, Guid itemId, CancellationToken ct)
        => Ok(await _service.RemoverPecaAsync(id, itemId, ct));

    /// <summary>Finaliza o diagnostico e gera o orcamento (status: Aguardando aprovacao).</summary>
    [HttpPost("{id:guid}/finalizar-diagnostico")]
    public async Task<IActionResult> FinalizarDiagnostico(Guid id, CancellationToken ct)
        => Ok(await _service.FinalizarDiagnosticoAsync(id, ct));

    // Orcamento
    [HttpPost("{id:guid}/aprovar")]
    public async Task<IActionResult> Aprovar(Guid id, CancellationToken ct)
        => Ok(await _service.AprovarAsync(id, ct));

    [HttpPost("{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id, CancellationToken ct)
        => Ok(await _service.CancelarAsync(id, ct));

    // Execucao
    [HttpPost("{id:guid}/servicos/{itemId:guid}/executar")]
    public async Task<IActionResult> ExecutarServico(Guid id, Guid itemId, CancellationToken ct)
        => Ok(await _service.ExecutarServicoAsync(id, itemId, ct));

    /// <summary>Registra o uso de uma peca e baixa o estoque.</summary>
    [HttpPost("{id:guid}/pecas/{itemId:guid}/usar")]
    public async Task<IActionResult> UsarPeca(Guid id, Guid itemId, CancellationToken ct)
        => Ok(await _service.UsarPecaAsync(id, itemId, ct));

    [HttpPost("{id:guid}/finalizar-execucao")]
    public async Task<IActionResult> FinalizarExecucao(Guid id, CancellationToken ct)
        => Ok(await _service.FinalizarExecucaoAsync(id, ct));

    // Entrega
    [HttpPost("{id:guid}/entregar")]
    public async Task<IActionResult> Entregar(Guid id, CancellationToken ct)
        => Ok(await _service.EntregarAsync(id, ct));

    /// <summary>Relatorio gerencial: tempo medio de execucao das OSs finalizadas/entregues.</summary>
    [HttpGet("relatorios/tempo-medio")]
    public async Task<IActionResult> TempoMedio(CancellationToken ct)
        => Ok(await _service.TempoMedioExecucaoAsync(ct));
}
