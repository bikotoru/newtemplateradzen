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
        Console.WriteLine("🔗 JavaScriptInterop inicializado con AuthService");
    }
    
    /// <summary>
    /// Método llamado desde JavaScript para inicializar la autenticación con el token real del backend
    /// </summary>
    [JSInvokable("InitializeAuthFromJavaScript")]
    public static async Task<bool> InitializeAuthFromJavaScript(string token)
    {
        try
        {
            Console.WriteLine("📞 InitializeAuthFromJavaScript llamado desde JavaScript");
            Console.WriteLine($"📞 Token recibido: {(!string.IsNullOrEmpty(token) ? $"{token.Substring(0, Math.Min(20, token.Length))}..." : "null")}");
            
            if (_authService == null)
            {
                Console.WriteLine("❌ AuthService no está inicializado");
                return false;
            }
            
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("❌ Token vacío recibido");
                return false;
            }
            
            // Configurar el token en el AuthService
            await _authService.SetTokenAsync(token);
            
            // Cargar el contexto de usuario desde el servidor
            await _authService.InitializeAsync();
            
            Console.WriteLine("✅ AuthService inicializado correctamente desde JavaScript");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error inicializando AuthService desde JavaScript: {ex.Message}");
            Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
            return false;
        }
    }
}