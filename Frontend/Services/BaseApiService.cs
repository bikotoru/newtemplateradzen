using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using Shared.Models.Requests;
using Shared.Models.Responses;
using Shared.Models.QueryModels;
using Shared.Models.Services;

namespace Frontend.Services
{
    public abstract class BaseApiService<T> where T : class
    {
        protected readonly HttpClient _httpClient;
        protected readonly ILogger<BaseApiService<T>> _logger;
        protected readonly string _baseUrl;
        protected readonly JsonSerializerOptions _jsonOptions;
        private readonly QueryService _queryService;

        protected BaseApiService(HttpClient httpClient, ILogger<BaseApiService<T>> logger, string baseUrl)
        {
            _httpClient = httpClient;
            _logger = logger;
            _baseUrl = baseUrl.TrimEnd('/');
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
            _queryService = new QueryService(httpClient);
        }

        #region Individual Operations (SEALED - No Override)

        /// <summary>
        /// Crear una entidad individual
        /// </summary>
        public virtual async Task<ApiResponse<T>> CreateAsync(CreateRequest<T> request)
        {
            try
            {
                _logger.LogInformation($"Creating {typeof(T).Name}");

                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/create", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<T>>(responseContent, _jsonOptions);
                    return result ?? ApiResponse<T>.ErrorResponse("Invalid response format");
                }

                _logger.LogError($"Error creating {typeof(T).Name}: {response.StatusCode} - {responseContent}");
                return ApiResponse<T>.ErrorResponse($"Error creating {typeof(T).Name}: {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception creating {typeof(T).Name}");
                return ApiResponse<T>.ErrorResponse($"Exception creating {typeof(T).Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Actualizar una entidad individual
        /// </summary>
        public virtual async Task<ApiResponse<T>> UpdateAsync(UpdateRequest<T> request)
        {
            try
            {
                _logger.LogInformation($"Updating {typeof(T).Name}");

                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{_baseUrl}/update", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<T>>(responseContent, _jsonOptions);
                    return result ?? ApiResponse<T>.ErrorResponse("Invalid response format");
                }

                _logger.LogError($"Error updating {typeof(T).Name}: {response.StatusCode} - {responseContent}");
                return ApiResponse<T>.ErrorResponse($"Error updating {typeof(T).Name}: {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception updating {typeof(T).Name}");
                return ApiResponse<T>.ErrorResponse($"Exception updating {typeof(T).Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtener todos los registros (paginado por defecto)
        /// </summary>
        public virtual async Task<ApiResponse<PagedResponse<T>>> GetAllPagedAsync(int page = 1, int pageSize = 10)
        {
            try
            {
                _logger.LogInformation($"Getting paged {typeof(T).Name} - Page: {page}, PageSize: {pageSize}");

                var response = await _httpClient.GetAsync($"{_baseUrl}/all?page={page}&pageSize={pageSize}&all=false");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<PagedResponse<T>>>(responseContent, _jsonOptions);
                    return result ?? ApiResponse<PagedResponse<T>>.ErrorResponse("Invalid response format");
                }

                _logger.LogError($"Error getting paged {typeof(T).Name}: {response.StatusCode} - {responseContent}");
                return ApiResponse<PagedResponse<T>>.ErrorResponse($"Error getting {typeof(T).Name}: {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception getting paged {typeof(T).Name}");
                return ApiResponse<PagedResponse<T>>.ErrorResponse($"Exception getting {typeof(T).Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtener todos los registros sin paginación
        /// </summary>
        public virtual async Task<ApiResponse<List<T>>> GetAllUnpagedAsync()
        {
            try
            {
                _logger.LogInformation($"Getting all {typeof(T).Name} (unpaged)");

                var response = await _httpClient.GetAsync($"{_baseUrl}/all?all=true");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<List<T>>>(responseContent, _jsonOptions);
                    return result ?? ApiResponse<List<T>>.ErrorResponse("Invalid response format");
                }

                _logger.LogError($"Error getting all {typeof(T).Name}: {response.StatusCode} - {responseContent}");
                return ApiResponse<List<T>>.ErrorResponse($"Error getting {typeof(T).Name}: {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception getting all {typeof(T).Name}");
                return ApiResponse<List<T>>.ErrorResponse($"Exception getting {typeof(T).Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtener por ID
        /// </summary>
        public virtual async Task<ApiResponse<T>> GetByIdAsync(Guid id)
        {
            try
            {
                _logger.LogInformation($"Getting {typeof(T).Name} by ID: {id}");

                var response = await _httpClient.GetAsync($"{_baseUrl}/{id}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<T>>(responseContent, _jsonOptions);
                    return result ?? ApiResponse<T>.ErrorResponse("Invalid response format");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return ApiResponse<T>.ErrorResponse($"{typeof(T).Name} with ID {id} not found");
                }

                _logger.LogError($"Error getting {typeof(T).Name} by ID: {response.StatusCode} - {responseContent}");
                return ApiResponse<T>.ErrorResponse($"Error getting {typeof(T).Name}: {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception getting {typeof(T).Name} by ID: {id}");
                return ApiResponse<T>.ErrorResponse($"Exception getting {typeof(T).Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Eliminar por ID
        /// </summary>
        public virtual async Task<ApiResponse<bool>> DeleteAsync(Guid id)
        {
            try
            {
                _logger.LogInformation($"Deleting {typeof(T).Name} with ID: {id}");

                var response = await _httpClient.DeleteAsync($"{_baseUrl}/{id}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<bool>>(responseContent, _jsonOptions);
                    return result ?? ApiResponse<bool>.ErrorResponse("Invalid response format");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return ApiResponse<bool>.ErrorResponse($"{typeof(T).Name} with ID {id} not found");
                }

                _logger.LogError($"Error deleting {typeof(T).Name} with ID {id}: {response.StatusCode} - {responseContent}");
                return ApiResponse<bool>.ErrorResponse($"Error deleting {typeof(T).Name}: {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception deleting {typeof(T).Name} with ID: {id}");
                return ApiResponse<bool>.ErrorResponse($"Exception deleting {typeof(T).Name}: {ex.Message}");
            }
        }

        #endregion

        #region Batch Operations (SEALED - No Override)

        /// <summary>
        /// Crear múltiples entidades
        /// </summary>
        public virtual async Task<ApiResponse<BatchResponse<T>>> CreateBatchAsync(CreateBatchRequest<T> batchRequest)
        {
            try
            {
                _logger.LogInformation($"Creating batch of {batchRequest.Requests?.Count ?? 0} {typeof(T).Name}");

                var json = JsonSerializer.Serialize(batchRequest, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/create-batch", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<BatchResponse<T>>>(responseContent, _jsonOptions);
                    return result ?? ApiResponse<BatchResponse<T>>.ErrorResponse("Invalid response format");
                }

                _logger.LogError($"Error in batch create for {typeof(T).Name}: {response.StatusCode} - {responseContent}");
                return ApiResponse<BatchResponse<T>>.ErrorResponse($"Error in batch create: {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception in batch create for {typeof(T).Name}");
                return ApiResponse<BatchResponse<T>>.ErrorResponse($"Exception in batch create: {ex.Message}");
            }
        }

        /// <summary>
        /// Actualizar múltiples entidades
        /// </summary>
        public virtual async Task<ApiResponse<BatchResponse<T>>> UpdateBatchAsync(UpdateBatchRequest<T> batchRequest)
        {
            try
            {
                _logger.LogInformation($"Updating batch of {batchRequest.Requests?.Count ?? 0} {typeof(T).Name}");

                var json = JsonSerializer.Serialize(batchRequest, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{_baseUrl}/update-batch", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<BatchResponse<T>>>(responseContent, _jsonOptions);
                    return result ?? ApiResponse<BatchResponse<T>>.ErrorResponse("Invalid response format");
                }

                _logger.LogError($"Error in batch update for {typeof(T).Name}: {response.StatusCode} - {responseContent}");
                return ApiResponse<BatchResponse<T>>.ErrorResponse($"Error in batch update: {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception in batch update for {typeof(T).Name}");
                return ApiResponse<BatchResponse<T>>.ErrorResponse($"Exception in batch update: {ex.Message}");
            }
        }

        #endregion

        #region Health Check (Optional)

        /// <summary>
        /// Health check endpoint
        /// </summary>
        public virtual async Task<ApiResponse<object>> HealthCheckAsync()
        {
            try
            {
                _logger.LogInformation($"Performing health check for {typeof(T).Name}");

                var response = await _httpClient.GetAsync($"{_baseUrl}/health");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<object>>(responseContent, _jsonOptions);
                    return result ?? ApiResponse<object>.ErrorResponse("Invalid response format");
                }

                _logger.LogError($"Health check failed for {typeof(T).Name}: {response.StatusCode} - {responseContent}");
                return ApiResponse<object>.ErrorResponse($"Health check failed: {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception in health check for {typeof(T).Name}");
                return ApiResponse<object>.ErrorResponse($"Exception in health check: {ex.Message}");
            }
        }

        #endregion

        #region Query Operations (Dynamic Queries)

        /// <summary>
        /// Ejecutar query dinámica
        /// </summary>
        public virtual async Task<ApiResponse<List<T>>> QueryAsync(QueryRequest queryRequest)
        {
            try
            {
                _logger.LogInformation($"Executing query for {typeof(T).Name}");

                var json = JsonSerializer.Serialize(queryRequest, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/query", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<List<T>>>(responseContent, _jsonOptions);
                    return result ?? ApiResponse<List<T>>.ErrorResponse("Invalid response format");
                }

                _logger.LogError($"Error executing query for {typeof(T).Name}: {response.StatusCode} - {responseContent}");
                return ApiResponse<List<T>>.ErrorResponse($"Error executing query: {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception executing query for {typeof(T).Name}");
                return ApiResponse<List<T>>.ErrorResponse($"Exception executing query: {ex.Message}");
            }
        }

        /// <summary>
        /// Ejecutar query dinámica con paginación
        /// </summary>
        public virtual async Task<ApiResponse<PagedResult<T>>> QueryPagedAsync(QueryRequest queryRequest)
        {
            try
            {
                _logger.LogInformation($"Executing paged query for {typeof(T).Name}");

                var json = JsonSerializer.Serialize(queryRequest, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/paged", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<PagedResult<T>>>(responseContent, _jsonOptions);
                    return result ?? ApiResponse<PagedResult<T>>.ErrorResponse("Invalid response format");
                }

                _logger.LogError($"Error executing paged query for {typeof(T).Name}: {response.StatusCode} - {responseContent}");
                return ApiResponse<PagedResult<T>>.ErrorResponse($"Error executing paged query: {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception executing paged query for {typeof(T).Name}");
                return ApiResponse<PagedResult<T>>.ErrorResponse($"Exception executing paged query: {ex.Message}");
            }
        }

        /// <summary>
        /// Ejecutar query con select personalizado
        /// </summary>
        public virtual async Task<ApiResponse<List<object>>> QuerySelectAsync(QueryRequest queryRequest)
        {
            try
            {
                _logger.LogInformation($"Executing select query for {typeof(T).Name}");

                var json = JsonSerializer.Serialize(queryRequest, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/select", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<List<object>>>(responseContent, _jsonOptions);
                    return result ?? ApiResponse<List<object>>.ErrorResponse("Invalid response format");
                }

                _logger.LogError($"Error executing select query for {typeof(T).Name}: {response.StatusCode} - {responseContent}");
                return ApiResponse<List<object>>.ErrorResponse($"Error executing select query: {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception executing select query for {typeof(T).Name}");
                return ApiResponse<List<object>>.ErrorResponse($"Exception executing select query: {ex.Message}");
            }
        }

        /// <summary>
        /// Ejecutar query con select personalizado y paginación
        /// </summary>
        public virtual async Task<ApiResponse<PagedResult<object>>> QuerySelectPagedAsync(QueryRequest queryRequest)
        {
            try
            {
                _logger.LogInformation($"Executing paged select query for {typeof(T).Name}");

                var json = JsonSerializer.Serialize(queryRequest, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/select-paged", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<PagedResult<object>>>(responseContent, _jsonOptions);
                    return result ?? ApiResponse<PagedResult<object>>.ErrorResponse("Invalid response format");
                }

                _logger.LogError($"Error executing paged select query for {typeof(T).Name}: {response.StatusCode} - {responseContent}");
                return ApiResponse<PagedResult<object>>.ErrorResponse($"Error executing paged select query: {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception executing paged select query for {typeof(T).Name}");
                return ApiResponse<PagedResult<object>>.ErrorResponse($"Exception executing paged select query: {ex.Message}");
            }
        }

        #endregion

        #region Search Operations (Intelligent Search)

        /// <summary>
        /// Búsqueda inteligente por texto
        /// </summary>
        public virtual async Task<ApiResponse<List<T>>> SearchAsync(SearchRequest searchRequest)
        {
            try
            {
                _logger.LogInformation($"Executing search for {typeof(T).Name} with term: {searchRequest.SearchTerm}");

                var json = JsonSerializer.Serialize(searchRequest, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/search", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<List<T>>>(responseContent, _jsonOptions);
                    return result ?? ApiResponse<List<T>>.ErrorResponse("Invalid response format");
                }

                _logger.LogError($"Error executing search for {typeof(T).Name}: {response.StatusCode} - {responseContent}");
                return ApiResponse<List<T>>.ErrorResponse($"Error executing search: {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception executing search for {typeof(T).Name}");
                return ApiResponse<List<T>>.ErrorResponse($"Exception executing search: {ex.Message}");
            }
        }

        /// <summary>
        /// Búsqueda inteligente por texto con paginación
        /// </summary>
        public virtual async Task<ApiResponse<PagedResult<T>>> SearchPagedAsync(SearchRequest searchRequest)
        {
            try
            {
                _logger.LogInformation($"Executing paged search for {typeof(T).Name} with term: {searchRequest.SearchTerm}");

                var json = JsonSerializer.Serialize(searchRequest, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/search-paged", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<PagedResult<T>>>(responseContent, _jsonOptions);
                    return result ?? ApiResponse<PagedResult<T>>.ErrorResponse("Invalid response format");
                }

                _logger.LogError($"Error executing paged search for {typeof(T).Name}: {response.StatusCode} - {responseContent}");
                return ApiResponse<PagedResult<T>>.ErrorResponse($"Error executing paged search: {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception executing paged search for {typeof(T).Name}");
                return ApiResponse<PagedResult<T>>.ErrorResponse($"Exception executing paged search: {ex.Message}");
            }
        }

        /// <summary>
        /// Búsqueda simple por término (helper method)
        /// </summary>
        public virtual async Task<ApiResponse<List<T>>> SearchAsync(string searchTerm, string[]? searchFields = null, QueryRequest? baseQuery = null)
        {
            var searchRequest = new SearchRequest
            {
                SearchTerm = searchTerm,
                SearchFields = searchFields ?? Array.Empty<string>(),
                BaseQuery = baseQuery
            };

            return await SearchAsync(searchRequest);
        }

        /// <summary>
        /// Búsqueda simple paginada por término (helper method)
        /// </summary>
        public virtual async Task<ApiResponse<PagedResult<T>>> SearchPagedAsync(string searchTerm, int page = 1, int pageSize = 10, string[]? searchFields = null, QueryRequest? baseQuery = null)
        {
            var searchRequest = new SearchRequest
            {
                SearchTerm = searchTerm,
                SearchFields = searchFields ?? Array.Empty<string>(),
                BaseQuery = baseQuery,
                Skip = (page - 1) * pageSize,
                Take = pageSize
            };

            return await SearchPagedAsync(searchRequest);
        }

        #endregion

        #region Strongly Typed Query Operations

        /// <summary>
        /// Acceso al QueryBuilder fuertemente tipado para queries complejas y búsquedas
        /// </summary>
        public virtual QueryBuilder<T> Query()
        {
            return _queryService.For<T>();
        }

        /// <summary>
        /// Helper method para búsquedas rápidas con Expression trees
        /// </summary>
        /// <example>
        /// await service.QueryAsync()
        ///     .Where(x => x.Active == true)
        ///     .Search("test")
        ///     .InFields(x => x.Name, x => x.Description)
        ///     .ToListAsync();
        /// </example>
        public virtual QueryBuilder<T> QueryAsync()
        {
            return Query();
        }

        #endregion
    }
}