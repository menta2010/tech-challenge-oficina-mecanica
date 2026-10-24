using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Oficina.API.Seguranca;
using Oficina.Application.OrdensServico;
using Oficina.Domain.Identidade;
using Oficina.Domain.OrdensServico;

namespace Oficina.API.Controllers;

/// <summary>
/// Fluxos da Ordem de Servico (nucleo). Cada acao corresponde a uma transicao da maquina de estados.
/// Abertura e consulta: funcionarios e cliente autenticado pelo CPF (apenas as proprias OS).
/// Transicoes operacionais: somente funcionarios (atendente restrito a propria loja).
/// </summary>
// [Authorize] na classe exige apenas autenticacao; o perfil e definido por acao
// (atributos de role na classe e no metodo se somam, por isso nao ficam na classe).
[ApiController]
[Authorize]
[Route("api/ordens-servico")]
public class OrdensServicoController : ControllerBase
{
    private const string FuncionariosOuCliente = Perfis.Funcionarios + "," + Perfis.Cliente;

    private readonly OrdemServicoService _service;
    private readonly WebhookOptions _webhook;

    public OrdensServicoController(OrdemServicoService service, IOptions<WebhookOptions> webhook)
    {
        _service = service;
        _webhook = webhook.Value;
    }

    /// <summary>Abre a OS (status inicial: Recebida). O cliente autenticado so abre OS para si.</summary>
    [HttpPost]
    [Authorize(Roles = FuncionariosOuCliente)]
    public async Task<IActionResult> Criar(CriarOrdemServicoRequest req, CancellationToken ct)
    {
        var r = await _service.CriarAsync(req, ct);
        return CreatedAtAction(nameof(Obter), new { id = r.Id }, r);
    }

    /// <summary>
    /// Lista as OS ativas (exclusao logica das Finalizadas/Entregues/Canceladas), ordenadas por
    /// prioridade de status e, dentro do status, das mais antigas para as mais novas.
    /// incluirEncerradas=true traz tambem as encerradas, ao final.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = FuncionariosOuCliente)]
    public async Task<IActionResult> Listar(
        [FromQuery] StatusOS? status, [FromQuery] bool incluirEncerradas, [FromQuery] Guid? lojaId, CancellationToken ct)
        => Ok(await _service.ListarAsync(status, incluirEncerradas, lojaId, ct));

    /// <summary>Detalha uma OS (itens, orcamento, historico de status).</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = FuncionariosOuCliente)]
    public async Task<IActionResult> Obter(Guid id, CancellationToken ct) => Ok(await _service.ObterAsync(id, ct));

    // Diagnostico
    [Authorize(Roles = Perfis.Funcionarios)]
    [HttpPost("{id:guid}/iniciar-diagnostico")]
    public async Task<IActionResult> IniciarDiagnostico(Guid id, CancellationToken ct)
        => Ok(await _service.IniciarDiagnosticoAsync(id, ct));

    [Authorize(Roles = Perfis.Funcionarios)]
    [HttpPost("{id:guid}/servicos")]
    public async Task<IActionResult> AdicionarServico(Guid id, AdicionarServicoRequest req, CancellationToken ct)
        => Ok(await _service.AdicionarServicoAsync(id, req, ct));

    [Authorize(Roles = Perfis.Funcionarios)]
    [HttpDelete("{id:guid}/servicos/{itemId:guid}")]
    public async Task<IActionResult> RemoverServico(Guid id, Guid itemId, CancellationToken ct)
        => Ok(await _service.RemoverServicoAsync(id, itemId, ct));

    [Authorize(Roles = Perfis.Funcionarios)]
    [HttpPost("{id:guid}/pecas")]
    public async Task<IActionResult> AdicionarPeca(Guid id, AdicionarPecaRequest req, CancellationToken ct)
        => Ok(await _service.AdicionarPecaAsync(id, req, ct));

    [Authorize(Roles = Perfis.Funcionarios)]
    [HttpDelete("{id:guid}/pecas/{itemId:guid}")]
    public async Task<IActionResult> RemoverPeca(Guid id, Guid itemId, CancellationToken ct)
        => Ok(await _service.RemoverPecaAsync(id, itemId, ct));

    /// <summary>Finaliza o diagnostico e gera o orcamento (status: Aguardando aprovacao).</summary>
    [Authorize(Roles = Perfis.Funcionarios)]
    [HttpPost("{id:guid}/finalizar-diagnostico")]
    public async Task<IActionResult> FinalizarDiagnostico(Guid id, CancellationToken ct)
        => Ok(await _service.FinalizarDiagnosticoAsync(id, ct));

    // Orcamento
    [Authorize(Roles = Perfis.Funcionarios)]
    [HttpPost("{id:guid}/aprovar")]
    public async Task<IActionResult> Aprovar(Guid id, CancellationToken ct)
        => Ok(await _service.AprovarAsync(id, ct));

    [Authorize(Roles = Perfis.Funcionarios)]
    [HttpPost("{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id, CancellationToken ct)
        => Ok(await _service.CancelarAsync(id, ct));

    /// <summary>
    /// Webhook para o cliente aprovar/recusar o orcamento por notificacao externa.
    /// Protegido por token de servico (header X-Webhook-Token), injetado via Secret no Kubernetes.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("{id:guid}/orcamento/resposta")]
    public async Task<IActionResult> ResponderOrcamento(
        Guid id, RespostaOrcamentoRequest req,
        [FromHeader(Name = "X-Webhook-Token")] string? token, CancellationToken ct)
    {
        if (!_webhook.TokenValido(token))
            throw new Application.Common.UnauthorizedException("Token de webhook invalido.");

        return Ok(await _service.ResponderOrcamentoAsync(id, req, ct));
    }

    // Execucao
    [Authorize(Roles = Perfis.Funcionarios)]
    [HttpPost("{id:guid}/servicos/{itemId:guid}/executar")]
    public async Task<IActionResult> ExecutarServico(Guid id, Guid itemId, CancellationToken ct)
        => Ok(await _service.ExecutarServicoAsync(id, itemId, ct));

    /// <summary>Registra o uso de uma peca e baixa o estoque da loja da OS.</summary>
    [Authorize(Roles = Perfis.Funcionarios)]
    [HttpPost("{id:guid}/pecas/{itemId:guid}/usar")]
    public async Task<IActionResult> UsarPeca(Guid id, Guid itemId, CancellationToken ct)
        => Ok(await _service.UsarPecaAsync(id, itemId, ct));

    [Authorize(Roles = Perfis.Funcionarios)]
    [HttpPost("{id:guid}/finalizar-execucao")]
    public async Task<IActionResult> FinalizarExecucao(Guid id, CancellationToken ct)
        => Ok(await _service.FinalizarExecucaoAsync(id, ct));

    // Entrega
    [Authorize(Roles = Perfis.Funcionarios)]
    [HttpPost("{id:guid}/entregar")]
    public async Task<IActionResult> Entregar(Guid id, CancellationToken ct)
        => Ok(await _service.EntregarAsync(id, ct));

    /// <summary>Relatorio gerencial: tempo medio de execucao das OS finalizadas/entregues.</summary>
    [Authorize(Roles = Perfis.Funcionarios)]
    [HttpGet("relatorios/tempo-medio")]
    public async Task<IActionResult> TempoMedio(CancellationToken ct)
        => Ok(await _service.TempoMedioExecucaoAsync(ct));

    /// <summary>Relatorio gerencial: tempo medio em cada status (Diagnostico, Execucao, Finalizacao...).</summary>
    [Authorize(Roles = Perfis.Funcionarios)]
    [HttpGet("relatorios/tempo-por-status")]
    public async Task<IActionResult> TempoPorStatus(CancellationToken ct)
        => Ok(await _service.TempoMedioPorStatusAsync(ct));
}
