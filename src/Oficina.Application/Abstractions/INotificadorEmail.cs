namespace Oficina.Application.Abstractions;

/// <summary>
/// Notifica o cliente sobre mudancas de status da OS (ex.: por e-mail).
/// Implementacao plugavel na Infrastructure. Atende ao requisito
/// "atualizacao de status da OS via alguma ferramenta como email" (Fase 2).
/// </summary>
public interface INotificadorEmail
{
    Task NotificarMudancaStatusAsync(Guid ordemServicoId, string novoStatus, CancellationToken ct = default);
}
