using Frontend.Services;
using Shared.Models.Entities;
using Shared.Models.Requests;
using Shared.Models.Responses;
using Shared.Models.Builders;
using Shared.Models.QueryModels;

namespace Frontend.Modules.Admin.SystemUsers
{
    public class SystemUserService : BaseApiService<Shared.Models.Entities.SystemEntities.SystemUsers>
    {
        public SystemUserService(API api, ILogger<SystemUserService> logger) 
            : base(api, logger, "api/admin/systemuser")
        {
        }

        // ✅ Hereda automáticamente todos los métodos base:
        
        // 📋 CRUD Individual:
        // - CreateAsync(CreateRequest<SystemUser>)
        // - UpdateAsync(UpdateRequest<SystemUser>)
        // - GetAllPagedAsync(page, pageSize)
        // - GetAllUnpagedAsync()
        // - GetByIdAsync(id)
        // - DeleteAsync(id)
        
        // 📦 CRUD por Lotes:
        // - CreateBatchAsync(CreateBatchRequest<SystemUser>)
        // - UpdateBatchAsync(UpdateBatchRequest<SystemUser>)
        
        
        // 🚀 Strongly Typed Query Builder:
        // - Query().Where(c => c.Active).Search("term").InFields(c => c.Nombre).ToListAsync()
        // - Query().Include(c => c.Usuario).OrderBy(c => c.Fecha).ToPagedResultAsync()
        
        // ⚡ Health Check:
        // - HealthCheckAsync()

        // ✅ Solo métodos custom permitidos aquí

        #region Gestión de Permisos de Usuarios

        /// <summary>
        /// Buscar permisos de un usuario con paginación y filtros
        /// </summary>
        public async Task<ApiResponse<PagedResult<Shared.Models.DTOs.UserPermissions.UserPermissionDto>>> GetUserPermissionsPagedAsync(Guid userId, Shared.Models.DTOs.UserPermissions.UserPermissionSearchRequest request)
        {
            try
            {
                _logger.LogInformation("Obteniendo permisos paginados para usuario {UserId}", userId);
                
                var response = await _api.PostAsync<PagedResult<Shared.Models.DTOs.UserPermissions.UserPermissionDto>>(
                    $"{_baseUrl}/{userId}/permissions/search", 
                    request
                );

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener permisos del usuario {UserId}", userId);
                return new ApiResponse<PagedResult<Shared.Models.DTOs.UserPermissions.UserPermissionDto>>
                {
                    Success = false,
                    Message = "Error al obtener permisos del usuario"
                };
            }
        }

        /// <summary>
        /// Actualizar permisos directos de un usuario
        /// </summary>
        public async Task<ApiResponse<bool>> UpdateUserPermissionsAsync(Guid userId, Shared.Models.DTOs.UserPermissions.UserPermissionUpdateRequest request)
        {
            try
            {
                _logger.LogInformation("Actualizando permisos para usuario {UserId}", userId);
                
                var response = await _api.PutAsync<bool>(
                    $"{_baseUrl}/{userId}/permissions", 
                    request
                );

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar permisos del usuario {UserId}", userId);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Error al actualizar permisos del usuario"
                };
            }
        }

        /// <summary>
        /// Obtener grupos de permisos disponibles
        /// </summary>
        public async Task<ApiResponse<List<string>>> GetAvailablePermissionGroupsAsync()
        {
            try
            {
                _logger.LogInformation("Obteniendo grupos de permisos disponibles");
                
                var response = await _api.GetAsync<List<string>>(
                    $"{_baseUrl}/permissions/groups"
                );

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener grupos de permisos");
                return new ApiResponse<List<string>>
                {
                    Success = false,
                    Message = "Error al obtener grupos de permisos"
                };
            }
        }

        /// <summary>
        /// Generar resumen de cambios en permisos de usuario
        /// </summary>
        public async Task<ApiResponse<Shared.Models.DTOs.UserPermissions.UserPermissionChangesSummary>> GetUserPermissionChangesSummaryAsync(Guid userId, Shared.Models.DTOs.UserPermissions.UserPermissionUpdateRequest request)
        {
            try
            {
                _logger.LogInformation("Generando resumen de cambios para usuario {UserId}", userId);
                
                var response = await _api.PostAsync<Shared.Models.DTOs.UserPermissions.UserPermissionChangesSummary>(
                    $"{_baseUrl}/{userId}/permissions/changes-summary", 
                    request
                );

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar resumen de cambios para usuario {UserId}", userId);
                return new ApiResponse<Shared.Models.DTOs.UserPermissions.UserPermissionChangesSummary>
                {
                    Success = false,
                    Message = "Error al generar resumen de cambios"
                };
            }
        }

        #endregion

        #region Gestión de Roles de Usuarios

        /// <summary>
        /// Buscar roles de un usuario con paginación y filtros
        /// </summary>
        public async Task<ApiResponse<PagedResult<Shared.Models.DTOs.UserRoles.UserRoleDto>>> GetUserRolesPagedAsync(Guid userId, Shared.Models.DTOs.UserRoles.UserRoleSearchRequest request)
        {
            try
            {
                _logger.LogInformation("Obteniendo roles paginados para usuario {UserId}", userId);
                
                var response = await _api.PostAsync<PagedResult<Shared.Models.DTOs.UserRoles.UserRoleDto>>(
                    $"{_baseUrl}/{userId}/roles/search", 
                    request
                );

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener roles del usuario {UserId}", userId);
                return new ApiResponse<PagedResult<Shared.Models.DTOs.UserRoles.UserRoleDto>>
                {
                    Success = false,
                    Message = "Error al obtener roles del usuario"
                };
            }
        }

        /// <summary>
        /// Obtener roles disponibles para asignar a un usuario
        /// </summary>
        public async Task<ApiResponse<List<Shared.Models.DTOs.UserRoles.AvailableRoleDto>>> GetAvailableRolesAsync(Guid userId, string? searchTerm = null)
        {
            try
            {
                _logger.LogInformation("Obteniendo roles disponibles para usuario {UserId}", userId);
                
                var url = $"{_baseUrl}/{userId}/roles/available";
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    url += $"?search={Uri.EscapeDataString(searchTerm)}";
                }

                var response = await _api.GetAsync<List<Shared.Models.DTOs.UserRoles.AvailableRoleDto>>(url);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener roles disponibles para usuario {UserId}", userId);
                return new ApiResponse<List<Shared.Models.DTOs.UserRoles.AvailableRoleDto>>
                {
                    Success = false,
                    Message = "Error al obtener roles disponibles"
                };
            }
        }

        /// <summary>
        /// Asignar rol a un usuario
        /// </summary>
        public async Task<ApiResponse<bool>> AssignRoleToUserAsync(Guid userId, Guid roleId)
        {
            try
            {
                _logger.LogInformation("Asignando rol {RoleId} al usuario {UserId}", roleId, userId);
                
                var response = await _api.PostAsync<bool>(
                    $"{_baseUrl}/{userId}/roles/{roleId}/assign",
                    new { } // Empty body
                );

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al asignar rol {RoleId} al usuario {UserId}", roleId, userId);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Error al asignar rol al usuario"
                };
            }
        }

        /// <summary>
        /// Remover rol de un usuario
        /// </summary>
        public async Task<ApiResponse<bool>> RemoveRoleFromUserAsync(Guid userId, Guid roleId)
        {
            try
            {
                _logger.LogInformation("Removiendo rol {RoleId} del usuario {UserId}", roleId, userId);
                
                var response = await _api.DeleteAsync<bool>(
                    $"{_baseUrl}/{userId}/roles/{roleId}"
                );

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al remover rol {RoleId} del usuario {UserId}", roleId, userId);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Error al remover rol del usuario"
                };
            }
        }

        #endregion
        
    }

}