using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Backend.Utils.Services;
using Backend.Utils.Security;
using Backend.Utils.Attributes;
using Shared.Models.Requests;
using Shared.Models.Responses;
using Shared.Models.QueryModels;
using Shared.Models.DTOs.Auth;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseQueryController<T> : ControllerBase where T : class
    {
        protected readonly BaseQueryService<T> _baseService;
        protected readonly ILogger<BaseQueryController<T>> _logger;
        protected readonly IServiceProvider _serviceProvider;
        protected readonly PermissionService _permissionService;

        protected BaseQueryController(BaseQueryService<T> baseService, ILogger<BaseQueryController<T>> logger, IServiceProvider serviceProvider)
        {
            _baseService = baseService;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _permissionService = serviceProvider.GetRequiredService<PermissionService>();
        }

        #region Individual Operations (SEALED - No Override)

        /// <summary>
        /// Crear una entidad individual
        /// </summary>
        [HttpPost("create")]
        public virtual async Task<IActionResult> Create([FromBody] CreateRequest<T> request)
        {
            // Validar permiso automáticamente: CATEGORIA.CREATE, USUARIO.CREATE, etc.
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("create");
            if (errorResult != null) return errorResult;

            try
            {
                _logger.LogInformation($"Creating {typeof(T).Name}");

                if (request?.Entity == null)
                {
                    return BadRequest("Request entity cannot be null");
                }

                var result = await _baseService.CreateAsync(request, user);
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
            // Validar permiso automáticamente: CATEGORIA.UPDATE, USUARIO.UPDATE, etc.
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("update");
            if (errorResult != null) return errorResult;

            try
            {
                _logger.LogInformation($"Updating {typeof(T).Name}");

                if (request?.Entity == null)
                {
                    return BadRequest("Request entity cannot be null");
                }

                var result = await _baseService.UpdateAsync(request, user);
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
            // Validar permiso automáticamente: CATEGORIA.VIEW, USUARIO.VIEW, etc.
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
            if (errorResult != null) return errorResult;

            try
            {
                _logger.LogInformation($"Getting {typeof(T).Name} - Page: {page}, PageSize: {pageSize}, All: {all}");

                if (all)
                {
                    var allData = await _baseService.GetAllUnpagedAsync(user);
                    return Ok(ApiResponse<List<T>>.SuccessResponse(allData, $"Retrieved all {typeof(T).Name}"));
                }

                var pagedData = await _baseService.GetAllPagedAsync(page, pageSize, user);
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
            // Validar permiso automáticamente: CATEGORIA.VIEW, USUARIO.VIEW, etc.
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
            if (errorResult != null) return errorResult;

            try
            {
                _logger.LogInformation($"Getting {typeof(T).Name} by ID: {id}");

                var result = await _baseService.GetByIdAsync(id, user);

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
            // Validar permiso automáticamente: CATEGORIA.DELETE, USUARIO.DELETE, etc.
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("delete");
            if (errorResult != null) return errorResult;

            try
            {
                _logger.LogInformation($"Deleting {typeof(T).Name} with ID: {id}");

                var result = await _baseService.DeleteAsync(id, user);

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
            // Validar permiso automáticamente: CATEGORIA.CREATE, USUARIO.CREATE, etc.
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("create");
            if (errorResult != null) return errorResult;

            try
            {
                _logger.LogInformation($"Creating batch of {batchRequest.Requests?.Count ?? 0} {typeof(T).Name}");

                if (batchRequest?.Requests == null || !batchRequest.Requests.Any())
                {
                    return BadRequest("Batch request must contain at least one item");
                }

                var result = await _baseService.CreateBatchAsync(batchRequest, user);

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
            // Validar permiso automáticamente: CATEGORIA.UPDATE, USUARIO.UPDATE, etc.
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("update");
            if (errorResult != null) return errorResult;

            try
            {
                _logger.LogInformation($"Updating batch of {batchRequest.Requests?.Count ?? 0} {typeof(T).Name}");

                if (batchRequest?.Requests == null || !batchRequest.Requests.Any())
                {
                    return BadRequest("Batch request must contain at least one item");
                }

                var result = await _baseService.UpdateBatchAsync(batchRequest, user);

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
            // Solo validar autenticación (no permisos específicos)
            var user = await ValidarUsuario();
            if (user == null) return Unauthorized();

            try
            {
                // Simple health check - attempt to query the entity
                var count = await _baseService.GetAllPagedAsync(1, 1, user);
                return Ok(new
                {
                    Status = "Healthy",
                    EntityType = typeof(T).Name,
                    Timestamp = DateTime.UtcNow,
                    TotalRecords = count.TotalCount,
                    User = user.Id
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
            // Validar permiso automáticamente: CATEGORIA.VIEW, USUARIO.VIEW, etc.
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
            if (errorResult != null) return errorResult;

            try
            {
                _logger.LogInformation($"Executing query for {typeof(T).Name}");

                var result = await _baseService.QueryAsync(queryRequest, user);
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
            // Validar permiso automáticamente: CATEGORIA.VIEW, USUARIO.VIEW, etc.
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
            if (errorResult != null) return errorResult;

            try
            {
                _logger.LogInformation($"Executing paged query for {typeof(T).Name}");

                var result = await _baseService.QueryPagedAsync(queryRequest, user);
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
            // Validar permiso automáticamente: CATEGORIA.VIEW, USUARIO.VIEW, etc.
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
            if (errorResult != null) return errorResult;

            try
            {
                _logger.LogInformation($"Executing select query for {typeof(T).Name}");

                var result = await _baseService.QuerySelectAsync(queryRequest, user);
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
            // Validar permiso automáticamente: CATEGORIA.VIEW, USUARIO.VIEW, etc.
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
            if (errorResult != null) return errorResult;

            try
            {
                _logger.LogInformation($"Executing paged select query for {typeof(T).Name}");

                var result = await _baseService.QuerySelectPagedAsync(queryRequest, user);
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
            // Validar permiso automáticamente: CATEGORIA.VIEW, USUARIO.VIEW, etc.
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
            if (errorResult != null) return errorResult;

            try
            {
                _logger.LogInformation($"Executing search for {typeof(T).Name} with term: {searchRequest.SearchTerm}");

                var result = await _baseService.SearchAsync(searchRequest, user);
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
            // Validar permiso automáticamente: CATEGORIA.VIEW, USUARIO.VIEW, etc.
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
            if (errorResult != null) return errorResult;

            try
            {
                _logger.LogInformation($"Executing paged search for {typeof(T).Name} with term: {searchRequest.SearchTerm}");

                var result = await _baseService.SearchPagedAsync(searchRequest, user);
                return Ok(ApiResponse<Shared.Models.QueryModels.PagedResult<T>>.SuccessResponse(result, $"Paged search executed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing paged search for {typeof(T).Name}");
                return StatusCode(500, ApiResponse<Shared.Models.QueryModels.PagedResult<T>>.ErrorResponse($"Error executing paged search: {ex.Message}"));
            }
        }

        #endregion

        #region Excel Export Operations

        /// <summary>
        /// Exporta datos a Excel basado en ExcelExportRequest
        /// </summary>
        [HttpPost("export/excel")]
        public virtual async Task<IActionResult> ExportToExcel([FromBody] Shared.Models.Export.ExcelExportRequest exportRequest)
        {
            // Validar permiso automáticamente: CATEGORIA.VIEW, USUARIO.VIEW, etc.
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
            if (errorResult != null) return errorResult;

            try
            {
                _logger.LogInformation($"Starting Excel export for {typeof(T).Name}");

                // Crear servicio de exportación
                var exportService = new Backend.Utils.Services.ExcelExportService<T>(_baseService, 
                    _serviceProvider.GetRequiredService<ILogger<Backend.Utils.Services.ExcelExportService<T>>>());

                // Generar Excel
                var excelBytes = await exportService.ExportToExcelAsync(exportRequest, user);

                // Generar nombre de archivo
                var fileName = $"{typeof(T).Name}_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                _logger.LogInformation($"Excel export completed for {typeof(T).Name}. File size: {excelBytes.Length} bytes");

                // Devolver archivo
                return File(
                    excelBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error exporting to Excel for {typeof(T).Name}");
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Error exporting to Excel: {ex.Message}"));
            }
        }

        #endregion

        #region User Validation

        /// <summary>
        /// Función ValidarUsuario optimizada para uso en controladores
        /// </summary>
        protected async Task<SessionDataDto?> ValidarUsuario()
        {
            try
            {
                return await _permissionService.ValidateUserFromHeadersAsync(Request.Headers);
            }
            catch (SessionExpiredException ex)
            {
                _logger.LogWarning("Session expired: {ErrorCode}", ex.ErrorCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user");
                return null;
            }
        }

        /// <summary>
        /// Valida usuario y verifica permiso específico
        /// </summary>
        protected async Task<(SessionDataDto? user, bool hasPermission)> ValidarUsuarioConPermiso(string permissionKey)
        {
            var user = await ValidarUsuario();
            if (user == null)
                return (null, false);

            var hasPermission = user.Permisos.Contains(permissionKey);
            return (user, hasPermission);
        }

        /// <summary>
        /// Respuesta estándar para usuario no autenticado
        /// </summary>
        protected new IActionResult Unauthorized()
        {
            return StatusCode(401, ApiResponse<object>.ErrorResponse("Usuario no autenticado"));
        }

        /// <summary>
        /// Respuesta estándar para acceso denegado por permisos
        /// </summary>
        protected IActionResult Forbidden(string message = "Acceso denegado")
        {
            return StatusCode(403, ApiResponse<object>.ErrorResponse(message));
        }

        /// <summary>
        /// Obtiene el nombre de la entidad desde la URL del controller
        /// </summary>
        private string GetEntityNameFromController()
        {
            // Obtener el nombre del controller desde la URL
            var controllerName = ControllerContext.ActionDescriptor.ControllerName;
            
            // Convertir de PascalCase a UPPER_CASE
            // Ejemplo: "Categoria" -> "CATEGORIA", "SystemUser" -> "SYSTEM_USER"
            return controllerName.ToUpperInvariant();
        }

        /// <summary>
        /// Construye la clave de permiso automáticamente: ENTIDAD.ACCION
        /// </summary>
        private string BuildPermissionKey(string action)
        {
            var entityName = GetEntityNameFromController();
            return $"{entityName}.{action.ToUpperInvariant()}";
        }

        /// <summary>
        /// Valida usuario y verifica permiso específico construido automáticamente
        /// </summary>
        protected async Task<(SessionDataDto? user, bool hasPermission)> CheckPermissionAsync(string action)
        {
            var user = await ValidarUsuario();
            if (user == null)
                return (null, false);

            var permissionKey = BuildPermissionKey(action);
            var hasPermission = user.Permisos.Contains(permissionKey);
            
            if (!hasPermission)
                _logger.LogWarning("Usuario {UserId} no tiene permiso {Permission}", user.Id, permissionKey);
            
            return (user, hasPermission);
        }

        /// <summary>
        /// Método helper para validar permisos rápidamente en endpoints
        /// Devuelve (user, hasPermission, errorResult)
        /// </summary>
        protected async Task<(SessionDataDto? user, bool hasPermission, IActionResult? errorResult)> ValidatePermissionAsync(string action)
        {
            var (user, hasPermission) = await CheckPermissionAsync(action);
            
            if (user == null)
                return (null, false, Unauthorized());
                
            if (!hasPermission)
                return (user, false, Forbidden($"Requiere permiso: {BuildPermissionKey(action)}"));
                
            return (user, true, null); // null = continuar con la ejecución
        }

        #endregion

    }
}