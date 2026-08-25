using Microsoft.Extensions.Logging;
using Oficina.Application.Abstractions;

namespace Oficina.Infrastructure.Notifications;

/// <summary>
/// Notificador de e-mail plugavel. Nesta versao registra o "envio" em log
/// (sem credenciais). Para envio real, basta trocar esta implementacao por
/// um cliente SMTP/servico de e-mail, sem tocar no dominio/aplicacao.
/// </summary>
public sealed class EmailNotificadorLog : INotificadorEmail
{
    private readonly ILogger<EmailNotificadorLog> _logger;
    public EmailNotificadorLog(ILogger<EmailNotificadorLog> logger) => _logger = logger;

    public Task NotificarMudancaStatusAsync(Guid ordemServicoId, string novoStatus, CancellationToken ct = default)
    {
        _logger.LogInformation("[EMAIL] OS {OrdemServicoId} mudou de status para '{NovoStatus}'. Cliente notificado.",
            ordemServicoId, novoStatus);
        return Task.CompletedTask;
    }
}
