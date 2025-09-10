using Microsoft.AspNetCore.Mvc;
using Backend.Utils.Services;
using Shared.Models.Entities;
using Backend.Controllers;
using Shared.Models.QueryModels;
using Shared.Models.Responses;
using Shared.Models.DTOs.UserPermissions;
using Shared.Models.DTOs.RolePermissions;

namespace Backend.Modules.Admin.SystemPermissions
{
    [Route("api/admin/systempermission")]
    public class SystemPermissionController : BaseQueryController<Shared.Models.Entities.SystemEntities.SystemPermissions>
    {
        private readonly SystemPermissionService _systempermissionService;

        public SystemPermissionController(SystemPermissionService systempermissionService, ILogger<SystemPermissionController> logger, IServiceProvider serviceProvider)
            : base(systempermissionService, logger, serviceProvider)
        {
            _systempermissionService = systempermissionService;
        }

        /// <summary>
        /// Obtener permisos filtrados: Global (OrganizationId = null) + Mi Organización
        /// </summary>
        [HttpPost("view-filtered")]
        public async Task<IActionResult> GetFilteredPermissions([FromBody] QueryRequest queryRequest)
        {
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
            if (errorResult != null) return errorResult;

            try
            {
                var result = await _systempermissionService.GetFilteredPermissionsPagedAsync(queryRequest, user);
                return Ok(ApiResponse<Shared.Models.QueryModels.PagedResult<Shared.Models.Entities.SystemEntities.SystemPermissions>>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener permisos filtrados");
                return StatusCode(500, ApiResponse<PagedResponse<Shared.Models.Entities.SystemEntities.SystemPermissions>>.ErrorResponse("Error interno del servidor"));
            }
        }

        /// <summary>
        /// Validar que ActionKey sea único en Global + Mi Organización
        /// </summary>
        [HttpPost("validate-action-key")]
        public async Task<IActionResult> ValidateActionKey([FromBody] ValidateActionKeyRequest request)
        {
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("*");
            if (errorResult != null) return errorResult;

            try
            {
                var isValid = await _systempermissionService.ValidateActionKeyAsync(request.ActionKey, user.OrganizationId, request.ExcludeId);
                return Ok(ApiResponse<bool>.SuccessResponse(isValid));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar ActionKey: {ActionKey}", request.ActionKey);
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Error interno del servidor"));
            }
        }

        /// <summary>
        /// Obtener grupos existentes para el dropdown
        /// </summary>
        [HttpGet("grupos-existentes")]
        public async Task<IActionResult> GetGruposExistentes()
        {
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
            if (errorResult != null) return errorResult;

            try
            {
                var grupos = await _systempermissionService.GetGruposExistentesAsync(user);
                return Ok(ApiResponse<List<string>>.SuccessResponse(grupos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener grupos existentes");
                return StatusCode(500, ApiResponse<List<string>>.ErrorResponse("Error interno del servidor"));
            }
        }

        /// <summary>
        /// Obtener usuarios que tienen un permiso específico (paginado)
        /// </summary>
        [HttpPost("permission-users-paged")]
        public async Task<IActionResult> GetPermissionUsersPagedAsync([FromBody] PermissionUserSearchRequest request)
        {
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
            if (errorResult != null) return errorResult;

            try
            {
                var result = await _systempermissionService.GetPermissionUsersPagedAsync(request, user);
                return Ok(ApiResponse<PagedResult<PermissionUserDto>>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuarios con permiso {PermissionId}", request.PermissionId);
                return StatusCode(500, ApiResponse<PagedResult<PermissionUserDto>>.ErrorResponse("Error interno del servidor"));
            }
        }

        /// <summary>
        /// Obtener roles que tienen un permiso específico (paginado)
        /// </summary>
        [HttpPost("permission-roles-paged")]
        public async Task<IActionResult> GetPermissionRolesPagedAsync([FromBody] PermissionRoleSearchRequest request)
        {
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
            if (errorResult != null) return errorResult;

            try
            {
                var result = await _systempermissionService.GetPermissionRolesPagedAsync(request, user);
                return Ok(ApiResponse<PagedResult<PermissionRoleDto>>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener roles con permiso {PermissionId}", request.PermissionId);
                return StatusCode(500, ApiResponse<PagedResult<PermissionRoleDto>>.ErrorResponse("Error interno del servidor"));
            }
        }
    }

    /// <summary>
    /// Request para validar ActionKey único
    /// </summary>
    public class ValidateActionKeyRequest
    {
        public string ActionKey { get; set; } = string.Empty;
        public Guid? ExcludeId { get; set; }
    }
}