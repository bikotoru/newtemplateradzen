using Microsoft.AspNetCore.Mvc;
using Backend.Utils.Services;
using Shared.Models.Entities;
using Backend.Controllers;
using Shared.Models.DTOs.RolePermissions;
using Shared.Models.DTOs.RoleUsers;
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

        /// <summary>
        /// Obtener usuarios de un rol con paginación y filtros
        /// </summary>
        [HttpPost("{roleId}/users/search")]
        public async Task<IActionResult> GetRoleUsersPaged(Guid roleId, [FromBody] RoleUserSearchRequest request)
        {
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("manageusers");
            if (errorResult != null) return errorResult;

            try
            {
                request.RoleId = roleId; // Asegurar que el roleId del parámetro se use
                var result = await _systemroleService.GetRoleUsersPagedAsync(request, user);
                return Ok(ApiResponse<PagedResult<RoleUserDto>>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuarios del rol {RoleId}", roleId);
                return StatusCode(500, ApiResponse<PagedResult<RoleUserDto>>.ErrorResponse("Error interno del servidor"));
            }
        }

        /// <summary>
        /// Asignar usuario a rol
        /// </summary>
        [HttpPost("{roleId}/users/assign")]
        public async Task<IActionResult> AssignUserToRole(Guid roleId, [FromBody] AssignUserToRoleRequest request)
        {
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("manageusers");
            if (errorResult != null) return errorResult;

            try
            {
                request.RoleId = roleId; // Asegurar que el roleId del parámetro se use
                await _systemroleService.AssignUserToRoleAsync(request, user);
                return Ok(ApiResponse<bool>.SuccessResponse(true, "Usuario asignado al rol exitosamente"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al asignar usuario {UserId} al rol {RoleId}", request.UserId, roleId);
                return StatusCode(500, ApiResponse<bool>.ErrorResponse($"Error al asignar usuario: {ex.Message}"));
            }
        }

        /// <summary>
        /// Remover usuario de rol
        /// </summary>
        [HttpPost("{roleId}/users/remove")]
        public async Task<IActionResult> RemoveUserFromRole(Guid roleId, [FromBody] RemoveUserFromRoleRequest request)
        {
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("manageusers");
            if (errorResult != null) return errorResult;

            try
            {
                request.RoleId = roleId; // Asegurar que el roleId del parámetro se use
                await _systemroleService.RemoveUserFromRoleAsync(request, user);
                return Ok(ApiResponse<bool>.SuccessResponse(true, "Usuario removido del rol exitosamente"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al remover usuario {UserId} del rol {RoleId}", request.UserId, roleId);
                return StatusCode(500, ApiResponse<bool>.ErrorResponse($"Error al remover usuario: {ex.Message}"));
            }
        }
    }
}