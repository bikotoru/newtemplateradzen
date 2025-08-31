using System.Net;
using System.Net.Http.Headers;

namespace Frontend.Services;

/// <summary>
/// Handler HTTP personalizado que automaticamente inyecta tokens de autenticación
/// en todas las requests y maneja respuestas de autenticación
/// </summary>
public class AuthHttpHandler : DelegatingHandler
{
    private readonly AuthService _authService;

    public AuthHttpHandler(AuthService authService)
    {
        _authService = authService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            // Obtener token de autenticación
            var token = await _authService.GetTokenAsync();

            // Inyectar token si está disponible y la request no es de login
            if (!string.IsNullOrEmpty(token) && !IsLoginRequest(request))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Enviar request
            var response = await base.SendAsync(request, cancellationToken);

            // Manejar respuestas de autenticación
            await HandleAuthenticationResponseAsync(response, request);

            return response;
        }
        catch (HttpRequestException ex)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// Verifica si la request es de login (no necesita token)
    /// </summary>
    private static bool IsLoginRequest(HttpRequestMessage request)
    {
        var uri = request.RequestUri?.PathAndQuery?.ToLowerInvariant();
        return uri?.Contains("/auth/login") == true ||
               uri?.Contains("/auth/validate") == true ||
               uri?.Contains("/auth/select-organization") == true;
    }

    /// <summary>
    /// Maneja respuestas relacionadas con autenticación
    /// </summary>
    private async Task HandleAuthenticationResponseAsync(HttpResponseMessage response, HttpRequestMessage request)
    {
        try
        {
            switch (response.StatusCode)
            {
                case HttpStatusCode.Unauthorized: // 401
                    await HandleUnauthorizedAsync();
                    break;

                case HttpStatusCode.Forbidden: // 403
                    await HandleForbiddenAsync();
                    break;

                case HttpStatusCode.OK when IsTokenRefreshResponse(response):
                    // Si la respuesta incluye un token actualizado, guardarlo
                    await HandleTokenRefreshAsync(response);
                    break;
            }
        }
        catch (Exception ex)
        {
        }
    }

    /// <summary>
    /// Maneja respuestas 401 (no autorizado)
    /// </summary>
    private async Task HandleUnauthorizedAsync()
    {
        try
        {
            
            // Limpiar estado de autenticación
            await _authService.RemoveTokenAsync();
            
            // El AuthStateProvider notificará automáticamente el cambio
            // y la aplicación redirigirá al login
        }
        catch (Exception ex)
        {
        }
    }

    /// <summary>
    /// Maneja respuestas 403 (prohibido - falta permisos)
    /// </summary>
    private async Task HandleForbiddenAsync()
    {
        try
        {
            
            // En este caso no limpiamos la sesión, solo logueamos
            // La aplicación puede mostrar una página de "Sin permisos"
        }
        catch (Exception ex)
        {
        }
    }

    /// <summary>
    /// Verifica si la respuesta contiene un token actualizado
    /// </summary>
    private static bool IsTokenRefreshResponse(HttpResponseMessage response)
    {
        // Verificar si hay header personalizado que indica token actualizado
        return response.Headers.Contains("X-Token-Refreshed") ||
               response.Headers.Contains("X-New-Token");
    }

    /// <summary>
    /// Maneja actualización automática de tokens
    /// </summary>
    private async Task HandleTokenRefreshAsync(HttpResponseMessage response)
    {
        try
        {
            // Buscar nuevo token en headers
            if (response.Headers.TryGetValues("X-New-Token", out var tokenValues))
            {
                var newToken = tokenValues.FirstOrDefault();
                if (!string.IsNullOrEmpty(newToken))
                {
                    await _authService.SetTokenAsync(newToken);
                }
            }
        }
        catch (Exception ex)
        {
        }
    }
}