using Frontend.Modules.Categoria;
using Frontend.Modules.Inventario.Core;
using Frontend.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace Frontend.Services;

public static class ServiceRegistry
{
    public static IServiceCollection RegisterAllServices(this IServiceCollection services)
    {
        // Core Services
        services.AddScoped<AuthService>();
        services.AddScoped<AuthStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<AuthStateProvider>());
        services.AddTransient<AuthHttpHandler>();
        services.AddScoped<CryptoService>();
        services.AddScoped<FileDownloadService>();
        services.AddScoped<QueryService>();

        // Module Services
        services.AddScoped<ProductoService>();
        services.AddScoped<CategoriaService>();

        return services;
    }
}