using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Application.Clientes;
using Oficina.Application.Estoque;
using Oficina.Application.Servicos;
using Oficina.Application.Identidade;
using Oficina.Application.OrdensServico;
using Oficina.Application.Veiculos;

namespace Oficina.Application;

/// <summary>Registra os casos de uso (services) e os validators da camada Application.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<ClienteService>();
        services.AddScoped<VeiculoService>();
        services.AddScoped<ServicoService>();
        services.AddScoped<PecaInsumoService>();
        services.AddScoped<AuthService>();
        services.AddScoped<OrdemServicoService>();

        return services;
    }
}
