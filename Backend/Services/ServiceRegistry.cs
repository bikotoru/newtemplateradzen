using Backend.Modules.Categoria;
using Backend.Modules.Auth.Login;
using Backend.Utils.Services;
using Backend.Utils.Security;

namespace Backend.Services;

public static class ServiceRegistry
{
    public static IServiceCollection RegisterAllServices(this IServiceCollection services)
    {
        // Auth Services
        services.AddScoped<LoginService>();
        
        // Module Services
        services.AddScoped<CategoriaService>();
        
        // Utils Services
        services.AddScoped<TokenCacheService>();
        
        return services;
    }
}