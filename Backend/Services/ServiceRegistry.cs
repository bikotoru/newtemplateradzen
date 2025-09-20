using Backend.Modules.Auth.Login;
using Backend.Modules.Admin.SystemPermissions;
using Backend.Modules.Admin.SystemRoles;
using Backend.Modules.Admin.SystemUsers;
using Backend.Modules.Core.Localidades.Regions;
using Backend.Utils.Security;
using Backend.Utils.Services;

namespace Backend.Services;

public static class ServiceRegistry
{
    public static IServiceCollection RegisterAllServices(this IServiceCollection services)
    {
        // Auth Services
        services.AddScoped<LoginService>();
        
        // Module Services
        services.AddScoped<RegionService>();
        services.AddScoped<SystemUserService>();
        services.AddScoped<SystemRoleService>();
        services.AddScoped<SystemPermissionService>();

        
        // Utils Services
        services.AddScoped<TokenCacheService>();
        services.AddScoped<DatabaseMigrationService>();
        
        return services;
    }
}
