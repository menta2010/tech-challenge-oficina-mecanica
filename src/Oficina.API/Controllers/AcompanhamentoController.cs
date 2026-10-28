using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Application.OrdensServico;
using Oficina.Domain.Identidade;

namespace Oficina.API.Controllers;

/// <summary>
/// Consulta de andamento da OS pelo cliente. Rota sensivel: exige o token emitido pela
/// autenticacao por CPF (o cliente so ve as proprias OS) ou um token de funcionario.
/// </summary>
[ApiController]
[Authorize(Roles = Perfis.Funcionarios + "," + Perfis.Cliente)]
[Route("api/acompanhamento")]
public class AcompanhamentoController : ControllerBase
{
    private readonly OrdemServicoService _service;
    public AcompanhamentoController(OrdemServicoService service) => _service = service;

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Consultar(Guid id, CancellationToken ct)
        => Ok(await _service.AcompanharAsync(id, ct));

    /// <summary>OS ativas do cliente autenticado (ou filtradas pelo escopo do funcionario).</summary>
    [HttpGet]
    public async Task<IActionResult> MinhasOrdens(CancellationToken ct)
        => Ok(await _service.ListarAsync(status: null, ct: ct));
}
