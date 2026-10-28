using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Oficina.Application.Common;
using Oficina.Domain.Shared;

namespace Oficina.API.Middlewares;

/// <summary>
/// Traduz excecoes em respostas no padrao Problem Details (RFC 7807), sempre com o
/// correlationId para rastrear o erro nos logs:
/// ValidationException/DomainException -> 400, UnauthorizedException -> 401,
/// ForbiddenException -> 403, NotFoundException -> 404, ConflictException e violacao de
/// integridade no banco -> 409, demais -> 500 (sem expor detalhes internos).
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (status, titulo, erros) = Classificar(ex);

            if (status >= StatusCodes.Status500InternalServerError)
                _logger.LogError(ex, "Erro nao tratado em {Metodo} {Rota}", context.Request.Method, context.Request.Path);
            else
                _logger.LogWarning("Requisicao recusada ({Status}) em {Metodo} {Rota}: {Motivo}",
                    status, context.Request.Method, context.Request.Path, ex.Message);

            await EscreverAsync(context, status, titulo,
                status >= StatusCodes.Status500InternalServerError ? "Erro interno do servidor." : ex.Message, erros);
        }
    }

    private static (int Status, string Titulo, object? Erros) Classificar(Exception ex) => ex switch
    {
        ValidationException v when v.Errors.Any() => (StatusCodes.Status400BadRequest, "Erro de validacao",
            v.Errors.Select(e => new { campo = e.PropertyName, mensagem = e.ErrorMessage })),
        ValidationException or DomainException => (StatusCodes.Status400BadRequest, "Regra de negocio violada", null),
        UnauthorizedException => (StatusCodes.Status401Unauthorized, "Nao autenticado", null),
        ForbiddenException => (StatusCodes.Status403Forbidden, "Acesso negado", null),
        NotFoundException => (StatusCodes.Status404NotFound, "Recurso nao encontrado", null),
        ConflictException => (StatusCodes.Status409Conflict, "Conflito", null),
        DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation } } =>
            (StatusCodes.Status409Conflict, "Registro em uso", null),
        DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } =>
            (StatusCodes.Status409Conflict, "Registro duplicado", null),
        _ => (StatusCodes.Status500InternalServerError, "Erro interno", null)
    };

    private static async Task EscreverAsync(HttpContext context, int status, string titulo, string detalhe, object? erros)
    {
        var problema = new ProblemDetails
        {
            Status = status,
            Title = titulo,
            Detail = detalhe,
            Instance = context.Request.Path
        };
        problema.Extensions["correlationId"] = CorrelacaoMiddleware.Obter(context);
        if (erros is not null) problema.Extensions["erros"] = erros;

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problema, options: null, contentType: "application/problem+json");
    }
}
