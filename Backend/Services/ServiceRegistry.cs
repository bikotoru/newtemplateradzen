using Backend.Modules.Auth.Login;
using Backend.Modules.Admin.SystemPermissions;
using Backend.Modules.Admin.SystemRoles;
using Backend.Modules.Admin.SystemUsers;
using Backend.Utils.Security;

namespace Backend.Services;

public static class ServiceRegistry
{
    public static IServiceCollection RegisterAllServices(this IServiceCollection services)
    {
        // Auth Services
        services.AddScoped<LoginService>();
        
        // Module Services
        services.AddScoped<SystemUserService>();
        services.AddScoped<SystemRoleService>();
        services.AddScoped<SystemPermissionService>();

        
        // Utils Services
        services.AddScoped<TokenCacheService>();
        
        return services;
    }
}
