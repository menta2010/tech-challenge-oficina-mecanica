using Oficina.Domain.Shared;

namespace Oficina.Domain.OrdensServico;

/// <summary>
/// Periodo em que a OS permaneceu em um status (Inicio..Fim). Fim nulo = status atual.
/// Base do indicador "tempo medio por status" (Diagnostico, Execucao, Finalizacao)
/// e trilha de auditoria das transicoes.
/// </summary>
public sealed class HistoricoStatusOS : Entity
{
    public StatusOS Status { get; private set; }
    public DateTime Inicio { get; private set; }
    public DateTime? Fim { get; private set; }

    public TimeSpan? Duracao => Fim - Inicio;

    private HistoricoStatusOS() { } // EF

    internal HistoricoStatusOS(StatusOS status, DateTime inicio)
    {
        Status = status;
        Inicio = inicio;
    }

    internal void Encerrar(DateTime fim) => Fim = fim;
}
