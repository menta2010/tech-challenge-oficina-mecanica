using FluentAssertions;
using Oficina.Domain.OrdensServico;
using Oficina.Domain.Shared;
using Xunit;

namespace Oficina.UnitTests.OrdensServico;

// Historico de status (base do tempo medio por status) e exclusao logica explicita (Encerrada).
public class HistoricoEExclusaoLogicaTests
{
    private static OrdemServico NovaOs() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    private static OrdemServico OsEmExecucao()
    {
        var os = NovaOs();
        os.IniciarDiagnostico();
        os.RegistrarServico(Guid.NewGuid(), "Troca de oleo", Money.From(120m), TimeSpan.FromHours(1));
        os.FinalizarDiagnostico();
        os.AprovarOrcamento();
        return os;
    }

    [Fact]
    public void Criar_AbrePeriodoRecebida()
    {
        var os = NovaOs();

        os.Historico.Should().ContainSingle()
            .Which.Should().Match<HistoricoStatusOS>(h => h.Status == StatusOS.Recebida && h.Fim == null);
        os.Encerrada.Should().BeFalse();
    }

    [Fact]
    public void CadaTransicao_FechaPeriodoAnteriorEAbreNovo()
    {
        var os = OsEmExecucao();

        os.Historico.Select(h => h.Status).Should().Equal(
            StatusOS.Recebida, StatusOS.EmDiagnostico, StatusOS.AguardandoAprovacao, StatusOS.EmExecucao);
        os.Historico.Count(h => h.Fim == null).Should().Be(1);
        os.Historico.Single(h => h.Fim == null).Status.Should().Be(StatusOS.EmExecucao);
        os.UltimoPeriodoConcluido()!.Status.Should().Be(StatusOS.AguardandoAprovacao);
    }

    [Fact]
    public void Finalizar_MarcaEncerrada()
    {
        var os = OsEmExecucao();
        os.ExecutarServico(os.Servicos.Single().Id);

        os.FinalizarExecucao();

        os.Encerrada.Should().BeTrue();
        os.EncerradaEm.Should().NotBeNull();
    }

    [Fact]
    public void Entregar_MantemEncerradaComDataOriginal()
    {
        var os = OsEmExecucao();
        os.ExecutarServico(os.Servicos.Single().Id);
        os.FinalizarExecucao();
        var encerradaEm = os.EncerradaEm;

        os.EntregarVeiculo();

        os.Encerrada.Should().BeTrue();
        os.EncerradaEm.Should().Be(encerradaEm);
    }

    [Fact]
    public void Cancelar_MarcaEncerrada()
    {
        var os = NovaOs();
        os.IniciarDiagnostico();
        os.RegistrarServico(Guid.NewGuid(), "Alinhamento", Money.From(90m), TimeSpan.FromMinutes(50));
        os.FinalizarDiagnostico();

        os.Cancelar();

        os.Encerrada.Should().BeTrue();
    }

    [Theory]
    [InlineData(StatusOS.Finalizada, true)]
    [InlineData(StatusOS.Entregue, true)]
    [InlineData(StatusOS.Cancelada, true)]
    [InlineData(StatusOS.EmExecucao, false)]
    [InlineData(StatusOS.Recebida, false)]
    public void StatusTerminais_DefinemAExclusaoLogica(StatusOS status, bool terminal)
        => OrdemServico.StatusTerminais.Contains(status).Should().Be(terminal);

    [Fact]
    public void Criar_SemLoja_DeveLancar()
    {
        var acao = () => new OrdemServico(Guid.Empty, Guid.NewGuid(), Guid.NewGuid());
        acao.Should().Throw<DomainException>().WithMessage("*Loja*");
    }
}
