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
        
    }

}