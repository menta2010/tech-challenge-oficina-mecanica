using Oficina.Domain.Shared;

namespace Oficina.Domain.OrdensServico;

/// <summary>
/// Raiz de agregado e nucleo do dominio. Controla o ciclo de vida da OS,
/// servicos/pecas, orcamento e a maquina de estados (alteracao automatica de status).
/// Toda mudanca de status passa por <see cref="AlterarStatus"/>, que registra o historico
/// e marca a exclusao logica (Encerrada) quando a OS chega a um status terminal.
/// </summary>
public sealed class OrdemServico : Entity
{
    /// <summary>Status que encerram a OS: ela sai da listagem operacional (exclusao logica).</summary>
    public static readonly IReadOnlySet<StatusOS> StatusTerminais =
        new HashSet<StatusOS> { StatusOS.Finalizada, StatusOS.Entregue, StatusOS.Cancelada };

    private readonly List<ItemServico> _servicos = new();
    private readonly List<ItemPeca> _pecas = new();
    private readonly List<HistoricoStatusOS> _historico = new();

    public Guid LojaId { get; private set; }
    public Guid ClienteId { get; private set; }
    public Guid VeiculoId { get; private set; }
    public StatusOS Status { get; private set; }
    public Orcamento? Orcamento { get; private set; }

    /// <summary>
    /// Exclusao logica: true quando a OS esta Finalizada, Entregue ou Cancelada.
    /// Persistida e indexada, a listagem operacional filtra por ela.
    /// </summary>
    public bool Encerrada { get; private set; }

    public IReadOnlyCollection<ItemServico> Servicos => _servicos;
    public IReadOnlyCollection<ItemPeca> Pecas => _pecas;
    public IReadOnlyCollection<HistoricoStatusOS> Historico => _historico;

    public DateTime CriadaEm { get; private set; }
    public DateTime? DiagnosticoIniciadoEm { get; private set; }
    public DateTime? ExecucaoIniciadaEm { get; private set; }
    public DateTime? ExecucaoFinalizadaEm { get; private set; }
    public DateTime? EntregueEm { get; private set; }
    public DateTime? CanceladaEm { get; private set; }
    public DateTime? EncerradaEm { get; private set; }

    /// <summary>Duracao da execucao (para o relatorio de tempo medio). Null se ainda nao finalizada.</summary>
    public TimeSpan? TempoExecucao =>
        ExecucaoIniciadaEm.HasValue && ExecucaoFinalizadaEm.HasValue
            ? ExecucaoFinalizadaEm - ExecucaoIniciadaEm
            : null;

    private OrdemServico() { } // EF

    public OrdemServico(Guid lojaId, Guid clienteId, Guid veiculoId)
    {
        if (lojaId == Guid.Empty) throw new DomainException("Loja e obrigatoria para criar a OS.");
        if (clienteId == Guid.Empty) throw new DomainException("Cliente e obrigatorio para criar a OS.");
        if (veiculoId == Guid.Empty) throw new DomainException("Veiculo e obrigatorio para criar a OS.");
        LojaId = lojaId;
        ClienteId = clienteId;
        VeiculoId = veiculoId;
        CriadaEm = DateTime.UtcNow;
        AlterarStatus(StatusOS.Recebida, CriadaEm);   // politica: status inicial = Recebida
    }

    // --- Etapa 1: Diagnostico ---

    public void IniciarDiagnostico()
    {
        Exigir(StatusOS.Recebida, "iniciar diagnostico");
        DiagnosticoIniciadoEm = AlterarStatus(StatusOS.EmDiagnostico);
    }

    public void RegistrarServico(Guid servicoId, string descricao, Money valor, TimeSpan tempoEstimado)
    {
        Exigir(StatusOS.EmDiagnostico, "registrar servico");
        _servicos.Add(new ItemServico(servicoId, descricao, valor, tempoEstimado));
    }

    public void RegistrarPeca(Guid pecaId, int quantidade, Money valorUnitario)
    {
        Exigir(StatusOS.EmDiagnostico, "registrar peca");
        _pecas.Add(new ItemPeca(pecaId, quantidade, valorUnitario));
    }

    public void FinalizarDiagnostico()
    {
        Exigir(StatusOS.EmDiagnostico, "finalizar diagnostico");
        if (_servicos.Count == 0)
            throw new DomainException("Diagnostico precisa de ao menos um servico para gerar orcamento.");
        GerarOrcamento();                       // politica: gera orcamento automaticamente
        AlterarStatus(StatusOS.AguardandoAprovacao);
    }

    // --- Etapa 2: Orcamento e aprovacao ---

    /// <summary>Cliente reprova um item: remove e recalcula o orcamento (permanece aguardando aprovacao).</summary>
    public void RemoverServico(Guid itemServicoId)
    {
        Exigir(StatusOS.AguardandoAprovacao, "remover servico");
        _servicos.Remove(ObterItem(_servicos, itemServicoId, s => s.Id, "Item de servico nao encontrado na OS."));
        GerarOrcamento();
    }

    public void RemoverPeca(Guid itemPecaId)
    {
        Exigir(StatusOS.AguardandoAprovacao, "remover peca");
        _pecas.Remove(ObterItem(_pecas, itemPecaId, p => p.Id, "Item de peca nao encontrado na OS."));
        GerarOrcamento();
    }

    public void AprovarOrcamento()
    {
        Exigir(StatusOS.AguardandoAprovacao, "aprovar orcamento");
        ExecucaoIniciadaEm = AlterarStatus(StatusOS.EmExecucao);  // aprovacao inicia a janela de execucao
    }

    public void Cancelar()
    {
        Exigir(StatusOS.AguardandoAprovacao, "cancelar OS");
        CanceladaEm = AlterarStatus(StatusOS.Cancelada);
    }

    // --- Etapa 3: Execucao ---

    public void ExecutarServico(Guid itemServicoId)
    {
        Exigir(StatusOS.EmExecucao, "executar servico");
        ObterItem(_servicos, itemServicoId, s => s.Id, "Item de servico nao encontrado na OS.").MarcarExecutado();
    }

    /// <summary>
    /// Marca a peca como utilizada. A baixa fisica de estoque (EstoqueLoja.Baixar)
    /// e orquestrada pela camada de aplicacao, respeitando o limite do agregado.
    /// </summary>
    public void RegistrarUsoPeca(Guid itemPecaId)
    {
        Exigir(StatusOS.EmExecucao, "registrar uso de peca");
        ObterItem(_pecas, itemPecaId, p => p.Id, "Item de peca nao encontrado na OS.").MarcarUtilizado();
    }

    public void FinalizarExecucao()
    {
        Exigir(StatusOS.EmExecucao, "finalizar execucao");
        if (_servicos.Any(s => !s.Executado))
            throw new DomainException("Existem servicos nao executados; nao e possivel finalizar.");
        ExecucaoFinalizadaEm = AlterarStatus(StatusOS.Finalizada);
    }

    // --- Etapa 4: Entrega ---

    public void EntregarVeiculo()
    {
        Exigir(StatusOS.Finalizada, "entregar veiculo");
        EntregueEm = AlterarStatus(StatusOS.Entregue);
    }

    /// <summary>Ultimo periodo concluido (status anterior), usado nas metricas de tempo por status.</summary>
    public HistoricoStatusOS? UltimoPeriodoConcluido()
        => _historico.Where(h => h.Fim is not null).MaxBy(h => h.Fim);

    // --- internos ---

    /// <summary>Unico ponto que muda o status: fecha o periodo atual, abre o novo e aplica a exclusao logica.</summary>
    private DateTime AlterarStatus(StatusOS novo, DateTime? quando = null)
    {
        var agora = quando ?? DateTime.UtcNow;
        _historico.LastOrDefault(h => h.Fim is null)?.Encerrar(agora);
        _historico.Add(new HistoricoStatusOS(novo, agora));
        Status = novo;

        if (StatusTerminais.Contains(novo) && !Encerrada)
        {
            Encerrada = true;
            EncerradaEm = agora;
        }
        return agora;
    }

    private void GerarOrcamento()
    {
        var valorServicos = _servicos.Aggregate(Money.Zero, (acc, s) => acc + s.Valor);
        var valorPecas = _pecas.Aggregate(Money.Zero, (acc, p) => acc + p.Subtotal);
        var tempoTotal = _servicos.Aggregate(TimeSpan.Zero, (acc, s) => acc + s.TempoEstimado);
        Orcamento = new Orcamento(valorServicos, valorPecas, DateTime.UtcNow.Add(tempoTotal));
    }

    private static T ObterItem<T>(IEnumerable<T> itens, Guid id, Func<T, Guid> chave, string erro)
        => itens.FirstOrDefault(i => chave(i) == id) ?? throw new DomainException(erro);

    private void Exigir(StatusOS esperado, string acao)
    {
        if (Status != esperado)
            throw new TransicaoStatusInvalidaException(acao, Status, esperado);
    }
}
