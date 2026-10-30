using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Oficina.API.HealthChecks;
using Oficina.API.Middlewares;
using Oficina.API.Seguranca;
using Oficina.Application;
using Oficina.Application.Abstractions;
using Oficina.Infrastructure;
using Oficina.Infrastructure.Persistence;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// Logs estruturados em JSON no stdout (coletados pelo New Relic), enriquecidos com CorrelationId
builder.Host.UseSerilog((context, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("service", "oficina-api")
    .Enrich.WithProperty("environment", context.HostingEnvironment.EnvironmentName)
    .WriteTo.Console(new RenderedCompactJsonFormatter()));

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

// Camadas Application (casos de uso + validators) e Infrastructure (EF/Postgres, SNS, New Relic, JWT)
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IUsuarioAtual, UsuarioAtual>();
builder.Services.Configure<WebhookOptions>(builder.Configuration.GetSection("ExternalApproval"));

// Swagger / OpenAPI com suporte a Bearer JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Oficina Mecanica API",
        Version = "v1",
        Description = "Ordens de servico da rede de oficinas (Tech Challenge FIAP - Fase 3). " +
                      "Obtenha o token em POST /auth/cpf (cliente) ou POST /auth/login (funcionario) no API Gateway."
    });

    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe o token JWT. Ex.: 'Bearer {token}'",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    options.AddSecurityDefinition("Bearer", jwtScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtScheme, Array.Empty<string>() }
    });
});

// Health checks: liveness valida o processo; readiness tambem valida o PostgreSQL.
builder.Services.AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgresql", tags: new[] { "ready" });

var app = builder.Build();

// Migrations + seed. No Kubernetes rodam uma unica vez num Job antes do deploy (argumento --migrar),
// evitando que varias replicas migrem ao mesmo tempo; localmente rodam no startup.
var somenteMigrar = args.Contains("--migrar");
if (somenteMigrar || app.Configuration.GetValue("Banco:MigrarNoStartup", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<OficinaDbContext>();
    await DbInitializer.InitializeAsync(db);
    if (somenteMigrar)
    {
        app.Logger.LogInformation("Migrations e seed aplicados; encerrando (modo --migrar).");
        return;
    }
}

app.UseMiddleware<CorrelacaoMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    // Uma linha por requisicao com rota, status e duracao (latencia) - base dos dashboards de API
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} respondeu {StatusCode} em {Elapsed:0.0} ms";
    options.GetLevel = (http, _, ex) => ex is not null || http.Response.StatusCode >= 500
        ? Serilog.Events.LogEventLevel.Error
        : http.Request.Path.StartsWithSegments("/health")
            ? Serilog.Events.LogEventLevel.Verbose
            : Serilog.Events.LogEventLevel.Information;
});
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Configuration.GetValue("Swagger:Habilitado", true))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
// Mantem compatibilidade com clientes que ainda utilizam o endpoint original.
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

await app.RunAsync();

// Necessario para WebApplicationFactory nos testes de integracao
public partial class Program
{
    protected Program() { }
}
