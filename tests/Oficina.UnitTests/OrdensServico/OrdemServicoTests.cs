using FluentAssertions;
using Oficina.Domain.OrdensServico;
using Oficina.Domain.Shared;
using Xunit;

namespace Oficina.UnitTests.OrdensServico;

// Cobre o ciclo de vida da OS e a maquina de estados (alteracao automatica de status,
// geracao de orcamento, baixa logica de peca, finalizacao e entrega).
public class OrdemServicoTests
{
    private static readonly Guid Cliente = Guid.NewGuid();
    private static readonly Guid Veiculo = Guid.NewGuid();

    private static OrdemServico OsEmDiagnostico()
    {
        var os = new OrdemServico(Cliente, Veiculo);
        os.IniciarDiagnostico();
        return os;
    }

    private static OrdemServico OsAguardandoAprovacao()
    {
        var os = OsEmDiagnostico();
        os.RegistrarServico(Guid.NewGuid(), "Troca de oleo", Money.From(120m), TimeSpan.FromHours(1));
        os.RegistrarPeca(Guid.NewGuid(), 2, Money.From(30m));
        os.FinalizarDiagnostico();
        return os;
    }

    [Fact]
    public void Criar_DeveIniciarComoRecebida()
    {
        var os = new OrdemServico(Cliente, Veiculo);
        os.Status.Should().Be(StatusOS.Recebida);
        os.CriadaEm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Criar_SemCliente_DeveLancar()
    {
        var acao = () => new OrdemServico(Guid.Empty, Veiculo);
        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void FinalizarDiagnostico_DeveGerarOrcamentoEAguardarAprovacao()
    {
        var os = OsAguardandoAprovacao();

        os.Status.Should().Be(StatusOS.AguardandoAprovacao);
        os.Orcamento.Should().NotBeNull();
        os.Orcamento!.ValorServicos.Valor.Should().Be(120m);
        os.Orcamento.ValorPecas.Valor.Should().Be(60m);   // 2 x 30
        os.Orcamento.ValorTotal.Valor.Should().Be(180m);
        os.Orcamento.PrevisaoEntrega.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void FinalizarDiagnostico_SemServicos_DeveLancar()
    {
        var os = OsEmDiagnostico();
        var acao = () => os.FinalizarDiagnostico();
        acao.Should().Throw<DomainException>().WithMessage("*ao menos um servico*");
    }

    [Fact]
    public void RemoverServico_DeveRecalcularOrcamento()
    {
        var os = OsEmDiagnostico();
        os.RegistrarServico(Guid.NewGuid(), "A", Money.From(100m), TimeSpan.FromHours(1));
        os.RegistrarServico(Guid.NewGuid(), "B", Money.From(50m), TimeSpan.FromHours(1));
        os.FinalizarDiagnostico();
        os.Orcamento!.ValorServicos.Valor.Should().Be(150m);

        var item = os.Servicos.First();
        os.RemoverServico(item.Id);

        os.Orcamento!.ValorServicos.Valor.Should().Be(50m);
    }

    [Fact]
    public void Cancelar_EmAguardandoAprovacao_DeveCancelar()
    {
        var os = OsAguardandoAprovacao();
        os.Cancelar();
        os.Status.Should().Be(StatusOS.Cancelada);
        os.CanceladaEm.Should().NotBeNull();
    }

    [Fact]
    public void IniciarDiagnostico_ForaDeRecebida_DeveLancarTransicaoInvalida()
    {
        var os = OsAguardandoAprovacao();
        var acao = () => os.IniciarDiagnostico();
        acao.Should().Throw<InvalidStatusTransitionException>();
    }

    [Fact]
    public void FinalizarExecucao_ComServicoPendente_DeveLancar()
    {
        var os = OsAguardandoAprovacao();
        os.AprovarOrcamento();          // -> EmExecucao
        var acao = () => os.FinalizarExecucao();
        acao.Should().Throw<DomainException>().WithMessage("*nao executados*");
    }

    [Fact]
    public void FluxoCompleto_DeveChegarAEntregueComTempoDeExecucao()
    {
        var os = OsAguardandoAprovacao();
        os.AprovarOrcamento();
        os.Status.Should().Be(StatusOS.EmExecucao);

        foreach (var s in os.Servicos.ToList())
            os.ExecutarServico(s.Id);
        foreach (var p in os.Pecas.ToList())
            os.RegistrarUsoPeca(p.Id);

        os.FinalizarExecucao();
        os.Status.Should().Be(StatusOS.Finalizada);

        os.EntregarVeiculo();
        os.Status.Should().Be(StatusOS.Entregue);

        os.Pecas.Should().OnlyContain(p => p.Utilizado);
        os.TempoExecucao.Should().NotBeNull();
        os.TempoExecucao!.Value.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public void EntregarVeiculo_AntesDeFinalizar_DeveLancar()
    {
        var os = OsAguardandoAprovacao();
        os.AprovarOrcamento();
        var acao = () => os.EntregarVeiculo();
        acao.Should().Throw<InvalidStatusTransitionException>();
    }
}
