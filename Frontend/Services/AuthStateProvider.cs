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
        _authService = authService;
        _authService.OnAuthStateChanged += NotifyAuthenticationStateChanged;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        
        try
        {
            // Usar método asíncrono que triggerea la inicialización
            var isAuthenticated = await _authService.IsAuthenticatedAsync();
            
            if (!isAuthenticated)
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }
        catch (Exception ex)
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var session = _authService.Session;
        
        var claims = new List<Claim>();

        // Claims básicos
        if (session != null)
        {
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
            // Estado mínimo autenticado sin contexto completo
            claims.Add(new Claim(ClaimTypes.NameIdentifier, "loading"));
            claims.Add(new Claim(ClaimTypes.Name, "Loading..."));
        }

        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

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