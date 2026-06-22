namespace Oficina.Domain.OrdensServico;

/// <summary>
/// Estados da OS conforme o PDF (Recebida..Entregue) + Cancelada (decisao de MVP).
/// As transicoes validas estao em OrdemServico (maquina de estados).
/// </summary>
public enum StatusOS
{
    Recebida,
    EmDiagnostico,
    AguardandoAprovacao,
    EmExecucao,
    Finalizada,
    Entregue,
    Cancelada
}
