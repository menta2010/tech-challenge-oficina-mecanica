using System.Text;
using Amazon.SimpleNotificationService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Oficina.Application.Abstractions;
using Oficina.Infrastructure.Identity;
using Oficina.Infrastructure.Notifications;
using Oficina.Infrastructure.Observabilidade;
using Oficina.Infrastructure.Persistence;
using Oficina.Infrastructure.Persistence.Repositories;

namespace Oficina.Infrastructure;

/// <summary>Registra DbContext, repositorios, UnitOfWork, notificacao, metricas e a autenticacao JWT.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");
        services.AddDbContext<OficinaDbContext>(options => options.UseNpgsql(connectionString));

        // Repositorios + UoW
        services.AddScoped<ILojaRepository, LojaRepository>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IVeiculoRepository, VeiculoRepository>();
        services.AddScoped<IServicoRepository, ServicoRepository>();
        services.AddScoped<IPecaInsumoRepository, PecaInsumoRepository>();
        services.AddScoped<IEstoqueLojaRepository, EstoqueLojaRepository>();
        services.AddScoped<IOrdemServicoRepository, OrdemServicoRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Notificacao: SNS quando o topico esta configurado (nuvem); log no ambiente local
        services.Configure<NotificacoesOptions>(configuration.GetSection("Notificacoes"));
        if (!string.IsNullOrWhiteSpace(configuration["Notificacoes:SnsTopicArn"]))
        {
            services.AddSingleton<IAmazonSimpleNotificationService, AmazonSimpleNotificationServiceClient>();
            services.AddScoped<INotificadorStatusOS, SnsNotificadorStatusOS>();
        }
        else
        {
            services.AddScoped<INotificadorStatusOS, LogNotificadorStatusOS>();
        }

        // Observabilidade de negocio (New Relic custom events + logs estruturados)
        services.AddSingleton<IMetricasOrdemServico, NewRelicMetricasOrdemServico>();

        // Identidade
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        // JWT: mesma chave/issuer/audience usada pela Lambda de autenticacao
        var jwt = new JwtOptions();
        configuration.GetSection("Jwt").Bind(jwt);
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();

        return services;
    }
}
