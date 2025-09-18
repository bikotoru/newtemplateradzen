using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Backend.Utils.Services;
using Backend.Utils.Data;
using Shared.Models.Entities.SystemEntities;
using CustomFields.API.Services;

namespace CustomFields.API.Controllers
{
    [ApiController]
    [Route("api/form-designer/entities")]
    public class SystemFormEntityController : Backend.Controllers.BaseQueryController<SystemFormEntities>
    {
        private readonly AppDbContext _context;
        private readonly SystemFormEntityService _systemFormEntityService;

        public SystemFormEntityController(
            BaseQueryService<SystemFormEntities> baseService,
            SystemFormEntityService systemFormEntityService,
            ILogger<SystemFormEntityController> logger,
            IServiceProvider serviceProvider,
            AppDbContext context)
            : base(baseService, logger, serviceProvider)
        {
            _context = context;
            _systemFormEntityService = systemFormEntityService;
        }

        /// <summary>
        /// Obtener entidades filtradas por organización del usuario
        /// Incluye entidades del sistema (OrganizationId = null) y las específicas de la organización del usuario
        /// </summary>
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableEntities(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? search = null,
            [FromQuery] string? category = null,
            [FromQuery] bool? allowCustomFields = null)
        {
            // Validar usuario y permiso
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
            if (errorResult != null) return errorResult;

            try
            {
                _logger.LogInformation($"Getting available SystemFormEntities for user {user!.Id}");

                // Usar el servicio especializado que maneja correctamente los filtros
                var entities = await _systemFormEntityService.GetAvailableEntitiesAsync(
                    user,
                    search,
                    category,
                    allowCustomFields,
                    (page - 1) * pageSize,
                    pageSize);

                // Para el total count, hacer query separada sin paginación
                var totalCount = await _systemFormEntityService.GetAvailableEntitiesAsync(
                    user,
                    search,
                    category,
                    allowCustomFields,
                    0,
                    int.MaxValue);

                var response = new
                {
                    Entities = entities,
                    TotalCount = totalCount.Count,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount.Count / pageSize)
                };

                return Ok(Shared.Models.Responses.ApiResponse<object>.SuccessResponse(
                    response,
                    $"Retrieved {entities.Count} available entities"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available SystemFormEntities");
                return StatusCode(500, Shared.Models.Responses.ApiResponse<object>.ErrorResponse(
                    $"Error getting available entities: {ex.Message}"));
            }
        }

        /// <summary>
        /// Obtener una entidad específica por nombre, verificando permisos de acceso
        /// </summary>
        [HttpGet("by-name/{entityName}")]
        public async Task<IActionResult> GetByEntityName(string entityName)
        {
            // Validar usuario y permiso
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
            if (errorResult != null) return errorResult;

            try
            {
                _logger.LogInformation($"Getting SystemFormEntities by name: {entityName} for user {user!.Id}");

                // Usar el servicio especializado que maneja correctamente los filtros
                var entity = await _systemFormEntityService.GetByEntityNameAsync(entityName, user);

                if (entity == null)
                {
                    return NotFound(Shared.Models.Responses.ApiResponse<SystemFormEntities>.ErrorResponse(
                        $"Entity '{entityName}' not found or access denied"));
                }

                return Ok(Shared.Models.Responses.ApiResponse<SystemFormEntities>.SuccessResponse(
                    entity,
                    "Entity retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting SystemFormEntities by name: {entityName}");
                return StatusCode(500, Shared.Models.Responses.ApiResponse<SystemFormEntities>.ErrorResponse(
                    $"Error getting entity: {ex.Message}"));
            }
        }

        /// <summary>
        /// Obtener categorías disponibles
        /// </summary>
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            // Validar usuario y permiso
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
            if (errorResult != null) return errorResult;

            try
            {
                _logger.LogInformation($"Getting SystemFormEntities categories for user {user!.Id}");

                // Usar el servicio especializado que maneja correctamente los filtros
                var categories = await _systemFormEntityService.GetCategoriesAsync(user);

                return Ok(Shared.Models.Responses.ApiResponse<List<string>>.SuccessResponse(
                    categories,
                    $"Retrieved {categories.Count} categories"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting SystemFormEntities categories");
                return StatusCode(500, Shared.Models.Responses.ApiResponse<List<string>>.ErrorResponse(
                    $"Error getting categories: {ex.Message}"));
            }
        }

        /// <summary>
        /// Override del query base para usar el servicio especializado que maneja filtros correctamente
        /// </summary>
        public override async Task<IActionResult> Query([FromBody] Shared.Models.QueryModels.QueryRequest queryRequest)
        {
            // Validar usuario y permiso
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
            if (errorResult != null) return errorResult;

            try
            {
                _logger.LogInformation($"Executing secure query for SystemFormEntities for user {user!.Id}");

                // Usar el servicio especializado que automáticamente aplica los filtros correctos
                var result = await _systemFormEntityService.QueryAsync(queryRequest, user);
                return Ok(Shared.Models.Responses.ApiResponse<List<SystemFormEntities>>.SuccessResponse(
                    result,
                    "Secure query executed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing secure query for SystemFormEntities");
                return StatusCode(500, Shared.Models.Responses.ApiResponse<List<SystemFormEntities>>.ErrorResponse(
                    $"Error executing secure query: {ex.Message}"));
            }
        }

        /// <summary>
        /// Override del query paginado para usar el servicio especializado que maneja filtros correctamente
        /// </summary>
        public override async Task<IActionResult> QueryPaged([FromBody] Shared.Models.QueryModels.QueryRequest queryRequest)
        {
            // Validar usuario y permiso
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
            if (errorResult != null) return errorResult;

            try
            {
                _logger.LogInformation($"Executing secure paged query for SystemFormEntities for user {user!.Id}");

                // Usar el servicio especializado que automáticamente aplica los filtros correctos
                var result = await _systemFormEntityService.QueryPagedAsync(queryRequest, user);
                return Ok(Shared.Models.Responses.ApiResponse<Shared.Models.QueryModels.PagedResult<SystemFormEntities>>.SuccessResponse(
                    result,
                    "Secure paged query executed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing secure paged query for SystemFormEntities");
                return StatusCode(500, Shared.Models.Responses.ApiResponse<Shared.Models.QueryModels.PagedResult<SystemFormEntities>>.ErrorResponse(
                    $"Error executing secure paged query: {ex.Message}"));
            }
        }

        #region Override de métodos de permisos para usar FORMDESIGNER

        /// <summary>
        /// Override para validar permisos usando FORMDESIGNER en lugar de SYSTEMFORMENTITY
        /// </summary>
        protected new async Task<(Shared.Models.DTOs.Auth.SessionDataDto? user, bool hasPermission, IActionResult? errorResult)> ValidatePermissionAsync(string action)
        {
            var user = await ValidarUsuario();

            if (user == null)
                return (null, false, Unauthorized());

            // Usar FORMDESIGNER en lugar del nombre de la entidad
            var permissionKey = $"FORMDESIGNER.{action.ToUpperInvariant()}";
            var hasPermission = user.Permisos.Contains(permissionKey);

            if (!hasPermission)
            {
                _logger.LogWarning("Usuario {UserId} no tiene permiso {Permission}", user.Id, permissionKey);
                return (user, false, Forbidden($"Requiere permiso: {permissionKey}"));
            }

            return (user, true, null); // null = continuar con la ejecución
        }

        #endregion
    }
}