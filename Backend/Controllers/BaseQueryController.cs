using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Backend.Utils.Services;
using Shared.Models.Requests;
using Shared.Models.Responses;
using Shared.Models.QueryModels;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseQueryController<T> : ControllerBase where T : class
    {
        protected readonly BaseQueryService<T> _baseService;
        protected readonly ILogger<BaseQueryController<T>> _logger;

        protected BaseQueryController(BaseQueryService<T> baseService, ILogger<BaseQueryController<T>> logger)
        {
            _baseService = baseService;
            _logger = logger;
        }

        #region Individual Operations (SEALED - No Override)

        /// <summary>
        /// Crear una entidad individual
        /// </summary>
        [HttpPost("create")]
        public virtual async Task<IActionResult> Create([FromBody] CreateRequest<T> request)
        {
            try
            {
                _logger.LogInformation($"Creating {typeof(T).Name}");

                if (request?.Entity == null)
                {
                    return BadRequest("Request entity cannot be null");
                }

                var result = await _baseService.CreateAsync(request);
                return Ok(ApiResponse<T>.SuccessResponse(result, $"{typeof(T).Name} created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating {typeof(T).Name}");
                return StatusCode(500, ApiResponse<T>.ErrorResponse($"Error creating {typeof(T).Name}: {ex.Message}"));
            }
        }

        /// <summary>
        /// Actualizar una entidad individual
        /// </summary>
        [HttpPut("update")]
        public virtual async Task<IActionResult> Update([FromBody] UpdateRequest<T> request)
        {
            try
            {
                _logger.LogInformation($"Updating {typeof(T).Name}");

                if (request?.Entity == null)
                {
                    return BadRequest("Request entity cannot be null");
                }

                var result = await _baseService.UpdateAsync(request);
                return Ok(ApiResponse<T>.SuccessResponse(result, $"{typeof(T).Name} updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating {typeof(T).Name}");
                return StatusCode(500, ApiResponse<T>.ErrorResponse($"Error updating {typeof(T).Name}: {ex.Message}"));
            }
        }

        /// <summary>
        /// Obtener todos los registros (paginado por defecto)
        /// </summary>
        [HttpGet("all")]
        public virtual async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool all = false)
        {
            try
            {
                _logger.LogInformation($"Getting {typeof(T).Name} - Page: {page}, PageSize: {pageSize}, All: {all}");

                if (all)
                {
                    var allData = await _baseService.GetAllUnpagedAsync();
                    return Ok(ApiResponse<List<T>>.SuccessResponse(allData, $"Retrieved all {typeof(T).Name}"));
                }

                var pagedData = await _baseService.GetAllPagedAsync(page, pageSize);
                return Ok(ApiResponse<PagedResponse<T>>.SuccessResponse(pagedData, $"Retrieved paged {typeof(T).Name}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting {typeof(T).Name}");
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Error getting {typeof(T).Name}: {ex.Message}"));
            }
        }

        /// <summary>
        /// Obtener por ID
        /// </summary>
        [HttpGet("{id}")]
        public virtual async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                _logger.LogInformation($"Getting {typeof(T).Name} by ID: {id}");

                var result = await _baseService.GetByIdAsync(id);

                if (result == null)
                {
                    return NotFound(ApiResponse<T>.ErrorResponse($"{typeof(T).Name} with ID {id} not found"));
                }

                return Ok(ApiResponse<T>.SuccessResponse(result, $"{typeof(T).Name} retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting {typeof(T).Name} by ID: {id}");
                return StatusCode(500, ApiResponse<T>.ErrorResponse($"Error getting {typeof(T).Name}: {ex.Message}"));
            }
        }

        /// <summary>
        /// Eliminar por ID
        /// </summary>
        [HttpDelete("{id}")]
        public virtual async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                _logger.LogInformation($"Deleting {typeof(T).Name} with ID: {id}");

                var result = await _baseService.DeleteAsync(id);

                if (!result)
                {
                    return NotFound(ApiResponse<bool>.ErrorResponse($"{typeof(T).Name} with ID {id} not found"));
                }

                return Ok(ApiResponse<bool>.SuccessResponse(true, $"{typeof(T).Name} deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting {typeof(T).Name} with ID: {id}");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse($"Error deleting {typeof(T).Name}: {ex.Message}"));
            }
        }

        #endregion

        #region Batch Operations (SEALED - No Override)

        /// <summary>
        /// Crear múltiples entidades
        /// </summary>
        [HttpPost("create-batch")]
        public virtual async Task<IActionResult> CreateBatch([FromBody] CreateBatchRequest<T> batchRequest)
        {
            try
            {
                _logger.LogInformation($"Creating batch of {batchRequest.Requests?.Count ?? 0} {typeof(T).Name}");

                if (batchRequest?.Requests == null || !batchRequest.Requests.Any())
                {
                    return BadRequest("Batch request must contain at least one item");
                }

                var result = await _baseService.CreateBatchAsync(batchRequest);

                var message = result.AllSuccessful
                    ? $"Successfully created {result.SuccessCount} {typeof(T).Name}"
                    : $"Created {result.SuccessCount} {typeof(T).Name}, {result.FailedCount} failed";

                return Ok(ApiResponse<BatchResponse<T>>.SuccessResponse(result, message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in batch create for {typeof(T).Name}");
                return StatusCode(500, ApiResponse<BatchResponse<T>>.ErrorResponse($"Error in batch create: {ex.Message}"));
            }
        }

        /// <summary>
        /// Actualizar múltiples entidades
        /// </summary>
        [HttpPut("update-batch")]
        public virtual async Task<IActionResult> UpdateBatch([FromBody] UpdateBatchRequest<T> batchRequest)
        {
            try
            {
                _logger.LogInformation($"Updating batch of {batchRequest.Requests?.Count ?? 0} {typeof(T).Name}");

                if (batchRequest?.Requests == null || !batchRequest.Requests.Any())
                {
                    return BadRequest("Batch request must contain at least one item");
                }

                var result = await _baseService.UpdateBatchAsync(batchRequest);

                var message = result.AllSuccessful
                    ? $"Successfully updated {result.SuccessCount} {typeof(T).Name}"
                    : $"Updated {result.SuccessCount} {typeof(T).Name}, {result.FailedCount} failed";

                return Ok(ApiResponse<BatchResponse<T>>.SuccessResponse(result, message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in batch update for {typeof(T).Name}");
                return StatusCode(500, ApiResponse<BatchResponse<T>>.ErrorResponse($"Error in batch update: {ex.Message}"));
            }
        }

        #endregion

        #region Health Check (Optional)

        /// <summary>
        /// Health check endpoint
        /// </summary>
        [HttpGet("health")]
        public virtual async Task<IActionResult> HealthCheck()
        {
            try
            {
                // Simple health check - attempt to query the entity
                var count = await _baseService.GetAllPagedAsync(1, 1);
                return Ok(new
                {
                    Status = "Healthy",
                    EntityType = typeof(T).Name,
                    Timestamp = DateTime.UtcNow,
                    TotalRecords = count.TotalCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Health check failed for {typeof(T).Name}");
                return StatusCode(500, new
                {
                    Status = "Unhealthy",
                    EntityType = typeof(T).Name,
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        #endregion

        #region Query Operations (Dynamic Queries)

        /// <summary>
        /// Ejecutar query dinámica
        /// </summary>
        [HttpPost("query")]
        public virtual async Task<IActionResult> Query([FromBody] QueryRequest queryRequest)
        {
            try
            {
                _logger.LogInformation($"Executing query for {typeof(T).Name}");

                var result = await _baseService.QueryAsync(queryRequest);
                return Ok(ApiResponse<List<T>>.SuccessResponse(result, $"Query executed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing query for {typeof(T).Name}");
                return StatusCode(500, ApiResponse<List<T>>.ErrorResponse($"Error executing query: {ex.Message}"));
            }
        }

        /// <summary>
        /// Ejecutar query dinámica con paginación
        /// </summary>
        [HttpPost("paged")]
        public virtual async Task<IActionResult> QueryPaged([FromBody] QueryRequest queryRequest)
        {
            try
            {
                _logger.LogInformation($"Executing paged query for {typeof(T).Name}");

                var result = await _baseService.QueryPagedAsync(queryRequest);
                return Ok(ApiResponse<Shared.Models.QueryModels.PagedResult<T>>.SuccessResponse(result, $"Paged query executed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing paged query for {typeof(T).Name}");
                return StatusCode(500, ApiResponse<Shared.Models.QueryModels.PagedResult<T>>.ErrorResponse($"Error executing paged query: {ex.Message}"));
            }
        }

        /// <summary>
        /// Ejecutar query con select personalizado
        /// </summary>
        [HttpPost("select")]
        public virtual async Task<IActionResult> QuerySelect([FromBody] QueryRequest queryRequest)
        {
            try
            {
                _logger.LogInformation($"Executing select query for {typeof(T).Name}");

                var result = await _baseService.QuerySelectAsync(queryRequest);
                return Ok(ApiResponse<List<object>>.SuccessResponse(result, $"Select query executed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing select query for {typeof(T).Name}");
                return StatusCode(500, ApiResponse<List<object>>.ErrorResponse($"Error executing select query: {ex.Message}"));
            }
        }

        /// <summary>
        /// Ejecutar query con select personalizado y paginación
        /// </summary>
        [HttpPost("select-paged")]
        public virtual async Task<IActionResult> QuerySelectPaged([FromBody] QueryRequest queryRequest)
        {
            try
            {
                _logger.LogInformation($"Executing paged select query for {typeof(T).Name}");

                var result = await _baseService.QuerySelectPagedAsync(queryRequest);
                return Ok(ApiResponse<Shared.Models.QueryModels.PagedResult<object>>.SuccessResponse(result, $"Paged select query executed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing paged select query for {typeof(T).Name}");
                return StatusCode(500, ApiResponse<Shared.Models.QueryModels.PagedResult<object>>.ErrorResponse($"Error executing paged select query: {ex.Message}"));
            }
        }

        #endregion

        #region Search Operations (Intelligent Search)

        /// <summary>
        /// Búsqueda inteligente por texto
        /// </summary>
        [HttpPost("search")]
        public virtual async Task<IActionResult> Search([FromBody] SearchRequest searchRequest)
        {
            try
            {
                _logger.LogInformation($"Executing search for {typeof(T).Name} with term: {searchRequest.SearchTerm}");

                var result = await _baseService.SearchAsync(searchRequest);
                return Ok(ApiResponse<List<T>>.SuccessResponse(result, $"Search executed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing search for {typeof(T).Name}");
                return StatusCode(500, ApiResponse<List<T>>.ErrorResponse($"Error executing search: {ex.Message}"));
            }
        }

        /// <summary>
        /// Búsqueda inteligente por texto con paginación
        /// </summary>
        [HttpPost("search-paged")]
        public virtual async Task<IActionResult> SearchPaged([FromBody] SearchRequest searchRequest)
        {
            try
            {
                _logger.LogInformation($"Executing paged search for {typeof(T).Name} with term: {searchRequest.SearchTerm}");

                var result = await _baseService.SearchPagedAsync(searchRequest);
                return Ok(ApiResponse<Shared.Models.QueryModels.PagedResult<T>>.SuccessResponse(result, $"Paged search executed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing paged search for {typeof(T).Name}");
                return StatusCode(500, ApiResponse<Shared.Models.QueryModels.PagedResult<T>>.ErrorResponse($"Error executing paged search: {ex.Message}"));
            }
        }

        #endregion
    }
}