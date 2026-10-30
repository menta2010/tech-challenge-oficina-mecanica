using Oficina.Domain.OrdensServico;

namespace Oficina.Application.Abstractions;

/// <summary>
/// Eventos de negocio enviados a observabilidade (New Relic). Alimentam os dashboards de
/// volume diario de OS, tempo medio por status e falhas no processamento.
/// </summary>
public interface IMetricasOrdemServico
{
    void OrdemCriada(OrdemServico os);

    /// <summary>Registra a transicao e quanto tempo a OS ficou no status anterior.</summary>
    void StatusAlterado(OrdemServico os, HistoricoStatusOS periodoAnterior);

    void FalhaProcessamento(Guid? ordemServicoId, string operacao, Exception erro);
}
