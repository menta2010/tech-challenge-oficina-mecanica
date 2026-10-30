using FluentValidation;
using Oficina.Application.Abstractions;
using Oficina.Application.Common;
using Oficina.Domain.OrdensServico;

namespace Oficina.Application.OrdensServico;

/// <summary>
/// Casos de uso do nucleo do dominio: ciclo de vida da OS.
/// Orquestra os agregados OrdemServico, Servico, EstoqueLoja (baixa), Cliente e Veiculo;
/// aplica o escopo de acesso (loja do atendente / OS do proprio cliente);
/// e, a cada mudanca de status, notifica o cliente e registra metricas de negocio.
/// </summary>
public sealed class OrdemServicoService
{
    private const string OsNaoEncontrada = "Ordem de servico nao encontrada.";

    // Prioridade de exibicao na listagem: Em Execucao > Aguardando Aprovacao > Diagnostico > Recebida.
    private static readonly IReadOnlyDictionary<StatusOS, int> PrioridadeListagem =
        new Dictionary<StatusOS, int>
        {
            [StatusOS.EmExecucao] = 0,
            [StatusOS.AguardandoAprovacao] = 1,
            [StatusOS.EmDiagnostico] = 2,
            [StatusOS.Recebida] = 3,
        };

    private readonly IOrdemServicoRepository _repo;
    private readonly ILojaRepository _lojas;
    private readonly IClienteRepository _clientes;
    private readonly IVeiculoRepository _veiculos;
    private readonly IServicoRepository _servicos;
    private readonly IPecaInsumoRepository _pecas;
    private readonly IEstoqueLojaRepository _estoques;
    private readonly IUnitOfWork _uow;
    private readonly IUsuarioAtual _usuario;
    private readonly INotificadorStatusOS _notificador;
    private readonly IMetricasOrdemServico _metricas;
    private readonly IValidator<CriarOrdemServicoRequest> _criarValidator;
    private readonly IValidator<AdicionarServicoRequest> _addServicoValidator;
    private readonly IValidator<AdicionarPecaRequest> _addPecaValidator;

    public OrdemServicoService(
        IOrdemServicoRepository repo, ILojaRepository lojas, IClienteRepository clientes, IVeiculoRepository veiculos,
        IServicoRepository servicos, IPecaInsumoRepository pecas, IEstoqueLojaRepository estoques, IUnitOfWork uow,
        IUsuarioAtual usuario, INotificadorStatusOS notificador, IMetricasOrdemServico metricas,
        IValidator<CriarOrdemServicoRequest> criarValidator,
        IValidator<AdicionarServicoRequest> addServicoValidator,
        IValidator<AdicionarPecaRequest> addPecaValidator)
    {
        _repo = repo; _lojas = lojas; _clientes = clientes; _veiculos = veiculos; _servicos = servicos;
        _pecas = pecas; _estoques = estoques; _uow = uow; _usuario = usuario; _notificador = notificador;
        _metricas = metricas; _criarValidator = criarValidator;
        _addServicoValidator = addServicoValidator; _addPecaValidator = addPecaValidator;
    }

    // --- Abertura ---
    public async Task<OrdemServicoResponse> CriarAsync(CriarOrdemServicoRequest req, CancellationToken ct = default)
    {
        await _criarValidator.ValidateAndThrowAsync(req, ct);

        var lojaId = ResolverLoja(req.LojaId);
        var clienteId = ResolverCliente(req.ClienteId);

        var loja = await _lojas.ObterOuFalharAsync(lojaId, "Loja nao encontrada.", ct);
        if (!loja.Ativa)
            throw new ConflictException("Loja inativa nao pode abrir ordens de servico.");

        var cliente = await _clientes.ObterOuFalharAsync(clienteId, "Cliente nao encontrado.", ct);
        if (!cliente.Ativo)
            cliente.Reativar(); // politica: cliente inativado por inatividade volta a ser ativo ao abrir nova OS

        var veiculo = await _veiculos.ObterOuFalharAsync(req.VeiculoId, "Veiculo nao encontrado.", ct);
        if (veiculo.ClienteId != clienteId)
            throw new ConflictException("O veiculo informado nao pertence a este cliente.");

        var os = new OrdemServico(lojaId, clienteId, req.VeiculoId);

        // Abertura com itens ja identificados: a OS entra em diagnostico e registra servicos/pecas.
        var temItens = (req.Servicos?.Count ?? 0) > 0 || (req.Pecas?.Count ?? 0) > 0;
        if (temItens)
        {
            os.IniciarDiagnostico();
            foreach (var item in req.Servicos ?? Array.Empty<AdicionarServicoRequest>())
                await RegistrarServicoAsync(os, item.ServicoId, ct);
            foreach (var item in req.Pecas ?? Array.Empty<AdicionarPecaRequest>())
                await RegistrarPecaAsync(os, item, ct);
        }

        await _repo.AddAsync(os, ct);
        await _uow.SaveChangesAsync(ct);

        _metricas.OrdemCriada(os);
        await NotificarAsync(os, cliente.Email, ct);
        return Map(os);
    }

    // --- Diagnostico ---
    public Task<OrdemServicoResponse> IniciarDiagnosticoAsync(Guid id, CancellationToken ct = default)
        => MutarAsync(id, nameof(IniciarDiagnosticoAsync), os => os.IniciarDiagnostico(), ct);

    public async Task<OrdemServicoResponse> AdicionarServicoAsync(Guid id, AdicionarServicoRequest req, CancellationToken ct = default)
    {
        await _addServicoValidator.ValidateAndThrowAsync(req, ct);
        return await MutarAsync(id, nameof(AdicionarServicoAsync), os => RegistrarServicoAsync(os, req.ServicoId, ct), ct);
    }

    public async Task<OrdemServicoResponse> AdicionarPecaAsync(Guid id, AdicionarPecaRequest req, CancellationToken ct = default)
    {
        await _addPecaValidator.ValidateAndThrowAsync(req, ct);
        return await MutarAsync(id, nameof(AdicionarPecaAsync), os => RegistrarPecaAsync(os, req, ct), ct);
    }

    public Task<OrdemServicoResponse> RemoverServicoAsync(Guid id, Guid itemId, CancellationToken ct = default)
        => MutarAsync(id, nameof(RemoverServicoAsync), os => os.RemoverServico(itemId), ct);

    public Task<OrdemServicoResponse> RemoverPecaAsync(Guid id, Guid itemId, CancellationToken ct = default)
        => MutarAsync(id, nameof(RemoverPecaAsync), os => os.RemoverPeca(itemId), ct);

    public Task<OrdemServicoResponse> FinalizarDiagnosticoAsync(Guid id, CancellationToken ct = default)
        => MutarAsync(id, nameof(FinalizarDiagnosticoAsync), os => os.FinalizarDiagnostico(), ct);

    // --- Orcamento ---
    public Task<OrdemServicoResponse> AprovarAsync(Guid id, CancellationToken ct = default)
        => MutarAsync(id, nameof(AprovarAsync), os => os.AprovarOrcamento(), ct);

    public Task<OrdemServicoResponse> CancelarAsync(Guid id, CancellationToken ct = default)
        => MutarAsync(id, nameof(CancelarAsync), os => os.Cancelar(), ct);

    /// <summary>Resposta externa (webhook) do cliente ao orcamento: aprova (Em execucao) ou recusa (Cancelada).</summary>
    public Task<OrdemServicoResponse> ResponderOrcamentoAsync(Guid id, RespostaOrcamentoRequest req, CancellationToken ct = default)
        => MutarAsync(id, nameof(ResponderOrcamentoAsync), os =>
        {
            if (req.Aprovado) os.AprovarOrcamento();
            else os.Cancelar();
        }, ct, verificarAcesso: false); // webhook e autenticado pelo token de servico, nao por usuario

    // --- Execucao ---
    public Task<OrdemServicoResponse> ExecutarServicoAsync(Guid id, Guid itemId, CancellationToken ct = default)
        => MutarAsync(id, nameof(ExecutarServicoAsync), os => os.ExecutarServico(itemId), ct);

    /// <summary>Registra o uso de uma peca e baixa o estoque da loja da OS (cross-agregado).</summary>
    public Task<OrdemServicoResponse> UsarPecaAsync(Guid id, Guid itemId, CancellationToken ct = default)
        => MutarAsync(id, nameof(UsarPecaAsync), async os =>
        {
            var item = os.Pecas.FirstOrDefault(p => p.Id == itemId)
                       ?? throw new NotFoundException("Item de peca nao encontrado na OS.");
            if (item.Utilizado)
                throw new ConflictException("Esta peca ja foi utilizada/baixada.");

            var estoque = await _estoques.ObterAsync(os.LojaId, item.PecaId, ct)
                          ?? throw new ConflictException("A loja da OS nao possui estoque desta peca.");
            estoque.Baixar(item.Quantidade);
            os.RegistrarUsoPeca(itemId);
        }, ct);

    public Task<OrdemServicoResponse> FinalizarExecucaoAsync(Guid id, CancellationToken ct = default)
        => MutarAsync(id, nameof(FinalizarExecucaoAsync), os => os.FinalizarExecucao(), ct);

    // --- Entrega ---
    public Task<OrdemServicoResponse> EntregarAsync(Guid id, CancellationToken ct = default)
        => MutarAsync(id, nameof(EntregarAsync), os => os.EntregarVeiculo(), ct);

    // --- Consultas ---
    public async Task<OrdemServicoResponse> ObterAsync(Guid id, CancellationToken ct = default)
        => Map(await ObterComAcessoAsync(id, ct));

    /// <summary>
    /// Listagem operacional. Por padrao aplica a exclusao logica: traz apenas OS com Encerrada = false
    /// (as Finalizadas, Entregues e Canceladas ficam fora). Ordena por status
    /// (Em Execucao > Aguardando Aprovacao > Diagnostico > Recebida) e, no mesmo status, das mais antigas
    /// para as mais novas. incluirEncerradas = true traz tambem as encerradas, ao final da lista.
    /// O escopo e limitado pelo token: atendente ve a propria loja; cliente ve as proprias OS.
    /// </summary>
    public async Task<IReadOnlyList<OrdemServicoResponse>> ListarAsync(
        StatusOS? status, bool incluirEncerradas = false, Guid? lojaId = null, CancellationToken ct = default)
    {
        var filtro = new FiltroOrdensServico(
            Status: status,
            IncluirEncerradas: incluirEncerradas || (status is not null && OrdemServico.StatusTerminais.Contains(status.Value)),
            LojaId: _usuario.EhAtendente ? _usuario.LojaId : lojaId,
            ClienteId: _usuario.EhCliente ? _usuario.ClienteId : null);

        return (await _repo.ListarAsync(filtro, ct))
            .OrderBy(o => o.Encerrada)
            .ThenBy(o => PrioridadeListagem.TryGetValue(o.Status, out var p) ? p : int.MaxValue)
            .ThenBy(o => o.CriadaEm)
            .Select(Map)
            .ToList();
    }

    /// <summary>Consulta de andamento pelo cliente (nao altera estado).</summary>
    public async Task<AcompanhamentoResponse> AcompanharAsync(Guid id, CancellationToken ct = default)
    {
        var os = await ObterComAcessoAsync(id, ct);
        return new AcompanhamentoResponse(os.Id, os.Status.ToString(), os.Orcamento?.PrevisaoEntrega, os.CriadaEm);
    }

    /// <summary>Tempo medio de execucao (entre inicio e fim) das OS finalizadas/entregues.</summary>
    public async Task<TempoMedioExecucaoResponse> TempoMedioExecucaoAsync(CancellationToken ct = default)
    {
        var tempos = (await _repo.ListarComExecucaoConcluidaAsync(LojaDoEscopo(), ct))
            .Where(o => o.TempoExecucao.HasValue)
            .Select(o => o.TempoExecucao!.Value.TotalMinutes)
            .ToList();
        return tempos.Count == 0
            ? new TempoMedioExecucaoResponse(0, 0)
            : new TempoMedioExecucaoResponse(tempos.Count, Math.Round(tempos.Average(), 2));
    }

    /// <summary>Tempo medio em cada status (Diagnostico, Execucao, Finalizacao...), a partir do historico.</summary>
    public async Task<IReadOnlyList<TempoMedioStatusResponse>> TempoMedioPorStatusAsync(CancellationToken ct = default)
        => (await _repo.ListarComExecucaoConcluidaAsync(LojaDoEscopo(), ct))
            .SelectMany(o => o.Historico)
            .Where(h => h.Duracao.HasValue)
            .GroupBy(h => h.Status)
            .OrderBy(g => g.Key)
            .Select(g => new TempoMedioStatusResponse(
                g.Key.ToString(), g.Count(), Math.Round(g.Average(h => h.Duracao!.Value.TotalMinutes), 2)))
            .ToList();

    // --- internos ---

    /// <summary>
    /// Fluxo comum de toda alteracao: carrega com verificacao de acesso, aplica a acao, persiste,
    /// e se o status mudou, registra metrica e notifica. Falhas sao registradas antes de propagar.
    /// </summary>
    private async Task<OrdemServicoResponse> MutarAsync(
        Guid id, string operacao, Func<OrdemServico, Task> acao, CancellationToken ct, bool verificarAcesso = true)
    {
        try
        {
            var os = verificarAcesso
                ? await ObterComAcessoAsync(id, ct)
                : await _repo.ObterOuFalharAsync(id, OsNaoEncontrada, ct);
            var statusAntes = os.Status;

            await acao(os);
            await _uow.SaveChangesAsync(ct);

            if (os.Status != statusAntes)
            {
                if (os.UltimoPeriodoConcluido() is { } periodo)
                    _metricas.StatusAlterado(os, periodo);
                var cliente = await _clientes.GetByIdAsync(os.ClienteId, ct);
                await NotificarAsync(os, cliente?.Email, ct);
            }
            return Map(os);
        }
        catch (Exception ex)
        {
            _metricas.FalhaProcessamento(id, operacao, ex);
            throw;
        }
    }

    private Task<OrdemServicoResponse> MutarAsync(
        Guid id, string operacao, Action<OrdemServico> acao, CancellationToken ct, bool verificarAcesso = true)
        => MutarAsync(id, operacao, os => { acao(os); return Task.CompletedTask; }, ct, verificarAcesso);

    private async Task<OrdemServico> ObterComAcessoAsync(Guid id, CancellationToken ct)
    {
        var os = await _repo.ObterOuFalharAsync(id, OsNaoEncontrada, ct);
        if (_usuario.EhAtendente && os.LojaId != _usuario.LojaId)
            throw new ForbiddenException("Esta OS pertence a outra loja.");
        if (_usuario.EhCliente && os.ClienteId != _usuario.ClienteId)
            throw new ForbiddenException("Esta OS pertence a outro cliente.");
        return os;
    }

    private Guid ResolverLoja(Guid? informada)
    {
        if (_usuario.EhAtendente)
        {
            if (informada is { } l && l != Guid.Empty && l != _usuario.LojaId)
                throw new ForbiddenException("Atendente so abre OS na propria loja.");
            return _usuario.LojaId!.Value;
        }
        return informada is { } loja && loja != Guid.Empty
            ? loja
            : throw new ValidationException("Informe a loja (LojaId) da ordem de servico.");
    }

    private Guid ResolverCliente(Guid? informado)
    {
        if (_usuario.EhCliente)
        {
            if (informado is { } c && c != Guid.Empty && c != _usuario.ClienteId)
                throw new ForbiddenException("Cliente so abre OS para si mesmo.");
            return _usuario.ClienteId!.Value;
        }
        return informado is { } cliente && cliente != Guid.Empty
            ? cliente
            : throw new ValidationException("Informe o cliente (ClienteId) da ordem de servico.");
    }

    private Guid? LojaDoEscopo() => _usuario.EhAtendente ? _usuario.LojaId : null;

    private async Task RegistrarServicoAsync(OrdemServico os, Guid servicoId, CancellationToken ct)
    {
        var servico = await _servicos.ObterOuFalharAsync(servicoId, "Servico nao encontrado no catalogo.", ct);
        os.RegistrarServico(servico.Id, servico.Nome, servico.ValorBase, servico.TempoEstimado);
    }

    private async Task RegistrarPecaAsync(OrdemServico os, AdicionarPecaRequest item, CancellationToken ct)
    {
        var peca = await _pecas.ObterOuFalharAsync(item.PecaId, "Peca/insumo nao encontrado.", ct);
        os.RegistrarPeca(peca.Id, item.Quantidade, peca.ValorUnitario);
    }

    private Task NotificarAsync(OrdemServico os, string? email, CancellationToken ct)
        => _notificador.NotificarAsync(
            new StatusOSAlterado(os.Id, os.LojaId, os.ClienteId, email, os.Status.ToString(), DateTime.UtcNow), ct);

    private static OrdemServicoResponse Map(OrdemServico os) => new(
        os.Id, os.LojaId, os.ClienteId, os.VeiculoId, os.Status.ToString(), os.Encerrada,
        os.Servicos.Select(s => new ItemServicoResponse(s.Id, s.ServicoId, s.Descricao, s.Valor.Valor, s.Executado)).ToList(),
        os.Pecas.Select(p => new ItemPecaResponse(p.Id, p.PecaId, p.Quantidade, p.ValorUnitario.Valor, p.Subtotal.Valor, p.Utilizado)).ToList(),
        os.Orcamento is null ? null : new OrcamentoResponse(
            os.Orcamento.ValorServicos.Valor, os.Orcamento.ValorPecas.Valor, os.Orcamento.ValorTotal.Valor,
            os.Orcamento.PrevisaoEntrega, os.Orcamento.GeradoEm),
        os.Historico.OrderBy(h => h.Inicio)
            .Select(h => new HistoricoStatusResponse(h.Status.ToString(), h.Inicio, h.Fim, h.Duracao?.TotalMinutes))
            .ToList(),
        os.CriadaEm, os.ExecucaoIniciadaEm, os.ExecucaoFinalizadaEm, os.EntregueEm, os.CanceladaEm,
        os.TempoExecucao?.TotalMinutes);
}
