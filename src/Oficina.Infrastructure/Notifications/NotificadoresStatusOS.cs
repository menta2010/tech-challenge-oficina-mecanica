using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oficina.Application.Abstractions;

namespace Oficina.Infrastructure.Notifications;

public sealed class NotificacoesOptions
{
    /// <summary>ARN do topico SNS. Vazio = notificacao apenas em log (ambiente local).</summary>
    public string? SnsTopicArn { get; set; }
}

/// <summary>
/// Publica o evento de mudanca de status no Amazon SNS (notificacao serverless).
/// O topico entrega aos assinantes (e-mail e, futuramente, SMS ou outra Lambda) sem a API
/// precisar conhecer cada canal. Falha de notificacao nao desfaz a mudanca de status:
/// e registrada em log de erro (gera alerta no New Relic) e a requisicao segue.
/// </summary>
public sealed class SnsNotificadorStatusOS : INotificadorStatusOS
{
    private readonly IAmazonSimpleNotificationService _sns;
    private readonly string _topicArn;
    private readonly ILogger<SnsNotificadorStatusOS> _logger;

    public SnsNotificadorStatusOS(IAmazonSimpleNotificationService sns, IOptions<NotificacoesOptions> options,
        ILogger<SnsNotificadorStatusOS> logger)
    {
        _sns = sns;
        _topicArn = options.Value.SnsTopicArn!;
        _logger = logger;
    }

    public async Task NotificarAsync(StatusOSAlterado evento, CancellationToken ct = default)
    {
        try
        {
            await _sns.PublishAsync(new PublishRequest
            {
                TopicArn = _topicArn,
                Subject = $"Oficina - sua OS mudou para {evento.NovoStatus}",
                Message = JsonSerializer.Serialize(evento),
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    ["status"] = new() { DataType = "String", StringValue = evento.NovoStatus },
                    ["lojaId"] = new() { DataType = "String", StringValue = evento.LojaId.ToString() }
                }
            }, ct);

            _logger.LogInformation("Notificacao publicada no SNS para a OS {OrdemServicoId} ({NovoStatus})",
                evento.OrdemServicoId, evento.NovoStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao publicar notificacao da OS {OrdemServicoId} no SNS", evento.OrdemServicoId);
        }
    }
}

/// <summary>Notificador local: apenas registra o "envio" em log.</summary>
public sealed class LogNotificadorStatusOS : INotificadorStatusOS
{
    private readonly ILogger<LogNotificadorStatusOS> _logger;
    public LogNotificadorStatusOS(ILogger<LogNotificadorStatusOS> logger) => _logger = logger;

    public Task NotificarAsync(StatusOSAlterado evento, CancellationToken ct = default)
    {
        _logger.LogInformation("[NOTIFICACAO] OS {OrdemServicoId} mudou para {NovoStatus}. Cliente {ClienteId} notificado.",
            evento.OrdemServicoId, evento.NovoStatus, evento.ClienteId);
        return Task.CompletedTask;
    }
}
