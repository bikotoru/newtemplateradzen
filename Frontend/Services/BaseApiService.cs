using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Shared.Models.Requests;
using Shared.Models.Responses;
using Shared.Models.QueryModels;
using Shared.Models.Services;
using Radzen;

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

        #region Radzen Integration (LoadDataArgs)

        /// <summary>
        /// 1. LoadDataAsync - Básico
        /// Convierte LoadDataArgs de Radzen a Query y devuelve datos para grids
        /// </summary>
        public virtual async Task<ApiResponse<PagedResult<T>>> LoadDataAsync(LoadDataArgs args)
        {
            try
            {
                var query = ConvertLoadDataArgsToQuery(args);
                return await query.ToPagedResultAsync() switch
                {
                    var result when result != null => ApiResponse<PagedResult<T>>.SuccessResponse(result),
                    _ => ApiResponse<PagedResult<T>>.ErrorResponse("No data returned from query")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception in LoadDataAsync for {typeof(T).Name}");
                return ApiResponse<PagedResult<T>>.ErrorResponse($"Exception in LoadData: {ex.Message}");
            }
        }

        /// <summary>
        /// 2. LoadDataAsync con campos de búsqueda tipados
        /// </summary>
        public virtual async Task<ApiResponse<PagedResult<T>>> LoadDataAsync(
            LoadDataArgs args,
            params Expression<Func<T, object>>[] searchFields)
        {
            try
            {
                var query = ConvertLoadDataArgsToQuery(args, null, searchFields);
                var result = await query.ToPagedResultAsync();
                return ApiResponse<PagedResult<T>>.SuccessResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception in LoadDataAsync with typed search fields for {typeof(T).Name}");
                return ApiResponse<PagedResult<T>>.ErrorResponse($"Exception in LoadData: {ex.Message}");
            }
        }

        /// <summary>
        /// 3. LoadDataAsync con campos de búsqueda string (soporta anidados como "Creador.Nombre")
        /// </summary>
        public virtual async Task<ApiResponse<PagedResult<T>>> LoadDataAsync(
            LoadDataArgs args,
            List<string> searchFields)
        {
            try
            {
                var query = ConvertLoadDataArgsToQuery(args, null, null, searchFields);
                var result = await query.ToPagedResultAsync();
                return ApiResponse<PagedResult<T>>.SuccessResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception in LoadDataAsync with string search fields for {typeof(T).Name}");
                return ApiResponse<PagedResult<T>>.ErrorResponse($"Exception in LoadData: {ex.Message}");
            }
        }

        /// <summary>
        /// 4. LoadDataAsync con QueryBuilder base + búsqueda tipada
        /// </summary>
        public virtual async Task<ApiResponse<PagedResult<T>>> LoadDataAsync(
            LoadDataArgs args,
            QueryBuilder<T>? baseQuery = null,
            params Expression<Func<T, object>>[] searchFields)
        {
            try
            {
                var query = ConvertLoadDataArgsToQuery(args, baseQuery, searchFields);
                var result = await query.ToPagedResultAsync();
                return ApiResponse<PagedResult<T>>.SuccessResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception in LoadDataAsync with base query and typed search fields for {typeof(T).Name}");
                return ApiResponse<PagedResult<T>>.ErrorResponse($"Exception in LoadData: {ex.Message}");
            }
        }

        /// <summary>
        /// 5. LoadDataAsync con QueryBuilder base + búsqueda string
        /// </summary>
        public virtual async Task<ApiResponse<PagedResult<T>>> LoadDataAsync(
            LoadDataArgs args,
            QueryBuilder<T>? baseQuery,
            List<string> searchFields)
        {
            try
            {
                var query = ConvertLoadDataArgsToQuery(args, baseQuery, null, searchFields);
                var result = await query.ToPagedResultAsync();
                return ApiResponse<PagedResult<T>>.SuccessResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception in LoadDataAsync with base query and string search fields for {typeof(T).Name}");
                return ApiResponse<PagedResult<T>>.ErrorResponse($"Exception in LoadData: {ex.Message}");
            }
        }

        /// <summary>
        /// 6. LoadDataAsync con Select tipado + búsqueda tipada
        /// </summary>
        public virtual async Task<ApiResponse<PagedResult<TResult>>> LoadDataAsync<TResult>(
            LoadDataArgs args,
            Expression<Func<T, TResult>> selector,
            QueryBuilder<T>? baseQuery = null,
            params Expression<Func<T, object>>[] searchFields)
        {
            try
            {
                var query = ConvertLoadDataArgsToQuery(args, baseQuery, searchFields);
                var result = await query.Select(selector).ToPagedResultAsync();
                return ApiResponse<PagedResult<TResult>>.SuccessResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception in LoadDataAsync with select and typed search fields for {typeof(T).Name}");
                return ApiResponse<PagedResult<TResult>>.ErrorResponse($"Exception in LoadData: {ex.Message}");
            }
        }

        /// <summary>
        /// 7. LoadDataAsync con Select tipado + búsqueda string
        /// </summary>
        public virtual async Task<ApiResponse<PagedResult<TResult>>> LoadDataAsync<TResult>(
            LoadDataArgs args,
            Expression<Func<T, TResult>> selector,
            QueryBuilder<T>? baseQuery,
            List<string> searchFields)
        {
            try
            {
                var query = ConvertLoadDataArgsToQuery(args, baseQuery, null, searchFields);
                var result = await query.Select(selector).ToPagedResultAsync();
                return ApiResponse<PagedResult<TResult>>.SuccessResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception in LoadDataAsync with select and string search fields for {typeof(T).Name}");
                return ApiResponse<PagedResult<TResult>>.ErrorResponse($"Exception in LoadData: {ex.Message}");
            }
        }

        /// <summary>
        /// 8. LoadDataAsync con campos string para select + búsqueda
        /// </summary>
        public virtual async Task<ApiResponse<PagedResult<dynamic>>> LoadDataAsync(
            LoadDataArgs args,
            List<string> selectFields,
            QueryBuilder<T>? baseQuery = null,
            List<string>? searchFields = null)
        {
            try
            {
                // Para este método necesitaremos usar el QueryRequest directamente
                // ya que no podemos hacer Select con strings en el QueryBuilder tipado
                var queryRequest = ConvertLoadDataArgsToQueryRequest(args, baseQuery, searchFields);
                queryRequest.Select = string.Join(", ", selectFields);
                
                var result = await QuerySelectPagedAsync(queryRequest);
                if (result.Success && result.Data != null)
                {
                    var pagedResult = new PagedResult<dynamic>
                    {
                        Data = result.Data.Data?.Cast<dynamic>().ToList() ?? new List<dynamic>(),
                        TotalCount = result.Data.TotalCount,
                        Page = result.Data.Page,
                        PageSize = result.Data.PageSize
                    };
                    return ApiResponse<PagedResult<dynamic>>.SuccessResponse(pagedResult);
                }
                return ApiResponse<PagedResult<dynamic>>.ErrorResponse(result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception in LoadDataAsync with string select fields for {typeof(T).Name}");
                return ApiResponse<PagedResult<dynamic>>.ErrorResponse($"Exception in LoadData: {ex.Message}");
            }
        }

        #endregion

        #region Private Helper Methods for LoadData

        /// <summary>
        /// Convierte LoadDataArgs de Radzen a QueryBuilder
        /// </summary>
        private QueryBuilder<T> ConvertLoadDataArgsToQuery(
            LoadDataArgs args, 
            QueryBuilder<T>? baseQuery = null,
            Expression<Func<T, object>>[]? typedSearchFields = null,
            List<string>? stringSearchFields = null)
        {
            var query = baseQuery ?? Query();

            // Aplicar filtros de Radzen
            if (args.Filters != null && args.Filters.Any())
            {
                foreach (var filter in args.Filters)
                {
                    var filterString = ConvertRadzenFilterToString(filter);
                    if (!string.IsNullOrEmpty(filterString))
                    {
                        // Necesitamos una forma de aplicar filtros string al QueryBuilder
                        // Por ahora, esto requerirá mejoras en el QueryBuilder para soportar filtros string
                        _logger.LogWarning($"Filter conversion not fully implemented: {filterString}");
                    }
                }
            }

            // Aplicar ordenamiento de Radzen
            if (args.Sorts != null && args.Sorts.Any())
            {
                var firstSort = args.Sorts.First();
                var orderByString = ConvertRadzenSortToString(firstSort);
                if (!string.IsNullOrEmpty(orderByString))
                {
                    // Similar al filtro, necesitamos soporte para OrderBy string
                    _logger.LogWarning($"Sort conversion not fully implemented: {orderByString}");
                }
            }

            // Aplicar búsqueda si hay término de búsqueda
            if (!string.IsNullOrEmpty(args.Filter))
            {
                query = query.Search(args.Filter);
                
                // Aplicar campos de búsqueda tipados
                if (typedSearchFields != null && typedSearchFields.Any())
                {
                    query = query.InFields(typedSearchFields);
                }
                // Aplicar campos de búsqueda string (convertir a tipados si es posible)
                else if (stringSearchFields != null && stringSearchFields.Any())
                {
                    // Para campos string, necesitaremos usar el SearchRequest directamente
                    // Por ahora registramos la limitación
                    _logger.LogInformation($"String search fields will be applied via SearchRequest: {string.Join(", ", stringSearchFields)}");
                }
            }

            // Aplicar Skip y Take para paginación
            if (args.Skip.HasValue)
            {
                query = query.Skip(args.Skip.Value);
            }

            if (args.Top.HasValue)
            {
                query = query.Take(args.Top.Value);
            }

            return query;
        }

        /// <summary>
        /// Convierte LoadDataArgs a QueryRequest para casos que requieren strings
        /// </summary>
        private QueryRequest ConvertLoadDataArgsToQueryRequest(
            LoadDataArgs args,
            QueryBuilder<T>? baseQuery = null,
            List<string>? searchFields = null)
        {
            var queryRequest = new QueryRequest();

            // Aplicar filtros base del QueryBuilder si existe
            if (baseQuery != null)
            {
                // Aquí necesitaríamos una forma de extraer los filtros del QueryBuilder
                // Por ahora, esto es una limitación que requerirá mejoras
                _logger.LogWarning("Base query extraction not fully implemented for QueryRequest conversion");
            }

            // Aplicar filtros de Radzen
            if (args.Filters != null && args.Filters.Any())
            {
                var filters = args.Filters.Select(ConvertRadzenFilterToString).Where(f => !string.IsNullOrEmpty(f));
                if (filters.Any())
                {
                    queryRequest.Filter = string.Join(" && ", filters);
                }
            }

            // Aplicar ordenamiento de Radzen
            if (args.Sorts != null && args.Sorts.Any())
            {
                var sorts = args.Sorts.Select(ConvertRadzenSortToString).Where(s => !string.IsNullOrEmpty(s));
                if (sorts.Any())
                {
                    queryRequest.OrderBy = string.Join(", ", sorts);
                }
            }

            // Aplicar paginación
            queryRequest.Skip = args.Skip;
            queryRequest.Take = args.Top;

            return queryRequest;
        }

        /// <summary>
        /// Convierte un FilterDescriptor de Radzen a string de filtro
        /// </summary>
        private string ConvertRadzenFilterToString(FilterDescriptor filter)
        {
            if (filter == null || string.IsNullOrEmpty(filter.Property)) 
                return string.Empty;

            var property = filter.Property;
            var value = filter.FilterValue?.ToString();
            
            return filter.FilterOperator switch
            {
                FilterOperator.Equals => $"{property} == \"{value}\"",
                FilterOperator.NotEquals => $"{property} != \"{value}\"",
                FilterOperator.Contains => $"{property}.Contains(\"{value}\")",
                FilterOperator.DoesNotContain => $"!{property}.Contains(\"{value}\")",
                FilterOperator.StartsWith => $"{property}.StartsWith(\"{value}\")",
                FilterOperator.EndsWith => $"{property}.EndsWith(\"{value}\")",
                FilterOperator.GreaterThan => $"{property} > {value}",
                FilterOperator.GreaterThanOrEquals => $"{property} >= {value}",
                FilterOperator.LessThan => $"{property} < {value}",
                FilterOperator.LessThanOrEquals => $"{property} <= {value}",
                FilterOperator.IsNull => $"{property} == null",
                FilterOperator.IsNotNull => $"{property} != null",
                FilterOperator.IsEmpty => $"string.IsNullOrEmpty({property})",
                FilterOperator.IsNotEmpty => $"!string.IsNullOrEmpty({property})",
                _ => string.Empty
            };
        }

        /// <summary>
        /// Convierte un SortDescriptor de Radzen a string de ordenamiento
        /// </summary>
        private string ConvertRadzenSortToString(SortDescriptor sort)
        {
            if (sort == null || string.IsNullOrEmpty(sort.Property))
                return string.Empty;

            var direction = sort.SortOrder == SortOrder.Descending ? " desc" : "";
            return $"{sort.Property}{direction}";
        }

        #endregion

        #region Excel Export Operations

        /// <summary>
        /// Exporta datos a Excel usando ExcelExportRequest
        /// </summary>
        public virtual async Task<byte[]> ExportToExcelAsync(Shared.Models.Export.ExcelExportRequest exportRequest)
        {
            try
            {
                _logger.LogInformation($"Starting Excel export for {typeof(T).Name}");

                var json = JsonSerializer.Serialize(exportRequest, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/export/excel", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var excelBytes = await response.Content.ReadAsByteArrayAsync();
                    _logger.LogInformation($"Excel export completed for {typeof(T).Name}. Downloaded {excelBytes.Length} bytes");
                    return excelBytes;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Error exporting to Excel for {typeof(T).Name}: {response.StatusCode} - {errorContent}");
                throw new HttpRequestException($"Error exporting to Excel: {response.StatusCode} - {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception exporting to Excel for {typeof(T).Name}");
                throw new InvalidOperationException($"Exception exporting to Excel: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Exporta datos a Excel y descarga automáticamente usando JavaScript (requiere FileDownloadService)
        /// </summary>
        public virtual async Task DownloadExcelAsync(Shared.Models.Export.ExcelExportRequest exportRequest, FileDownloadService fileDownloadService, string? fileName = null)
        {
            try
            {
                _logger.LogInformation($"Starting Excel download for {typeof(T).Name}");

                // Obtener bytes del Excel
                var excelBytes = await ExportToExcelAsync(exportRequest);
                var finalFileName = fileName ?? $"{typeof(T).Name}_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                
                // Usar FileDownloadService para descargar
                await fileDownloadService.DownloadExcelAsync(excelBytes, finalFileName);
                
                _logger.LogInformation($"Excel file downloaded successfully: {finalFileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception downloading Excel for {typeof(T).Name}");
                throw new InvalidOperationException($"Error descargando Excel: {ex.Message}", ex);
            }
        }

        #endregion
    }
}