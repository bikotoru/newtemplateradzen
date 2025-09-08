using Backend.Modules.Auth.Login;
using Backend.Modules.Catalogo.Marcas;
using Backend.Modules.Catalogo.Categorias;
using Backend.Modules.Catalogo.Productos;
using Backend.Modules.Ventas.Ventas;
using Backend.Utils.Security;

namespace Backend.Services;

public static class ServiceRegistry
{
    public static IServiceCollection RegisterAllServices(this IServiceCollection services)
    {
        // Auth Services
        services.AddScoped<LoginService>();
        
        // Module Services
        services.AddScoped<VentaService>();
        services.AddScoped<ProductoService>();
        services.AddScoped<CategoriaService>();
        services.AddScoped<MarcaService>();

        
        // Utils Services
        services.AddScoped<TokenCacheService>();
        
        return services;
    }
}
