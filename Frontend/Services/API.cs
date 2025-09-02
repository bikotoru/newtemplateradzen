using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Shared.Models.Responses;

namespace Frontend.Services;

/// <summary>
/// Servicio API genérico para realizar peticiones HTTP con diferentes tipos de retorno
/// Soporta métodos autenticados y no autenticados
/// </summary>
public class API
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public API(HttpClient httpClient, AuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
    }

    #region Métodos GET

    /// <summary>
    /// GET request que retorna string
    /// </summary>
    public async Task<string> GetStringAsync(string endpoint)
    {
        await EnsureAuthenticatedAsync();
        var request = CreateAuthenticatedRequest(HttpMethod.Get, endpoint);
        var response = await _httpClient.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// GET request que retorna string sin autenticación
    /// </summary>
    public async Task<string> GetStringNoAuthAsync(string endpoint)
    {
        var response = await _httpClient.GetAsync(endpoint);
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// GET request que retorna ApiResponse&lt;T&gt;
    /// </summary>
    public async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
    {
        try
        {
            await EnsureAuthenticatedAsync();
            var request = CreateAuthenticatedRequest(HttpMethod.Get, endpoint);
            var response = await _httpClient.SendAsync(request);
            var jsonString = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<T>>(jsonString, JsonOptions);
                return result ?? ApiResponse<T>.ErrorResponse("Respuesta vacía del servidor");
            }
            else
            {
                var errorResponse = TryDeserializeError<T>(jsonString);
                return errorResponse ?? ApiResponse<T>.ErrorResponse($"Error HTTP {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            return ApiResponse<T>.ErrorResponse($"Error de conexión: {ex.Message}");
        }
    }

    /// <summary>
    /// GET request que retorna ApiResponse&lt;T&gt; sin autenticación
    /// </summary>
    public async Task<ApiResponse<T>> GetNoAuthAsync<T>(string endpoint)
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint);
            var jsonString = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<T>>(jsonString, JsonOptions);
                return result ?? ApiResponse<T>.ErrorResponse("Respuesta vacía del servidor");
            }
            else
            {
                var errorResponse = TryDeserializeError<T>(jsonString);
                return errorResponse ?? ApiResponse<T>.ErrorResponse($"Error HTTP {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            return ApiResponse<T>.ErrorResponse($"Error de conexión: {ex.Message}");
        }
    }

    /// <summary>
    /// GET request que retorna T directamente
    /// </summary>
    public async Task<T?> GetDirectAsync<T>(string endpoint)
    {
        try
        {
            await EnsureAuthenticatedAsync();
            var request = CreateAuthenticatedRequest(HttpMethod.Get, endpoint);
            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(jsonString, JsonOptions);
            }
            return default(T);
        }
        catch
        {
            return default(T);
        }
    }

    /// <summary>
    /// GET request que retorna T directamente sin autenticación
    /// </summary>
    public async Task<T?> GetDirectNoAuthAsync<T>(string endpoint)
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint);
            
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(jsonString, JsonOptions);
            }
            return default(T);
        }
        catch
        {
            return default(T);
        }
    }

    #endregion

    #region Métodos POST

    /// <summary>
    /// POST request que retorna string
    /// </summary>
    public async Task<string> PostStringAsync(string endpoint, object? data = null)
    {
        await EnsureAuthenticatedAsync();
        var request = CreateAuthenticatedRequest(HttpMethod.Post, endpoint, data);
        var response = await _httpClient.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// POST request que retorna string sin autenticación
    /// </summary>
    public async Task<string> PostStringNoAuthAsync(string endpoint, object? data = null)
    {
        var content = CreateJsonContent(data);
        var response = await _httpClient.PostAsync(endpoint, content);
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// POST request que retorna ApiResponse&lt;T&gt;
    /// </summary>
    public async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object? data = null)
    {
        try
        {
            await EnsureAuthenticatedAsync();
            var request = CreateAuthenticatedRequest(HttpMethod.Post, endpoint, data);
            var response = await _httpClient.SendAsync(request);
            var jsonString = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<T>>(jsonString, JsonOptions);
                return result ?? ApiResponse<T>.ErrorResponse("Respuesta vacía del servidor");
            }
            else
            {
                var errorResponse = TryDeserializeError<T>(jsonString);
                return errorResponse ?? ApiResponse<T>.ErrorResponse($"Error HTTP {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            return ApiResponse<T>.ErrorResponse($"Error de conexión: {ex.Message}");
        }
    }

    /// <summary>
    /// POST request que retorna ApiResponse&lt;T&gt; sin autenticación
    /// </summary>
    public async Task<ApiResponse<T>> PostNoAuthAsync<T>(string endpoint, object? data = null)
    {
        try
        {
            var content = CreateJsonContent(data);
            var response = await _httpClient.PostAsync(endpoint, content);
            var jsonString = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<T>>(jsonString, JsonOptions);
                return result ?? ApiResponse<T>.ErrorResponse("Respuesta vacía del servidor");
            }
            else
            {
                var errorResponse = TryDeserializeError<T>(jsonString);
                return errorResponse ?? ApiResponse<T>.ErrorResponse($"Error HTTP {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            return ApiResponse<T>.ErrorResponse($"Error de conexión: {ex.Message}");
        }
    }

    /// <summary>
    /// POST request que retorna T directamente
    /// </summary>
    public async Task<T?> PostDirectAsync<T>(string endpoint, object? data = null)
    {
        try
        {
            await EnsureAuthenticatedAsync();
            var request = CreateAuthenticatedRequest(HttpMethod.Post, endpoint, data);
            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(jsonString, JsonOptions);
            }
            return default(T);
        }
        catch
        {
            return default(T);
        }
    }

    /// <summary>
    /// POST request que retorna T directamente sin autenticación
    /// </summary>
    public async Task<T?> PostDirectNoAuthAsync<T>(string endpoint, object? data = null)
    {
        try
        {
            var content = CreateJsonContent(data);
            var response = await _httpClient.PostAsync(endpoint, content);
            
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(jsonString, JsonOptions);
            }
            return default(T);
        }
        catch
        {
            return default(T);
        }
    }

    #endregion

    #region Métodos PUT

    /// <summary>
    /// PUT request que retorna string
    /// </summary>
    public async Task<string> PutStringAsync(string endpoint, object? data = null)
    {
        await EnsureAuthenticatedAsync();
        var request = CreateAuthenticatedRequest(HttpMethod.Put, endpoint, data);
        var response = await _httpClient.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// PUT request que retorna string sin autenticación
    /// </summary>
    public async Task<string> PutStringNoAuthAsync(string endpoint, object? data = null)
    {
        var content = CreateJsonContent(data);
        var response = await _httpClient.PutAsync(endpoint, content);
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// PUT request que retorna ApiResponse&lt;T&gt;
    /// </summary>
    public async Task<ApiResponse<T>> PutAsync<T>(string endpoint, object? data = null)
    {
        try
        {
            await EnsureAuthenticatedAsync();
            var request = CreateAuthenticatedRequest(HttpMethod.Put, endpoint, data);
            var response = await _httpClient.SendAsync(request);
            var jsonString = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<T>>(jsonString, JsonOptions);
                return result ?? ApiResponse<T>.ErrorResponse("Respuesta vacía del servidor");
            }
            else
            {
                var errorResponse = TryDeserializeError<T>(jsonString);
                return errorResponse ?? ApiResponse<T>.ErrorResponse($"Error HTTP {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            return ApiResponse<T>.ErrorResponse($"Error de conexión: {ex.Message}");
        }
    }

    /// <summary>
    /// PUT request que retorna ApiResponse&lt;T&gt; sin autenticación
    /// </summary>
    public async Task<ApiResponse<T>> PutNoAuthAsync<T>(string endpoint, object? data = null)
    {
        try
        {
            var content = CreateJsonContent(data);
            var response = await _httpClient.PutAsync(endpoint, content);
            var jsonString = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<T>>(jsonString, JsonOptions);
                return result ?? ApiResponse<T>.ErrorResponse("Respuesta vacía del servidor");
            }
            else
            {
                var errorResponse = TryDeserializeError<T>(jsonString);
                return errorResponse ?? ApiResponse<T>.ErrorResponse($"Error HTTP {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            return ApiResponse<T>.ErrorResponse($"Error de conexión: {ex.Message}");
        }
    }

    /// <summary>
    /// PUT request que retorna T directamente
    /// </summary>
    public async Task<T?> PutDirectAsync<T>(string endpoint, object? data = null)
    {
        try
        {
            await EnsureAuthenticatedAsync();
            var request = CreateAuthenticatedRequest(HttpMethod.Put, endpoint, data);
            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(jsonString, JsonOptions);
            }
            return default(T);
        }
        catch
        {
            return default(T);
        }
    }

    /// <summary>
    /// PUT request que retorna T directamente sin autenticación
    /// </summary>
    public async Task<T?> PutDirectNoAuthAsync<T>(string endpoint, object? data = null)
    {
        try
        {
            var content = CreateJsonContent(data);
            var response = await _httpClient.PutAsync(endpoint, content);
            
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(jsonString, JsonOptions);
            }
            return default(T);
        }
        catch
        {
            return default(T);
        }
    }

    #endregion

    #region Métodos DELETE

    /// <summary>
    /// DELETE request que retorna string
    /// </summary>
    public async Task<string> DeleteStringAsync(string endpoint)
    {
        await EnsureAuthenticatedAsync();
        var request = CreateAuthenticatedRequest(HttpMethod.Delete, endpoint);
        var response = await _httpClient.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// DELETE request que retorna string sin autenticación
    /// </summary>
    public async Task<string> DeleteStringNoAuthAsync(string endpoint)
    {
        var response = await _httpClient.DeleteAsync(endpoint);
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// DELETE request que retorna ApiResponse&lt;T&gt;
    /// </summary>
    public async Task<ApiResponse<T>> DeleteAsync<T>(string endpoint)
    {
        try
        {
            await EnsureAuthenticatedAsync();
            var request = CreateAuthenticatedRequest(HttpMethod.Delete, endpoint);
            var response = await _httpClient.SendAsync(request);
            var jsonString = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<T>>(jsonString, JsonOptions);
                return result ?? ApiResponse<T>.ErrorResponse("Respuesta vacía del servidor");
            }
            else
            {
                var errorResponse = TryDeserializeError<T>(jsonString);
                return errorResponse ?? ApiResponse<T>.ErrorResponse($"Error HTTP {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            return ApiResponse<T>.ErrorResponse($"Error de conexión: {ex.Message}");
        }
    }

    /// <summary>
    /// DELETE request que retorna ApiResponse&lt;T&gt; sin autenticación
    /// </summary>
    public async Task<ApiResponse<T>> DeleteNoAuthAsync<T>(string endpoint)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(endpoint);
            var jsonString = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<T>>(jsonString, JsonOptions);
                return result ?? ApiResponse<T>.ErrorResponse("Respuesta vacía del servidor");
            }
            else
            {
                var errorResponse = TryDeserializeError<T>(jsonString);
                return errorResponse ?? ApiResponse<T>.ErrorResponse($"Error HTTP {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            return ApiResponse<T>.ErrorResponse($"Error de conexión: {ex.Message}");
        }
    }

    /// <summary>
    /// DELETE request que retorna T directamente
    /// </summary>
    public async Task<T?> DeleteDirectAsync<T>(string endpoint)
    {
        try
        {
            await EnsureAuthenticatedAsync();
            var request = CreateAuthenticatedRequest(HttpMethod.Delete, endpoint);
            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(jsonString, JsonOptions);
            }
            return default(T);
        }
        catch
        {
            return default(T);
        }
    }

    /// <summary>
    /// DELETE request que retorna T directamente sin autenticación
    /// </summary>
    public async Task<T?> DeleteDirectNoAuthAsync<T>(string endpoint)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(endpoint);
            
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(jsonString, JsonOptions);
            }
            return default(T);
        }
        catch
        {
            return default(T);
        }
    }

    #endregion

    #region Métodos que procesan ApiResponse<T>

    /// <summary>
    /// Procesa un ApiResponse y ejecuta una acción si es exitoso, o maneja el error
    /// </summary>
    public async Task ProcessResponseAsync<T>(ApiResponse<T> response, 
        Func<T, Task> onSuccess, 
        Func<ApiResponse<T>, Task>? onError = null)
    {
        if (response.Success && response.Data != null)
        {
            await onSuccess(response.Data);
        }
        else if (onError != null)
        {
            await onError(response);
        }
    }

    /// <summary>
    /// Procesa un ApiResponse y ejecuta una acción síncrona si es exitoso, o maneja el error
    /// </summary>
    public void ProcessResponse<T>(ApiResponse<T> response, 
        Action<T> onSuccess, 
        Action<ApiResponse<T>>? onError = null)
    {
        if (response.Success && response.Data != null)
        {
            onSuccess(response.Data);
        }
        else if (onError != null)
        {
            onError(response);
        }
    }

    /// <summary>
    /// Extrae el dato de un ApiResponse si es exitoso, o retorna un valor por defecto
    /// </summary>
    public T? GetDataOrDefault<T>(ApiResponse<T> response, T? defaultValue = default)
    {
        return response.Success ? response.Data : defaultValue;
    }

    /// <summary>
    /// Extrae el dato de un ApiResponse si es exitoso, o lanza una excepción con el mensaje de error
    /// </summary>
    public T GetDataOrThrow<T>(ApiResponse<T> response)
    {
        if (response.Success && response.Data != null)
        {
            return response.Data;
        }
        
        var errorMessage = response.Message ?? "Error desconocido en la respuesta de la API";
        if (response.Errors?.Any() == true)
        {
            errorMessage += $". Errores: {string.Join(", ", response.Errors)}";
        }
        
        throw new InvalidOperationException(errorMessage);
    }

    /// <summary>
    /// Transforma un ApiResponse<T> en ApiResponse<TResult>
    /// </summary>
    public ApiResponse<TResult> TransformResponse<T, TResult>(ApiResponse<T> response, 
        Func<T, TResult> transform)
    {
        if (!response.Success || response.Data == null)
        {
            return ApiResponse<TResult>.ErrorResponse(
                response.Message ?? "Error en la respuesta", 
                response.Errors);
        }

        try
        {
            var transformedData = transform(response.Data);
            return ApiResponse<TResult>.SuccessResponse(transformedData, response.Message);
        }
        catch (Exception ex)
        {
            return ApiResponse<TResult>.ErrorResponse(
                $"Error transformando datos: {ex.Message}");
        }
    }

    /// <summary>
    /// Transforma un ApiResponse<T> en ApiResponse<TResult> de forma asíncrona
    /// </summary>
    public async Task<ApiResponse<TResult>> TransformResponseAsync<T, TResult>(ApiResponse<T> response, 
        Func<T, Task<TResult>> transform)
    {
        if (!response.Success || response.Data == null)
        {
            return ApiResponse<TResult>.ErrorResponse(
                response.Message ?? "Error en la respuesta", 
                response.Errors);
        }

        try
        {
            var transformedData = await transform(response.Data);
            return ApiResponse<TResult>.SuccessResponse(transformedData, response.Message);
        }
        catch (Exception ex)
        {
            return ApiResponse<TResult>.ErrorResponse(
                $"Error transformando datos: {ex.Message}");
        }
    }

    /// <summary>
    /// Combina múltiples ApiResponse en uno solo
    /// </summary>
    public ApiResponse<List<T>> CombineResponses<T>(params ApiResponse<T>[] responses)
    {
        var successfulData = new List<T>();
        var allErrors = new List<string>();
        var hasFailure = false;

        foreach (var response in responses)
        {
            if (response.Success && response.Data != null)
            {
                successfulData.Add(response.Data);
            }
            else
            {
                hasFailure = true;
                if (!string.IsNullOrEmpty(response.Message))
                {
                    allErrors.Add(response.Message);
                }
                if (response.Errors?.Any() == true)
                {
                    allErrors.AddRange(response.Errors);
                }
            }
        }

        if (hasFailure && successfulData.Count == 0)
        {
            return ApiResponse<List<T>>.ErrorResponse(
                "Todas las operaciones fallaron", 
                allErrors);
        }

        if (hasFailure)
        {
            var combinedResponse = ApiResponse<List<T>>.SuccessResponse(
                successfulData, 
                "Algunas operaciones completadas exitosamente");
            combinedResponse.Errors = allErrors;
            return combinedResponse;
        }

        return ApiResponse<List<T>>.SuccessResponse(
            successfulData, 
            "Todas las operaciones completadas exitosamente");
    }

    /// <summary>
    /// Valida un ApiResponse y retorna los errores en formato string
    /// </summary>
    public string GetErrorMessages<T>(ApiResponse<T> response)
    {
        if (response.Success)
        {
            return string.Empty;
        }

        var messages = new List<string>();
        
        if (!string.IsNullOrEmpty(response.Message))
        {
            messages.Add(response.Message);
        }

        if (response.Errors?.Any() == true)
        {
            messages.AddRange(response.Errors);
        }

        return string.Join(Environment.NewLine, messages);
    }

    /// <summary>
    /// Retorna true si el ApiResponse es exitoso y tiene datos
    /// </summary>
    public bool IsSuccessWithData<T>(ApiResponse<T> response)
    {
        return response.Success && response.Data != null;
    }

    /// <summary>
    /// Retorna true si el ApiResponse tiene errores
    /// </summary>
    public bool HasErrors<T>(ApiResponse<T> response)
    {
        return !response.Success || 
               !string.IsNullOrEmpty(response.Message) || 
               (response.Errors?.Any() == true);
    }

    /// <summary>
    /// Ejecuta una acción solo si el ApiResponse es exitoso
    /// </summary>
    public ApiResponse<T> OnSuccess<T>(ApiResponse<T> response, Action<T> action)
    {
        if (response.Success && response.Data != null)
        {
            action(response.Data);
        }
        return response;
    }

    /// <summary>
    /// Ejecuta una acción solo si el ApiResponse tiene errores
    /// </summary>
    public ApiResponse<T> OnError<T>(ApiResponse<T> response, Action<ApiResponse<T>> action)
    {
        if (!response.Success)
        {
            action(response);
        }
        return response;
    }

    /// <summary>
    /// Ejecuta una acción asíncrona solo si el ApiResponse es exitoso
    /// </summary>
    public async Task<ApiResponse<T>> OnSuccessAsync<T>(ApiResponse<T> response, Func<T, Task> action)
    {
        if (response.Success && response.Data != null)
        {
            await action(response.Data);
        }
        return response;
    }

    /// <summary>
    /// Ejecuta una acción asíncrona solo si el ApiResponse tiene errores
    /// </summary>
    public async Task<ApiResponse<T>> OnErrorAsync<T>(ApiResponse<T> response, Func<ApiResponse<T>, Task> action)
    {
        if (!response.Success)
        {
            await action(response);
        }
        return response;
    }

    #endregion

    #region Métodos auxiliares privados

    private async Task EnsureAuthenticatedAsync()
    {
        await _authService.EnsureInitializedAsync();
    }

    private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string endpoint, object? data = null)
    {
        var request = new HttpRequestMessage(method, endpoint);
        
        // Agregar token de autorización si existe
        var token = _authService.Session?.Token;
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.TryAddWithoutValidation("Authorization", token);
        }
        
        // Agregar contenido para POST/PUT
        if (data != null && (method == HttpMethod.Post || method == HttpMethod.Put))
        {
            request.Content = CreateJsonContent(data);
        }
        
        return request;
    }

    private static StringContent CreateJsonContent(object? data)
    {
        if (data == null) return new StringContent(string.Empty, Encoding.UTF8, "application/json");
        
        var json = JsonSerializer.Serialize(data, JsonOptions);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private static ApiResponse<T>? TryDeserializeError<T>(string jsonString)
    {
        try
        {
            return JsonSerializer.Deserialize<ApiResponse<T>>(jsonString, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region Métodos para archivos binarios

    /// <summary>
    /// POST request que retorna un archivo binario (para descargas de Excel, PDF, etc.)
    /// </summary>
    public async Task<byte[]> PostFileAsync(string endpoint, object? data = null)
    {
        try
        {
            await EnsureAuthenticatedAsync();
            var request = CreateAuthenticatedRequest(HttpMethod.Post, endpoint, data);
            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Error HTTP {response.StatusCode}: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error descargando archivo: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// GET request que retorna un archivo binario
    /// </summary>
    public async Task<byte[]> GetFileAsync(string endpoint)
    {
        try
        {
            await EnsureAuthenticatedAsync();
            var request = CreateAuthenticatedRequest(HttpMethod.Get, endpoint);
            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Error HTTP {response.StatusCode}: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error descargando archivo: {ex.Message}", ex);
        }
    }

    #endregion
}