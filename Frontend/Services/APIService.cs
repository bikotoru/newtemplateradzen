using System.Net.Http.Json;
using System.Text.Json;

namespace Frontend.Services
{
    public class APIService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<APIService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public APIService(HttpClient httpClient, ILogger<APIService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        #region GET Methods

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            try
            {
                _logger.LogInformation("GET request to: {Endpoint}", endpoint);
                var response = await _httpClient.GetAsync(endpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<T>(json, _jsonOptions);
                }
                
                _logger.LogWarning("GET request failed: {StatusCode} - {Endpoint}", response.StatusCode, endpoint);
                return default(T);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GET request to: {Endpoint}", endpoint);
                throw;
            }
        }

        public async Task<List<T>> GetListAsync<T>(string endpoint)
        {
            try
            {
                _logger.LogInformation("GET list request to: {Endpoint}", endpoint);
                var response = await _httpClient.GetAsync(endpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<List<T>>(json, _jsonOptions);
                    return result ?? new List<T>();
                }
                
                _logger.LogWarning("GET list request failed: {StatusCode} - {Endpoint}", response.StatusCode, endpoint);
                return new List<T>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GET list request to: {Endpoint}", endpoint);
                throw;
            }
        }

        #endregion

        #region POST Methods

        public async Task<T?> PostAsync<T>(string endpoint, object? data = null)
        {
            try
            {
                _logger.LogInformation("POST request to: {Endpoint}", endpoint);
                var response = await _httpClient.PostAsJsonAsync(endpoint, data, _jsonOptions);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<T>(json, _jsonOptions);
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("POST request failed: {StatusCode} - {Endpoint} - {Error}", 
                    response.StatusCode, endpoint, errorContent);
                return default(T);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in POST request to: {Endpoint}", endpoint);
                throw;
            }
        }

        public async Task<bool> PostAsync(string endpoint, object? data = null)
        {
            try
            {
                _logger.LogInformation("POST request to: {Endpoint}", endpoint);
                var response = await _httpClient.PostAsJsonAsync(endpoint, data, _jsonOptions);
                
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("POST request failed: {StatusCode} - {Endpoint} - {Error}", 
                    response.StatusCode, endpoint, errorContent);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in POST request to: {Endpoint}", endpoint);
                throw;
            }
        }

        #endregion

        #region PUT Methods

        public async Task<T?> PutAsync<T>(string endpoint, object? data = null)
        {
            try
            {
                _logger.LogInformation("PUT request to: {Endpoint}", endpoint);
                var response = await _httpClient.PutAsJsonAsync(endpoint, data, _jsonOptions);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<T>(json, _jsonOptions);
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("PUT request failed: {StatusCode} - {Endpoint} - {Error}", 
                    response.StatusCode, endpoint, errorContent);
                return default(T);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PUT request to: {Endpoint}", endpoint);
                throw;
            }
        }

        public async Task<bool> PutAsync(string endpoint, object? data = null)
        {
            try
            {
                _logger.LogInformation("PUT request to: {Endpoint}", endpoint);
                var response = await _httpClient.PutAsJsonAsync(endpoint, data, _jsonOptions);
                
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("PUT request failed: {StatusCode} - {Endpoint} - {Error}", 
                    response.StatusCode, endpoint, errorContent);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PUT request to: {Endpoint}", endpoint);
                throw;
            }
        }

        #endregion

        #region DELETE Methods

        public async Task<bool> DeleteAsync(string endpoint)
        {
            try
            {
                _logger.LogInformation("DELETE request to: {Endpoint}", endpoint);
                var response = await _httpClient.DeleteAsync(endpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("DELETE request failed: {StatusCode} - {Endpoint} - {Error}", 
                    response.StatusCode, endpoint, errorContent);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DELETE request to: {Endpoint}", endpoint);
                throw;
            }
        }

        #endregion

        #region Utility Methods

        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GetErrorMessageAsync(HttpResponseMessage response)
        {
            try
            {
                var content = await response.Content.ReadAsStringAsync();
                
                // Try to parse as JSON error response
                try
                {
                    var errorObject = JsonSerializer.Deserialize<JsonElement>(content);
                    if (errorObject.TryGetProperty("message", out var messageProperty))
                    {
                        return messageProperty.GetString() ?? "Unknown error";
                    }
                    if (errorObject.TryGetProperty("error", out var errorProperty))
                    {
                        return errorProperty.GetString() ?? "Unknown error";
                    }
                }
                catch
                {
                    // If not JSON, return raw content
                }
                
                return string.IsNullOrWhiteSpace(content) ? "Unknown error" : content;
            }
            catch
            {
                return "Unknown error";
            }
        }

        #endregion
    }
}