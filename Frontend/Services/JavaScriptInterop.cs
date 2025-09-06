using Microsoft.JSInterop;

namespace Frontend.Services;

/// <summary>
/// Servicio para interacción entre JavaScript y Blazor
/// </summary>
public static class JavaScriptInterop
{
    private static AuthService? _authService;
    
    /// <summary>
    /// Inicializa la referencia al AuthService (llamado desde Program.cs)
    /// </summary>
    public static void Initialize(AuthService authService)
    {
        _authService = authService;
    }
    
    /// <summary>
    /// Método llamado desde JavaScript para inicializar la autenticación con el token real del backend
    /// </summary>
    [JSInvokable("InitializeAuthFromJavaScript")]
    public static async Task<bool> InitializeAuthFromJavaScript(string token)
    {
        try
        {
            
            if (_authService == null)
            {
                return false;
            }
            
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }
            
            // Configurar el token en el AuthService
            await _authService.SetTokenAsync(token);
            
            // Cargar el contexto de usuario desde el servidor
            await _authService.InitializeAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
}