using Oficina.Domain.Shared;

namespace Oficina.Domain.Servicos;

/// <summary>Raiz de agregado Servico (catalogo). Atende ao CRUD de servicos; base de valor e tempo do orcamento.</summary>
public sealed class Servico : Entity
{
    public string Nome { get; private set; } = null!;
    public string? Descricao { get; private set; }
    public Money ValorBase { get; private set; } = null!;
    public TimeSpan TempoEstimado { get; private set; }

    private Servico() { } // EF

    public Servico(string nome, Money valorBase, TimeSpan tempoEstimado, string? descricao = null)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new DomainException("Nome do servico e obrigatorio.");
        if (tempoEstimado <= TimeSpan.Zero) throw new DomainException("Tempo estimado deve ser positivo.");
        Nome = nome.Trim();
        ValorBase = valorBase ?? throw new DomainException("Valor base e obrigatorio.");
        TempoEstimado = tempoEstimado;
        Descricao = descricao;
    }
    public void Atualizar(string nome, Money valorBase, TimeSpan tempoEstimado, string? descricao)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new DomainException("Nome do servico e obrigatorio.");
        if (tempoEstimado <= TimeSpan.Zero) throw new DomainException("Tempo estimado deve ser positivo.");
        Nome = nome.Trim();
        ValorBase = valorBase ?? throw new DomainException("Valor base e obrigatorio.");
        TempoEstimado = tempoEstimado;
        Descricao = descricao;
    }
}
