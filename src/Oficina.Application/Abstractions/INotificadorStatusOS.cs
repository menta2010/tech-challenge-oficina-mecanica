namespace Oficina.Application.Abstractions;

/// <summary>Evento publicado a cada mudanca de status da OS.</summary>
public sealed record StatusOSAlterado(
    Guid OrdemServicoId,
    Guid LojaId,
    Guid ClienteId,
    string? EmailCliente,
    string NovoStatus,
    DateTime OcorridoEm);

/// <summary>
/// Notifica o cliente sobre a mudanca de status da OS.
/// Em nuvem a implementacao publica no Amazon SNS (notificacao serverless, ver ADR-006);
/// localmente apenas registra em log.
/// </summary>
public interface INotificadorStatusOS
{
    Task NotificarAsync(StatusOSAlterado evento, CancellationToken ct = default);
}
