using Frontend.Services;
using Frontend.Modules.Core.Localidades.Regions;
using Frontend.Modules.Admin.SystemUsers;
using Frontend.Modules.Admin.SystemRoles;
using Frontend.Modules.Admin.SystemPermissions;
using Frontend.Modules.Admin.FormDesigner;
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
        services.AddScoped<SystemUserService>();
        services.AddScoped<SystemRoleService>();
        services.AddScoped<SystemPermissionService>();
        services.AddScoped<SystemFormEntityService>();

        return services;
    }
}