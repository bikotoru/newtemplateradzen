using Microsoft.AspNetCore.Mvc;
using Backend.Utils.Services;
using Shared.Models.Entities;
using Backend.Controllers;
using Shared.Models.QueryModels;
using Shared.Models.Responses;

namespace Backend.Modules.Admin.SystemUsers
{
    [Route("api/admin/systemuser")]
    public class SystemUserController : BaseQueryController<Shared.Models.Entities.SystemEntities.SystemUsers>
    {
        private readonly SystemUserService _systemuserService;

        public SystemUserController(SystemUserService systemuserService, ILogger<SystemUserController> logger, IServiceProvider serviceProvider)
            : base(systemuserService, logger, serviceProvider)
        {
            _systemuserService = systemuserService;
        }

        /// <summary>
        /// Obtener usuarios filtrados: Global (OrganizationId = null) + Mi Organización
        /// </summary>
        [HttpPost("view-filtered")]
        public async Task<IActionResult> GetFilteredUsers([FromBody] QueryRequest queryRequest)
        {
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
            if (errorResult != null) return errorResult;

            try
            {
                var result = await _systemuserService.GetFilteredUsersPagedAsync(queryRequest, user);
                return Ok(ApiResponse<Shared.Models.QueryModels.PagedResult<Shared.Models.Entities.SystemEntities.SystemUsers>>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuarios filtrados");
                return StatusCode(500, ApiResponse<PagedResponse<Shared.Models.Entities.SystemEntities.SystemUsers>>.ErrorResponse("Error interno del servidor"));
            }
        }

        /// <summary>
        /// Validar que Username sea único en Global + Mi Organización
        /// </summary>
        [HttpPost("validate-username")]
        public async Task<IActionResult> ValidateUsername([FromBody] ValidateUsernameRequest request)
        {
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("*");
            if (errorResult != null) return errorResult;

            try
            {
                var isValid = await _systemuserService.ValidateUsernameAsync(request.Username, user.OrganizationId, request.ExcludeId);
                return Ok(ApiResponse<bool>.SuccessResponse(isValid));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar username");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Error interno del servidor"));
            }
        }

        /// <summary>
        /// Validar que Email sea único en Global + Mi Organización
        /// </summary>
        [HttpPost("validate-email")]
        public async Task<IActionResult> ValidateEmail([FromBody] ValidateEmailRequest request)
        {
            var (user, hasPermission, errorResult) = await ValidatePermissionAsync("*");
            if (errorResult != null) return errorResult;

            try
            {
                var isValid = await _systemuserService.ValidateEmailAsync(request.Email, user.OrganizationId, request.ExcludeId);
                return Ok(ApiResponse<bool>.SuccessResponse(isValid));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar email");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Error interno del servidor"));
            }
        }

        #region Gestión de Permisos de Usuarios

        /// <summary>
    /// Buscar permisos de un usuario con paginación y filtros
    /// </summary>
    [HttpPost("{userId:guid}/permissions/search")]
    public async Task<IActionResult> SearchUserPermissions(Guid userId, [FromBody] Shared.Models.DTOs.UserPermissions.UserPermissionSearchRequest request)
    {
        var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
        if (errorResult != null) return errorResult;

        try
        {
            request.UserId = userId;
            var result = await _systemuserService.GetUserPermissionsPagedAsync(request, user!);
            
            return Ok(new ApiResponse<PagedResult<Shared.Models.DTOs.UserPermissions.UserPermissionDto>>
            {
                Success = true,
                Data = result,
                Message = "Permisos del usuario obtenidos exitosamente"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar permisos del usuario {UserId}", userId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Error interno del servidor"
            });
        }
    }

    /// <summary>
    /// Actualizar permisos directos de un usuario
    /// </summary>
    [HttpPut("{userId:guid}/permissions")]
    public async Task<IActionResult> UpdateUserPermissions(Guid userId, [FromBody] Shared.Models.DTOs.UserPermissions.UserPermissionUpdateRequest request)
    {
        var (user, hasPermission, errorResult) = await ValidatePermissionAsync("edit");
        if (errorResult != null) return errorResult;

        try
        {
            request.UserId = userId;
            var result = await _systemuserService.UpdateUserPermissionsAsync(request, user!);
            
            if (result)
            {
                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Permisos del usuario actualizados exitosamente"
                });
            }
            else
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "No se pudieron actualizar los permisos del usuario"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar permisos del usuario {UserId}", userId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Error interno del servidor"
            });
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
            var groups = await _systemuserService.GetAvailablePermissionGroupsAsync(user!);
            
            return Ok(new ApiResponse<List<string>>
            {
                Success = true,
                Data = groups,
                Message = "Grupos de permisos obtenidos exitosamente"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener grupos de permisos");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Error interno del servidor"
            });
        }
    }

    /// <summary>
    /// Generar resumen de cambios en permisos de usuario
    /// </summary>
    [HttpPost("{userId:guid}/permissions/changes-summary")]
    public async Task<IActionResult> GetUserPermissionChangesSummary(Guid userId, [FromBody] Shared.Models.DTOs.UserPermissions.UserPermissionUpdateRequest request)
    {
        var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
        if (errorResult != null) return errorResult;

        try
        {
            request.UserId = userId;
            var summary = await _systemuserService.GetChangesSummaryAsync(request, user!);
            
            return Ok(new ApiResponse<Shared.Models.DTOs.UserPermissions.UserPermissionChangesSummary>
            {
                Success = true,
                Data = summary,
                Message = "Resumen de cambios generado exitosamente"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar resumen de cambios para usuario {UserId}", userId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Error interno del servidor"
            });
        }
    }

    #endregion

    #region Gestión de Roles de Usuarios

    /// <summary>
    /// Buscar roles de un usuario con paginación y filtros
    /// </summary>
    [HttpPost("{userId:guid}/roles/search")]
    public async Task<IActionResult> SearchUserRoles(Guid userId, [FromBody] Shared.Models.DTOs.UserRoles.UserRoleSearchRequest request)
    {
        var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
        if (errorResult != null) return errorResult;

        try
        {
            request.UserId = userId;
            var result = await _systemuserService.GetUserRolesPagedAsync(request, user!);
            
            return Ok(new ApiResponse<Shared.Models.QueryModels.PagedResult<Shared.Models.DTOs.UserRoles.UserRoleDto>>
            {
                Success = true,
                Data = result,
                Message = "Roles del usuario obtenidos exitosamente"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar roles del usuario {UserId}", userId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Error interno del servidor"
            });
        }
    }

    /// <summary>
    /// Obtener roles disponibles para asignar a un usuario
    /// </summary>
    [HttpGet("{userId:guid}/roles/available")]
    public async Task<IActionResult> GetAvailableRoles(Guid userId, [FromQuery] string? search = null)
    {
        var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
        if (errorResult != null) return errorResult;

        try
        {
            var roles = await _systemuserService.GetAvailableRolesAsync(userId, user!, search);
            
            return Ok(new ApiResponse<List<Shared.Models.DTOs.UserRoles.AvailableRoleDto>>
            {
                Success = true,
                Data = roles,
                Message = "Roles disponibles obtenidos exitosamente"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener roles disponibles para usuario {UserId}", userId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Error interno del servidor"
            });
        }
    }

    /// <summary>
    /// Asignar rol a un usuario
    /// </summary>
    [HttpPost("{userId:guid}/roles/{roleId:guid}/assign")]
    public async Task<IActionResult> AssignRoleToUser(Guid userId, Guid roleId)
    {
        var (user, hasPermission, errorResult) = await ValidatePermissionAsync("edit");
        if (errorResult != null) return errorResult;

        try
        {
            var result = await _systemuserService.AssignRoleToUserAsync(userId, roleId, user!);
            
            if (result)
            {
                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Rol asignado exitosamente al usuario"
                });
            }
            else
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "No se pudo asignar el rol al usuario"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al asignar rol {RoleId} al usuario {UserId}", roleId, userId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Error interno del servidor"
            });
        }
    }

    /// <summary>
    /// Remover rol de un usuario
    /// </summary>
    [HttpDelete("{userId:guid}/roles/{roleId:guid}")]
    public async Task<IActionResult> RemoveRoleFromUser(Guid userId, Guid roleId)
    {
        var (user, hasPermission, errorResult) = await ValidatePermissionAsync("edit");
        if (errorResult != null) return errorResult;

        try
        {
            var result = await _systemuserService.RemoveRoleFromUserAsync(userId, roleId, user!);
            
            if (result)
            {
                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Rol removido exitosamente del usuario"
                });
            }
            else
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "No se pudo remover el rol del usuario"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al remover rol {RoleId} del usuario {UserId}", roleId, userId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Error interno del servidor"
            });
        }
    }

    #endregion
}

// DTOs para validación
public class ValidateUsernameRequest
{
    public string Username { get; set; } = string.Empty;
    public Guid? ExcludeId { get; set; }
}

public class ValidateEmailRequest
{
    public string Email { get; set; } = string.Empty;
    public Guid? ExcludeId { get; set; }
}
}