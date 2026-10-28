using Microsoft.Extensions.Logging;
using Oficina.Application.Abstractions;
using Oficina.Domain.OrdensServico;
using Oficina.Domain.Shared;
using NR = NewRelic.Api.Agent;

namespace Oficina.Infrastructure.Observabilidade;

/// <summary>
/// Envia eventos de negocio ao New Relic de duas formas:
/// 1) Custom event "OrdemServicoEvento" pelo agente .NET (consultavel em NRQL; sem agente, e no-op);
/// 2) Log estruturado com os mesmos campos (Logs in Context, correlacionado ao trace da requisicao).
///
/// NRQL dos dashboards (ver docs/observabilidade.md):
///   SELECT count(*) FROM OrdemServicoEvento WHERE evento = 'criada' FACET lojaId TIMESERIES 1 day
///   SELECT average(minutosNoStatusAnterior) FROM OrdemServicoEvento WHERE evento = 'status_alterado' FACET statusAnterior
///   SELECT count(*) FROM OrdemServicoEvento WHERE evento = 'falha' FACET operacao, erro
/// </summary>
public sealed class NewRelicMetricasOrdemServico : IMetricasOrdemServico
{
    public const string TipoEvento = "OrdemServicoEvento";

    private readonly ILogger<NewRelicMetricasOrdemServico> _logger;

    public NewRelicMetricasOrdemServico(ILogger<NewRelicMetricasOrdemServico> logger) => _logger = logger;

    public void OrdemCriada(OrdemServico os)
    {
        Registrar(new Dictionary<string, object>
        {
            ["evento"] = "criada",
            ["ordemServicoId"] = os.Id.ToString(),
            ["lojaId"] = os.LojaId.ToString(),
            ["status"] = os.Status.ToString()
        });
        _logger.LogInformation("OS {OrdemServicoId} criada na loja {LojaId} com status {Status}",
            os.Id, os.LojaId, os.Status);
    }

    public void StatusAlterado(OrdemServico os, HistoricoStatusOS periodoAnterior)
    {
        var minutos = Math.Round(periodoAnterior.Duracao?.TotalMinutes ?? 0, 2);
        Registrar(new Dictionary<string, object>
        {
            ["evento"] = "status_alterado",
            ["ordemServicoId"] = os.Id.ToString(),
            ["lojaId"] = os.LojaId.ToString(),
            ["statusAnterior"] = periodoAnterior.Status.ToString(),
            ["status"] = os.Status.ToString(),
            ["minutosNoStatusAnterior"] = minutos
        });
        _logger.LogInformation(
            "OS {OrdemServicoId} mudou de {StatusAnterior} para {Status} apos {MinutosNoStatusAnterior} min",
            os.Id, periodoAnterior.Status, os.Status, minutos);
    }

    public void FalhaProcessamento(Guid? ordemServicoId, string operacao, Exception erro)
    {
        // Regra de negocio violada (ex.: transicao invalida) e esperado; o alerta olha so as falhas inesperadas
        var esperada = erro is DomainException or FluentValidation.ValidationException
            || erro.GetType().Namespace == "Oficina.Application.Common";

        Registrar(new Dictionary<string, object>
        {
            ["evento"] = "falha",
            ["ordemServicoId"] = ordemServicoId?.ToString() ?? string.Empty,
            ["operacao"] = operacao,
            ["erro"] = erro.GetType().Name,
            ["inesperada"] = !esperada
        });

        if (esperada)
            _logger.LogWarning("Operacao {Operacao} recusada para a OS {OrdemServicoId}: {Motivo}",
                operacao, ordemServicoId, erro.Message);
        else
            _logger.LogError(erro, "Falha no processamento da OS {OrdemServicoId} na operacao {Operacao}",
                ordemServicoId, operacao);
    }

    private static void Registrar(Dictionary<string, object> atributos)
        => NR.NewRelic.RecordCustomEvent(TipoEvento, atributos);
}
