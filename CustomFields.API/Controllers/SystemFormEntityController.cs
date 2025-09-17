using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Backend.Utils.Services;
using Backend.Utils.Data;
using Shared.Models.Entities.SystemEntities;

namespace CustomFields.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SystemFormEntityController : Backend.Controllers.BaseQueryController<SystemFormEntity>
    {
        private readonly AppDbContext _context;

        public SystemFormEntityController(
            BaseQueryService<SystemFormEntity> baseService,
            ILogger<SystemFormEntityController> logger,
            IServiceProvider serviceProvider,
            AppDbContext context)
            : base(baseService, logger, serviceProvider)
        {
            _context = context;
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
                _logger.LogInformation($"Getting available SystemFormEntity for user {user!.Id}");

                // Construir query base con filtros de seguridad
                var query = _context.Set<SystemFormEntity>()
                    .Where(x => x.Active == true &&
                               (x.OrganizationId == null || x.OrganizationId == user.OrganizationId))
                    .OrderBy(x => x.Category)
                    .ThenBy(x => x.DisplayName);

                // Aplicar filtros adicionales
                if (!string.IsNullOrEmpty(search))
                {
                    var searchTerm = search.ToLower();
                    query = (IOrderedQueryable<SystemFormEntity>)query.Where(x =>
                        x.EntityName.ToLower().Contains(searchTerm) ||
                        x.DisplayName.ToLower().Contains(searchTerm) ||
                        (x.Description != null && x.Description.ToLower().Contains(searchTerm)));
                }

                if (!string.IsNullOrEmpty(category))
                {
                    query = (IOrderedQueryable<SystemFormEntity>)query.Where(x => x.Category == category);
                }

                if (allowCustomFields.HasValue)
                {
                    query = (IOrderedQueryable<SystemFormEntity>)query.Where(x => x.AllowCustomFields == allowCustomFields.Value);
                }

                // Aplicar paginación
                var totalCount = query.Count();
                var entities = query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var response = new
                {
                    Entities = entities,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(Shared.Models.Responses.ApiResponse<object>.SuccessResponse(
                    response,
                    $"Retrieved {entities.Count} available entities"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available SystemFormEntity");
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
                _logger.LogInformation($"Getting SystemFormEntity by name: {entityName} for user {user!.Id}");

                var entity = _context.Set<SystemFormEntity>()
                    .Where(x => x.Active == true &&
                               x.EntityName == entityName &&
                               (x.OrganizationId == null || x.OrganizationId == user.OrganizationId))
                    .FirstOrDefault();

                if (entity == null)
                {
                    return NotFound(Shared.Models.Responses.ApiResponse<SystemFormEntity>.ErrorResponse(
                        $"Entity '{entityName}' not found or access denied"));
                }

                return Ok(Shared.Models.Responses.ApiResponse<SystemFormEntity>.SuccessResponse(
                    entity,
                    "Entity retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting SystemFormEntity by name: {entityName}");
                return StatusCode(500, Shared.Models.Responses.ApiResponse<SystemFormEntity>.ErrorResponse(
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
                _logger.LogInformation($"Getting SystemFormEntity categories for user {user!.Id}");

                var categories = _context.Set<SystemFormEntity>()
                    .Where(x => x.Active == true &&
                               (x.OrganizationId == null || x.OrganizationId == user.OrganizationId))
                    .Select(x => x.Category)
                    .Where(x => x != null)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList()
                    .Where(x => x != null)
                    .Cast<string>()
                    .ToList();

                return Ok(Shared.Models.Responses.ApiResponse<List<string>>.SuccessResponse(
                    categories,
                    $"Retrieved {categories.Count} categories"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting SystemFormEntity categories");
                return StatusCode(500, Shared.Models.Responses.ApiResponse<List<string>>.ErrorResponse(
                    $"Error getting categories: {ex.Message}"));
            }
        }

        /// <summary>
        /// Override del query base para aplicar siempre filtros de seguridad por organización
        /// </summary>
        public override async Task<IActionResult> Query([FromBody] Shared.Models.QueryModels.QueryRequest queryRequest)
        {
            // Validar usuario y permiso
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
            if (errorResult != null) return errorResult;

            try
            {
                _logger.LogInformation($"Executing secure query for SystemFormEntity for user {user!.Id}");

                // Agregar filtros de seguridad automáticamente
                var securityFilter = $"(OrganizationId == null || OrganizationId == \"{user.OrganizationId}\") && Active == true";

                // Combinar con filtros existentes
                if (!string.IsNullOrEmpty(queryRequest.Filter))
                {
                    queryRequest.Filter = $"({queryRequest.Filter}) && ({securityFilter})";
                }
                else
                {
                    queryRequest.Filter = securityFilter;
                }

                // Ejecutar query base con filtros de seguridad aplicados
                var result = await _baseService.QueryAsync(queryRequest, user);
                return Ok(Shared.Models.Responses.ApiResponse<List<SystemFormEntity>>.SuccessResponse(
                    result,
                    "Secure query executed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing secure query for SystemFormEntity");
                return StatusCode(500, Shared.Models.Responses.ApiResponse<List<SystemFormEntity>>.ErrorResponse(
                    $"Error executing secure query: {ex.Message}"));
            }
        }

        /// <summary>
        /// Override del query paginado para aplicar siempre filtros de seguridad por organización
        /// </summary>
        public override async Task<IActionResult> QueryPaged([FromBody] Shared.Models.QueryModels.QueryRequest queryRequest)
        {
            // Validar usuario y permiso
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
            if (errorResult != null) return errorResult;

            try
            {
                _logger.LogInformation($"Executing secure paged query for SystemFormEntity for user {user!.Id}");

                // Agregar filtros de seguridad automáticamente
                var securityFilter = $"(OrganizationId == null || OrganizationId == \"{user.OrganizationId}\") && Active == true";

                // Combinar con filtros existentes
                if (!string.IsNullOrEmpty(queryRequest.Filter))
                {
                    queryRequest.Filter = $"({queryRequest.Filter}) && ({securityFilter})";
                }
                else
                {
                    queryRequest.Filter = securityFilter;
                }

                // Ejecutar query base con filtros de seguridad aplicados
                var result = await _baseService.QueryPagedAsync(queryRequest, user);
                return Ok(Shared.Models.Responses.ApiResponse<Shared.Models.QueryModels.PagedResult<SystemFormEntity>>.SuccessResponse(
                    result,
                    "Secure paged query executed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing secure paged query for SystemFormEntity");
                return StatusCode(500, Shared.Models.Responses.ApiResponse<Shared.Models.QueryModels.PagedResult<SystemFormEntity>>.ErrorResponse(
                    $"Error executing secure paged query: {ex.Message}"));
            }
        }
    }
}