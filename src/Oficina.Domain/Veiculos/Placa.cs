using System.Text.RegularExpressions;
using Oficina.Domain.Shared;

namespace Oficina.Domain.Veiculos;

/// <summary>
/// Value Object de placa veicular. Aceita o padrao antigo (AAA0000) e o
/// Mercosul (AAA0A00). Atende ao requisito de validacao de placa do PDF.
/// </summary>
public sealed partial class Placa
{
    public string Valor { get; }

    private Placa(string valor) => Valor = valor;

    public static Placa Criar(string entrada)
    {
        var v = (entrada ?? string.Empty)
            .Trim().ToUpperInvariant()
            .Replace("-", string.Empty).Replace(" ", string.Empty);

        if (Antiga().IsMatch(v) || Mercosul().IsMatch(v))
            return new Placa(v);

        throw new DomainException("Placa invalida. Use o formato AAA0000 ou AAA0A00 (Mercosul).");
    }

    [GeneratedRegex("^[A-Z]{3}[0-9]{4}$")]
    private static partial Regex Antiga();

    [GeneratedRegex("^[A-Z]{3}[0-9][A-Z][0-9]{2}$")]
    private static partial Regex Mercosul();

    public override bool Equals(object? obj) => obj is Placa p && p.Valor == Valor;
    public override int GetHashCode() => Valor.GetHashCode();
    public override string ToString() => Valor;
}
