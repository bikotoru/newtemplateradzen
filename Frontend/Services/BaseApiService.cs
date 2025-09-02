using Microsoft.Extensions.Logging;
using Shared.Models.Requests;
using Shared.Models.Responses;

namespace Frontend.Services
{
    public abstract class BaseApiService<T> where T : class
    {
        protected readonly API _api;
        protected readonly ILogger<BaseApiService<T>> _logger;
        protected readonly string _baseUrl;

        protected BaseApiService(API api, ILogger<BaseApiService<T>> logger, string baseUrl)
        {
            _api = api;
            _logger = logger;
            _baseUrl = baseUrl.TrimEnd('/');
        }

        #region Individual Operations

        /// <summary>
        /// Crear una entidad individual
        /// </summary>
        public virtual async Task<ApiResponse<T>> CreateAsync(CreateRequest<T> request)
        {
            _logger.LogInformation($"Creating {typeof(T).Name}");
            return await _api.PostAsync<T>($"{_baseUrl}/create", request);
        }

        /// <summary>
        /// Actualizar una entidad individual
        /// </summary>
        public virtual async Task<ApiResponse<T>> UpdateAsync(UpdateRequest<T> request)
        {
            _logger.LogInformation($"Updating {typeof(T).Name}");
            return await _api.PutAsync<T>($"{_baseUrl}/update", request);
        }

        /// <summary>
        /// Obtener todos los registros (paginado por defecto)
        /// </summary>
        public virtual async Task<ApiResponse<PagedResponse<T>>> GetAllPagedAsync(int page = 1, int pageSize = 10)
        {
            _logger.LogInformation($"Getting paged {typeof(T).Name} - Page: {page}, PageSize: {pageSize}");
            return await _api.GetAsync<PagedResponse<T>>($"{_baseUrl}/all?page={page}&pageSize={pageSize}&all=false");
        }

        /// <summary>
        /// Obtener todos los registros sin paginación
        /// </summary>
        public virtual async Task<ApiResponse<List<T>>> GetAllUnpagedAsync()
        {
            _logger.LogInformation($"Getting all {typeof(T).Name} (unpaged)");
            return await _api.GetAsync<List<T>>($"{_baseUrl}/all?all=true");
        }

        /// <summary>
        /// Obtener por ID
        /// </summary>
        public virtual async Task<ApiResponse<T>> GetByIdAsync(Guid id)
        {
            _logger.LogInformation($"Getting {typeof(T).Name} by ID: {id}");
            return await _api.GetAsync<T>($"{_baseUrl}/{id}");
        }

        /// <summary>
        /// Eliminar por ID
        /// </summary>
        public virtual async Task<ApiResponse<bool>> DeleteAsync(Guid id)
        {
            _logger.LogInformation($"Deleting {typeof(T).Name} with ID: {id}");
            return await _api.DeleteAsync<bool>($"{_baseUrl}/{id}");
        }

        #endregion

        #region Batch Operations

        /// <summary>
        /// Crear múltiples entidades
        /// </summary>
        public virtual async Task<ApiResponse<BatchResponse<T>>> CreateBatchAsync(CreateBatchRequest<T> batchRequest)
        {
            _logger.LogInformation($"Creating batch of {batchRequest.Requests?.Count ?? 0} {typeof(T).Name}");
            return await _api.PostAsync<BatchResponse<T>>($"{_baseUrl}/create-batch", batchRequest);
        }

        /// <summary>
        /// Actualizar múltiples entidades
        /// </summary>
        public virtual async Task<ApiResponse<BatchResponse<T>>> UpdateBatchAsync(UpdateBatchRequest<T> batchRequest)
        {
            _logger.LogInformation($"Updating batch of {batchRequest.Requests?.Count ?? 0} {typeof(T).Name}");
            return await _api.PutAsync<BatchResponse<T>>($"{_baseUrl}/update-batch", batchRequest);
        }

        #endregion

        #region Health Check

        /// <summary>
        /// Health check endpoint
        /// </summary>
        public virtual async Task<ApiResponse<object>> HealthCheckAsync()
        {
            _logger.LogInformation($"Performing health check for {typeof(T).Name}");
            return await _api.GetAsync<object>($"{_baseUrl}/health");
        }

        #endregion
    }
}