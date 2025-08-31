using Microsoft.JSInterop;
using Shared.Models.DTOs.Auth;
using System.Security.Claims;
using System.Text.Json;

namespace Frontend.Services;

/// <summary>
/// Servicio de autenticación que maneja autenticación, permisos, roles y estado del usuario
/// </summary>
public class AuthService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly HttpClient _httpClient;
    
    // Estado interno
    private bool _isAuthenticated = false;
    private bool _isInitialized = false;
    private string _authToken = string.Empty;
    private SessionDataDto? _session;
    
    // Eventos
    public event Action? OnAuthStateChanged;
    
    // Propiedades públicas - completamente lazy, sin bloqueos
    public bool IsAuthenticated 
    {
        get 
        {
            // Verificar que tenemos una sesión válida con OrganizationId
            var isAuthenticated = !string.IsNullOrEmpty(_authToken) && 
                                _session != null && 
                                _session.Organization != null && 
                                _session.Organization.Id != Guid.Empty;
                                
            return isAuthenticated;
        }
    }
    
    public string AuthToken => _authToken;
    public SessionDataDto? Session => _session;

    public AuthService(IJSRuntime jsRuntime, HttpClient httpClient)
    {
        _jsRuntime = jsRuntime;
        _httpClient = httpClient;
    }

    #region Inicialización

    /// <summary>
    /// Inicializa el servicio de forma lazy cuando se necesite
    /// </summary>
    private async Task EnsureInitializedAsync()
    {
        if (_isInitialized) 
        {
            return;
        }


        try
        {
            // Verificar si hay token en localStorage (persistente)
            var localToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");

            if (!string.IsNullOrEmpty(localToken))
            {
                
                // Configurar token
                _authToken = localToken;
                
                // Cargar contexto desde servidor usando /api/auth/me
                await LoadUserContextFromServer();
                
                NotifyAuthStateChanged();
            }
            else 
            {
            }
            
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            _isInitialized = true; // Marcar como inicializado para evitar loops
        }
    }

    /// <summary>
    /// Método público para inicialización manual (opcional)
    /// </summary>
    public async Task InitializeAsync()
    {
        await EnsureInitializedAsync();
    }

    /// <summary>
    /// Versión async para verificar estado de autenticación
    /// </summary>
    public async Task<bool> IsAuthenticatedAsync()
    {
        await EnsureInitializedAsync();
        
        // Verificar que tenemos una sesión válida con OrganizationId
        var isAuthenticated = !string.IsNullOrEmpty(_authToken) && 
                            _session != null && 
                            _session.Organization != null && 
                            _session.Organization.Id != Guid.Empty;

        if (!isAuthenticated)
        {
            await LoadUserContextFromServer();
        }
        
        return isAuthenticated;
    }

    #endregion

    #region Autenticación

    /// <summary>
    /// Marca el usuario como autenticado y carga su contexto
    /// </summary>
    public async Task SetAuthenticatedAsync(string token, string? encryptedData = null)
    {
        _authToken = token;
        _isAuthenticated = true;

        if (!string.IsNullOrEmpty(encryptedData))
        {
            //await LoadUserContextFromEncryptedData(encryptedData);
        }

        NotifyAuthStateChanged();
    }

    /// <summary>
    /// Cierra la sesión del usuario
    /// </summary>
    public async Task LogoutAsync()
    {
        try
        {
            // Limpiar estado interno
            _isAuthenticated = false;
            _authToken = string.Empty;
            _session = null;

            // Limpiar almacenamiento
            await _jsRuntime.InvokeVoidAsync("sessionStorage.clear");
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "rememberSession");

            NotifyAuthStateChanged();

            // Recargar página para volver al login
            await _jsRuntime.InvokeVoidAsync("location.reload");
        }
        catch (Exception ex)
        {
            // Forzar recarga de página como fallback
            await _jsRuntime.InvokeVoidAsync("location.reload");
        }
    }

    #endregion

    #region Gestión de Contexto de Usuario

    /// <summary>
    /// Carga el contexto de usuario desde datos encriptados del backend usando JavaScript
    /// </summary>
    private async Task LoadUserContextFromEncryptedData(string encryptedData)
    {
        try
        {
            
            // Primero asegurar que el objeto unifiedCrypto esté disponible
            await _jsRuntime.InvokeVoidAsync("eval", Shared.Models.Security.UnifiedEncryption.GetJavaScriptEncryptionCode());
            
            // Desencriptar usando JavaScript SubtleCrypto
            var decryptedJson = await _jsRuntime.InvokeAsync<string>("unifiedCrypto.decrypt", encryptedData);
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var userData = JsonSerializer.Deserialize<SessionDataDto>(decryptedJson, options);
            
            if (userData != null)
            {
                _session = userData;
            }
            else
            {
            }
        }
        catch (Exception ex)
        {
            // Intentar cargar datos del servidor como fallback
            await LoadUserContextFromServer();
        }
    }

    /// <summary>
    /// Carga el contexto de usuario desde el servidor usando /api/auth/me
    /// </summary>
    private async Task LoadUserContextFromServer()
    {
        try
        {
            if (string.IsNullOrEmpty(_authToken)) 
            {
                return;
            }

            
            var request = new HttpRequestMessage(HttpMethod.Get, $"{Variables.URLBackend}/api/auth/me");
            
            // El token encriptado contiene caracteres especiales - usar TryAddWithoutValidation
            request.Headers.TryAddWithoutValidation("Authorization", _authToken);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                
                // El servidor devuelve: { "Token": "...", "Expired": "...", "Data": "encrypted_data" }
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var authResponse = JsonSerializer.Deserialize<AuthResponse>(json, options);

                if (authResponse != null && !string.IsNullOrEmpty(authResponse.Data))
                {
                    await LoadUserContextFromEncryptedData(authResponse.Data);
                }
                else
                {
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
            }
        }
        catch (Exception ex)
        {
        }
    }

    /// <summary>
    /// Modelo para la respuesta del endpoint /api/auth/me
    /// </summary>
    private class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expired { get; set; }
        public string Data { get; set; } = string.Empty;
    }

    #endregion

    #region Gestión de Permisos

    /// <summary>
    /// Verifica si el usuario tiene un permiso específico
    /// </summary>
    public bool HasPermission(string permission)
    {
        if (!_isAuthenticated || _session?.Permisos.Count == 0) return false;

        // Verificar permiso exacto
        if (_session.Permisos.Contains(permission, StringComparer.OrdinalIgnoreCase))
            return true;

        // Verificar wildcard: si tiene "categoria.*" y busca "categoria.algo"
        if (permission.Contains('.'))
        {
            var parts = permission.Split('.');
            if (parts.Length == 2)
            {
                var wildcardPermission = $"{parts[0]}.*";
                if (_session.Permisos.Contains(wildcardPermission, StringComparer.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Verifica si el usuario tiene al menos uno de los permisos especificados
    /// </summary>
    public bool HasAnyPermission(params string[] permissions)
    {
        if (!_isAuthenticated || _session?.Permisos.Count == 0) return false;
        return permissions.Any(p => HasPermission(p));
    }

    /// <summary>
    /// Obtiene todos los permisos del usuario
    /// </summary>
    public List<string> GetAllUserPermissions()
    {
        return _session?.Permisos.ToList() ?? new List<string>();
    }

    /// <summary>
    /// Versión async de GetAllUserPermissions
    /// </summary>
    public async Task<List<string>> GetAllUserPermissionsAsync()
    {
        await EnsureInitializedAsync();
        
        // Asegurar que el contexto esté cargado
        if (_session == null && _isAuthenticated)
        {
            await LoadUserContextFromServer();
        }
        return _session?.Permisos.ToList() ?? new List<string>();
    }

    /// <summary>
    /// Versión async de HasPermission
    /// </summary>
    public async Task<bool> HasPermissionAsync(string permission)
    {
        // Asegurar que el contexto esté cargado
        if (_session == null && _isAuthenticated)
        {
            await LoadUserContextFromServer();
        }
        return HasPermission(permission);
    }

    /// <summary>
    /// Versión async de HasAnyPermission
    /// </summary>
    public async Task<bool> HasAnyPermissionAsync(params string[] permissions)
    {
        // Asegurar que el contexto esté cargado
        if (_session == null && _isAuthenticated)
        {
            await LoadUserContextFromServer();
        }
        return HasAnyPermission(permissions);
    }

    // Métodos de conveniencia - mantener compatibilidad con código existente
    public bool HasPermission(string action, string entity) => HasPermission($"{entity}.{action}");
    public async Task<bool> HasPermissionAsync(string action, string entity) => await HasPermissionAsync($"{entity}.{action}");
    public bool HasPermissionByName(string permissionName) => HasPermission(permissionName);
    public async Task<bool> HasPermissionByNameAsync(string permissionName) => await HasPermissionAsync(permissionName);

    #endregion

    #region Gestión de Roles

    /// <summary>
    /// Verifica si el usuario tiene un rol específico
    /// </summary>
    public bool HasRole(string roleName)
    {
        if (!_isAuthenticated || _session?.Roles.Count == 0) return false;
        return _session.Roles.Any(r => string.Equals(r.Nombre, roleName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifica si el usuario es SuperAdmin
    /// </summary>
    public bool IsSuperAdmin()
    {
        return HasRole("SuperAdmin") || HasRole("Super Admin") || HasRole("Administrator");
    }

    /// <summary>
    /// Verifica si el usuario es Admin de organización
    /// </summary>
    public bool IsOrganizationAdmin()
    {
        return HasRole("Admin") || HasRole("OrganizationAdmin") || IsSuperAdmin();
    }

    /// <summary>
    /// Obtiene todos los roles del usuario
    /// </summary>
    public List<string> GetUserRoles()
    {
        return _session?.Roles.Select(r => r.Nombre).ToList() ?? new List<string>();
    }

    /// <summary>
    /// Versión async de HasRole
    /// </summary>
    public async Task<bool> HasRoleAsync(string roleName)
    {
        // Asegurar que el contexto esté cargado
        if (_session == null && _isAuthenticated)
        {
            await LoadUserContextFromServer();
        }
        return HasRole(roleName);
    }

    /// <summary>
    /// Versión async de GetUserRoles
    /// </summary>
    public async Task<List<string>> GetUserRolesAsync()
    {
        // Asegurar que el contexto esté cargado
        if (_session == null && _isAuthenticated)
        {
            await LoadUserContextFromServer();
        }
        return _session.Roles.Select(r => r.Nombre).ToList();
    }

    #endregion

    #region Token Management (para AuthHttpHandler)

    /// <summary>
    /// Obtiene el token de autenticación actual
    /// </summary>
    public async Task<string?> GetTokenAsync()
    {
        if (!string.IsNullOrEmpty(_authToken))
            return _authToken;

        // Intentar cargar desde sessionStorage si no está en memoria
        try
        {
            var sessionToken = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", "authToken");
            if (!string.IsNullOrEmpty(sessionToken))
            {
                _authToken = sessionToken;
                return _authToken;
            }
        }
        catch (Exception ex)
        {
        }

        return null;
    }

    /// <summary>
    /// Establece el token de autenticación
    /// </summary>
    public async Task SetTokenAsync(string token)
    {
        _authToken = token;
        _isAuthenticated = !string.IsNullOrEmpty(token);

        try
        {
            await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "authToken", token);
            await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "isAuthenticated", "true");
        }
        catch (Exception ex)
        {
        }

        NotifyAuthStateChanged();
    }

    /// <summary>
    /// Elimina el token de autenticación
    /// </summary>
    public async Task RemoveTokenAsync()
    {
        _authToken = string.Empty;
        _isAuthenticated = false;
        _session = null;

        try
        {
            await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "authToken");
            await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "isAuthenticated");
            await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "sessionData");
        }
        catch (Exception ex)
        {
        }

        NotifyAuthStateChanged();
    }

    #endregion

    #region Utilidades

    /// <summary>
    /// Obtiene el nombre de display del usuario
    /// </summary>
    public string GetDisplayName()
    {

        if (_session == null)
        {
            return "Usuario";
        }


        if (!string.IsNullOrWhiteSpace(_session.Nombre))
        {
            return _session.Nombre;
        }

        if (!string.IsNullOrWhiteSpace(_session.Email))
        {
            return _session.Email;
        }

        return "Usuario";
    }

    /// <summary>
    /// Notifica cambios en el estado de autenticación
    /// </summary>
    private void NotifyAuthStateChanged()
    {
        OnAuthStateChanged?.Invoke();
    }

    #endregion
}