using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Oficina.Domain.Clientes;
using Oficina.Domain.Shared;
using Oficina.Domain.Veiculos;

namespace Oficina.Infrastructure.Persistence;

/// <summary>
/// Conversores entre Value Objects e colunas relacionais.
/// Documento e Placa viram uma unica coluna de texto; Money vira numeric.
/// A reconstrucao usa as fabricas do dominio (dados ja validados em escrita).
/// </summary>
public static class Converters
{
    public static readonly ValueConverter<Documento, string> Documento =
        new(d => d.Numero, s => Oficina.Domain.Clientes.Documento.Criar(s));

    public static readonly ValueConverter<Placa, string> Placa =
        new(p => p.Valor, s => Oficina.Domain.Veiculos.Placa.Criar(s));

    public static readonly ValueConverter<Money, decimal> Money =
        new(m => m.Valor, v => Oficina.Domain.Shared.Money.From(v));
}
