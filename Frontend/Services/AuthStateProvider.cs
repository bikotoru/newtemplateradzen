using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Frontend.Services;

/// <summary>
/// AuthenticationStateProvider que usa AuthService
/// </summary>
public class AuthStateProvider : AuthenticationStateProvider
{
    private readonly AuthService _authService;

    public AuthStateProvider(AuthService authService)
    {
        Console.WriteLine("🔐 AuthStateProvider constructor iniciado");
        _authService = authService;
        _authService.OnAuthStateChanged += NotifyAuthenticationStateChanged;
        Console.WriteLine("🔐 AuthStateProvider constructor completado");
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        Console.WriteLine("🔐 AuthStateProvider.GetAuthenticationStateAsync iniciado");
        
        try
        {
            // Usar método asíncrono que triggerea la inicialización
            var isAuthenticated = await _authService.IsAuthenticatedAsync();
            Console.WriteLine($"🔐 AuthStateProvider: isAuthenticated (async) = {isAuthenticated}");
            
            if (!isAuthenticated)
            {
                Console.WriteLine("🔐 AuthStateProvider: Usuario no autenticado");
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ AuthStateProvider: Error verificando autenticación: {ex.Message}");
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var session = _authService.Session;
        Console.WriteLine($"🔐 AuthStateProvider: session = {(session != null ? "presente" : "null")}");
        
        var claims = new List<Claim>();

        // Claims básicos
        if (session != null)
        {
            Console.WriteLine("🔐 AuthStateProvider: Agregando claims de usuario");
            claims.Add(new Claim(ClaimTypes.NameIdentifier, session.Id.ToString()));
            claims.Add(new Claim(ClaimTypes.Name, session.Email ?? ""));
            claims.Add(new Claim(ClaimTypes.GivenName, session.Nombre ?? ""));
            claims.Add(new Claim("organization_id", session.OrganizationId?.ToString() ?? ""));
            claims.Add(new Claim("organization_name", session.OrganizationName ?? ""));

            // Agregar permisos como claims
            if (session.Permisos != null)
            {
                foreach (var permission in session.Permisos)
                {
                    if (!string.IsNullOrEmpty(permission))
                        claims.Add(new Claim("permission", permission));
                }
            }

            // Agregar roles como claims
            if (session.Roles != null)
            {
                foreach (var role in session.Roles)
                {
                    if (!string.IsNullOrEmpty(role?.Nombre))
                        claims.Add(new Claim(ClaimTypes.Role, role.Nombre));
                    if (role?.Id != Guid.Empty)
                        claims.Add(new Claim("role_id", role.Id.ToString()));
                }
            }
        }
        else
        {
            Console.WriteLine("🔐 AuthStateProvider: Usando claims mínimos");
            // Estado mínimo autenticado sin contexto completo
            claims.Add(new Claim(ClaimTypes.NameIdentifier, "loading"));
            claims.Add(new Claim(ClaimTypes.Name, "Loading..."));
        }

        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        Console.WriteLine($"🔐 AuthStateProvider: Retornando AuthenticationState con {claims.Count} claims");
        return new AuthenticationState(user);
    }

    private void NotifyAuthenticationStateChanged()
    {
        //NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void MarkUserAsAuthenticated()
    {
        NotifyAuthenticationStateChanged();
    }

    public void MarkUserAsLoggedOut()
    {
        NotifyAuthenticationStateChanged();
    }
}