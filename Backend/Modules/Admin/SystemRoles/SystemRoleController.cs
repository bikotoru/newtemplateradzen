using Microsoft.AspNetCore.Mvc;
using Backend.Utils.Services;
using Shared.Models.Entities;
using Backend.Controllers;
using Shared.Models.DTOs.RolePermissions;
using Shared.Models.Responses;

namespace Backend.Modules.Admin.SystemRoles
{
    [Route("api/admin/systemrole")]
    public class SystemRoleController : BaseQueryController<Shared.Models.Entities.SystemEntities.SystemRoles>
    {
        private readonly SystemRoleService _systemroleService;

        public SystemRoleController(SystemRoleService systemroleService, ILogger<SystemRoleController> logger, IServiceProvider serviceProvider)
            : base(systemroleService, logger, serviceProvider)
        {
            _systemroleService = systemroleService;
        }

        /// <summary>
        /// Obtener permisos de un rol con paginación y búsqueda
        /// </summary>
        [HttpPost("{roleId}/permissions/search")]
        public async Task<IActionResult> GetRolePermissionsPaged(Guid roleId, [FromBody] RolePermissionSearchRequest request)
        {
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("managepermissions");
            if (errorResult != null) return errorResult;

            try
            {
                request.RoleId = roleId; // Asegurar que el ID del rol coincida con la URL
                var result = await _systemroleService.GetRolePermissionsPagedAsync(request, user);
                return Ok(ApiResponse<Shared.Models.QueryModels.PagedResult<RolePermissionDto>>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener permisos del rol {RoleId}", roleId);
                return StatusCode(500, ApiResponse<Shared.Models.QueryModels.PagedResult<RolePermissionDto>>.ErrorResponse("Error interno del servidor"));
            }
        }

        /// <summary>
        /// Actualizar permisos de un rol
        /// </summary>
        [HttpPut("{roleId}/permissions")]
        public async Task<IActionResult> UpdateRolePermissions(Guid roleId, [FromBody] RolePermissionUpdateRequest request)
        {
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("addpermissions");
            if (errorResult != null) return errorResult;

            try
            {
                request.RoleId = roleId; // Asegurar que el ID del rol coincida con la URL
                var success = await _systemroleService.UpdateRolePermissionsAsync(request, user);
                
                if (success)
                {
                    return Ok(ApiResponse<bool>.SuccessResponse(true, "Permisos del rol actualizados exitosamente"));
                }
                else
                {
                    return BadRequest(ApiResponse<bool>.ErrorResponse("No se pudieron actualizar los permisos del rol"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar permisos del rol {RoleId}", roleId);
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Error interno del servidor"));
            }
        }

        /// <summary>
        /// Obtener grupos de permisos disponibles
        /// </summary>
        [HttpGet("permissions/groups")]
        public async Task<IActionResult> GetAvailablePermissionGroups()
        {
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
            if (errorResult != null) return errorResult;

            try
            {
                var groups = await _systemroleService.GetAvailablePermissionGroupsAsync(user);
                return Ok(ApiResponse<List<string>>.SuccessResponse(groups));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener grupos de permisos disponibles");
                return StatusCode(500, ApiResponse<List<string>>.ErrorResponse("Error interno del servidor"));
            }
        }

        /// <summary>
        /// Generar resumen de cambios antes de aplicar
        /// </summary>
        [HttpPost("{roleId}/permissions/changes-summary")]
        public async Task<IActionResult> GetChangesSummary(Guid roleId, [FromBody] RolePermissionUpdateRequest request)
        {
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("managepermissions");
            if (errorResult != null) return errorResult;

            try
            {
                request.RoleId = roleId; // Asegurar que el ID del rol coincida con la URL
                var summary = await _systemroleService.GetChangesSummaryAsync(request, user);
                return Ok(ApiResponse<RolePermissionChangesSummary>.SuccessResponse(summary));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar resumen de cambios para rol {RoleId}", roleId);
                return StatusCode(500, ApiResponse<RolePermissionChangesSummary>.ErrorResponse("Error interno del servidor"));
            }
        }
    }
}