using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Oficina.API.HealthChecks;
using Oficina.API.Middlewares;
using Oficina.Application;
using Oficina.Infrastructure;
using Oficina.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// MVC controllers
builder.Services.AddControllers();

// Camadas Application (casos de uso + validators) e Infrastructure (EF/Postgres, repositorios)
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Swagger / OpenAPI com suporte a Bearer JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Oficina Mecanica API",
        Version = "v1",
        Description = "Sistema Integrado de Atendimento e Execucao de Servicos (MVP - Tech Challenge FIAP)."
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

// AuthN/AuthZ (JWT) sao configurados em AddInfrastructure

// Health checks: liveness valida o processo; readiness tambem valida o PostgreSQL.
builder.Services.AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgresql", tags: new[] { "ready" });

var app = builder.Build();

// Cria/migra o schema e popula seeds no startup (MVP). Em producao, prefira migrations.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OficinaDbContext>();
    await DbInitializer.InitializeAsync(db);
}

// Tratamento global de excecoes -> respostas HTTP consistentes
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
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
