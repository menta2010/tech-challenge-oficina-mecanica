using System.Diagnostics;
using Serilog.Context;

namespace Oficina.API.Middlewares;

/// <summary>
/// Correlacao entre requisicoes: le o header x-correlation-id (gerado pelo API Gateway/Lambda
/// ou pelo cliente) ou cria um novo, devolve na resposta e o injeta em todos os logs da requisicao
/// (campo CorrelationId no JSON) e no trace do New Relic.
/// </summary>
public sealed class CorrelacaoMiddleware
{
    public const string Header = "x-correlation-id";

    private readonly RequestDelegate _next;
    public CorrelacaoMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        // O valor vem do cliente: so e aceito se for curto e "seguro" (evita poluir/forjar logs e headers)
        var correlationId = context.Request.Headers.TryGetValue(Header, out var valor) && Valido(valor.ToString())
            ? valor.ToString()
            : Guid.NewGuid().ToString();

        context.Items[Header] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[Header] = correlationId;
            return Task.CompletedTask;
        });

        Activity.Current?.SetTag("correlation.id", correlationId);
        NewRelic.Api.Agent.NewRelic.GetAgent().CurrentTransaction.AddCustomAttribute("correlationId", correlationId);

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    public static string? Obter(HttpContext context) => context.Items[Header] as string;

    internal static bool Valido(string? valor)
        => !string.IsNullOrWhiteSpace(valor)
           && valor.Length <= 100
           && valor.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or ':');
}
