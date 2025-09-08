using Frontend.Services;
using Frontend.Modules.Ventas.Ventas;
using Frontend.Modules.Catalogo.Productos;
using Frontend.Modules.Catalogo.Categorias;
using Frontend.Modules.Catalogo.Marcas;
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
        services.AddScoped<VentaService>();
        services.AddScoped<ProductoService>();
        services.AddScoped<CategoriaService>();
        services.AddScoped<MarcaService>();

        return services;
    }
}