using Oficina.Domain.Shared;

namespace Oficina.Domain.OrdensServico;

/// <summary>
/// Raiz de agregado e nucleo do dominio. Controla o ciclo de vida da OS,
/// servicos/pecas, orcamento e a maquina de estados (alteracao automatica de status).
/// Registra timestamps usados no relatorio de tempo medio de execucao.
/// </summary>
public sealed class OrdemServico : Entity
{
    private readonly List<ItemServico> _servicos = new();
    private readonly List<ItemPeca> _pecas = new();

    public Guid ClienteId { get; private set; }
    public Guid VeiculoId { get; private set; }
    public StatusOS Status { get; private set; }
    public Orcamento? Orcamento { get; private set; }

    public IReadOnlyCollection<ItemServico> Servicos => _servicos;
    public IReadOnlyCollection<ItemPeca> Pecas => _pecas;

    public DateTime CriadaEm { get; private set; }
    public DateTime? DiagnosticoIniciadoEm { get; private set; }
    public DateTime? ExecucaoIniciadaEm { get; private set; }
    public DateTime? ExecucaoFinalizadaEm { get; private set; }
    public DateTime? EntregueEm { get; private set; }
    public DateTime? CanceladaEm { get; private set; }

    /// <summary>Duracao da execucao (para o relatorio de tempo medio). Null se ainda nao finalizada.</summary>
    public TimeSpan? TempoExecucao =>
        ExecucaoIniciadaEm.HasValue && ExecucaoFinalizadaEm.HasValue
            ? ExecucaoFinalizadaEm - ExecucaoIniciadaEm
            : null;

    private OrdemServico() { } // EF

    public OrdemServico(Guid clienteId, Guid veiculoId)
    {
        if (clienteId == Guid.Empty) throw new DomainException("Cliente e obrigatorio para criar a OS.");
        if (veiculoId == Guid.Empty) throw new DomainException("Veiculo e obrigatorio para criar a OS.");
        ClienteId = clienteId;
        VeiculoId = veiculoId;
        Status = StatusOS.Recebida;          // politica: status inicial = Recebida
        CriadaEm = DateTime.UtcNow;
    }

    // --- Fase 2: Diagnostico ---

    public void IniciarDiagnostico()
    {
        Exigir(StatusOS.Recebida, "iniciar diagnostico");
        Status = StatusOS.EmDiagnostico;
        DiagnosticoIniciadoEm = DateTime.UtcNow;
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
        Status = StatusOS.AguardandoAprovacao;
    }

    // --- Fase 3: Orcamento e aprovacao ---

    /// <summary>Cliente reprova um item: remove e recalcula o orcamento (permanece aguardando aprovacao).</summary>
    public void RemoverServico(Guid itemServicoId)
    {
        Exigir(StatusOS.AguardandoAprovacao, "remover servico");
        var item = _servicos.FirstOrDefault(s => s.Id == itemServicoId)
                   ?? throw new DomainException("Item de servico nao encontrado na OS.");
        _servicos.Remove(item);
        GerarOrcamento();
    }

    public void RemoverPeca(Guid itemPecaId)
    {
        Exigir(StatusOS.AguardandoAprovacao, "remover peca");
        var item = _pecas.FirstOrDefault(p => p.Id == itemPecaId)
                   ?? throw new DomainException("Item de peca nao encontrado na OS.");
        _pecas.Remove(item);
        GerarOrcamento();
    }

    public void AprovarOrcamento()
    {
        Exigir(StatusOS.AguardandoAprovacao, "aprovar orcamento");
        Status = StatusOS.EmExecucao;           // aprovacao inicia a janela de execucao
        ExecucaoIniciadaEm = DateTime.UtcNow;
    }

    public void Cancelar()
    {
        Exigir(StatusOS.AguardandoAprovacao, "cancelar OS");
        Status = StatusOS.Cancelada;            // soft-cancel (decisao de MVP)
        CanceladaEm = DateTime.UtcNow;
    }

    // --- Fase 4: Execucao ---

    public void ExecutarServico(Guid itemServicoId)
    {
        Exigir(StatusOS.EmExecucao, "executar servico");
        var item = _servicos.FirstOrDefault(s => s.Id == itemServicoId)
                   ?? throw new DomainException("Item de servico nao encontrado na OS.");
        item.MarcarExecutado();
    }

    /// <summary>
    /// Marca a peca como utilizada. A baixa fisica de estoque (PecaInsumo.Baixar)
    /// e orquestrada pela camada de aplicacao, respeitando o limite do agregado.
    /// </summary>
    public void RegistrarUsoPeca(Guid itemPecaId)
    {
        Exigir(StatusOS.EmExecucao, "registrar uso de peca");
        var item = _pecas.FirstOrDefault(p => p.Id == itemPecaId)
                   ?? throw new DomainException("Item de peca nao encontrado na OS.");
        item.MarcarUtilizado();
    }

    public void FinalizarExecucao()
    {
        Exigir(StatusOS.EmExecucao, "finalizar execucao");
        if (_servicos.Any(s => !s.Executado))
            throw new DomainException("Existem servicos nao executados; nao e possivel finalizar.");
        Status = StatusOS.Finalizada;
        ExecucaoFinalizadaEm = DateTime.UtcNow;
    }

    // --- Fase 5: Entrega ---

    public void EntregarVeiculo()
    {
        Exigir(StatusOS.Finalizada, "entregar veiculo");
        Status = StatusOS.Entregue;
        EntregueEm = DateTime.UtcNow;
    }

    // --- internos ---

    private void GerarOrcamento()
    {
        var valorServicos = _servicos.Aggregate(Money.Zero, (acc, s) => acc + s.Valor);
        var valorPecas = _pecas.Aggregate(Money.Zero, (acc, p) => acc + p.Subtotal);
        var tempoTotal = _servicos.Aggregate(TimeSpan.Zero, (acc, s) => acc + s.TempoEstimado);
        Orcamento = new Orcamento(valorServicos, valorPecas, DateTime.UtcNow.Add(tempoTotal));
    }

    private void Exigir(StatusOS esperado, string acao)
    {
        if (Status != esperado)
            throw new InvalidStatusTransitionException(acao, Status, esperado);
    }
}
