using Frontend.Services;
using Shared.Models.Entities;
using Shared.Models.Requests;
using Shared.Models.Responses;
using Shared.Models.Builders;
using Shared.Models.QueryModels;
using Shared.Models.DTOs.RolePermissions;

namespace Frontend.Modules.Admin.SystemRoles
{
    public class SystemRoleService : BaseApiService<Shared.Models.Entities.SystemEntities.SystemRoles>
    {
        public SystemRoleService(API api, ILogger<SystemRoleService> logger) 
            : base(api, logger, "api/admin/systemrole")
        {
        }

        // ✅ Hereda automáticamente todos los métodos base:
        
        // 📋 CRUD Individual:
        // - CreateAsync(CreateRequest<SystemRole>)
        // - UpdateAsync(UpdateRequest<SystemRole>)
        // - GetAllPagedAsync(page, pageSize)
        // - GetAllUnpagedAsync()
        // - GetByIdAsync(id)
        // - DeleteAsync(id)
        
        // 📦 CRUD por Lotes:
        // - CreateBatchAsync(CreateBatchRequest<SystemRole>)
        // - UpdateBatchAsync(UpdateBatchRequest<SystemRole>)
        
        
        // 🚀 Strongly Typed Query Builder:
        // - Query().Where(c => c.Active).Search("term").InFields(c => c.Nombre).ToListAsync()
        // - Query().Include(c => c.Usuario).OrderBy(c => c.Fecha).ToPagedResultAsync()
        
        // ⚡ Health Check:
        // - HealthCheckAsync()

        // ✅ Solo métodos custom permitidos aquí

        #region Métodos de Gestión de Permisos

        /// <summary>
        /// Obtener permisos de un rol con paginación y búsqueda
        /// </summary>
        public async Task<ApiResponse<PagedResult<RolePermissionDto>>> GetRolePermissionsPagedAsync(Guid roleId, RolePermissionSearchRequest request)
        {
            try
            {
                var endpoint = $"{_baseUrl}/{roleId}/permissions/search";
                return await _api.PostAsync<PagedResult<RolePermissionDto>>(endpoint, request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener permisos del rol {RoleId}", roleId);
                return ApiResponse<PagedResult<RolePermissionDto>>.ErrorResponse($"Error al obtener permisos del rol: {ex.Message}");
            }
        }

        /// <summary>
        /// Actualizar permisos de un rol
        /// </summary>
        public async Task<ApiResponse<bool>> UpdateRolePermissionsAsync(Guid roleId, RolePermissionUpdateRequest request)
        {
            try
            {
                var endpoint = $"{_baseUrl}/{roleId}/permissions";
                return await _api.PutAsync<bool>(endpoint, request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar permisos del rol {RoleId}", roleId);
                return ApiResponse<bool>.ErrorResponse($"Error al actualizar permisos del rol: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtener grupos de permisos disponibles
        /// </summary>
        public async Task<ApiResponse<List<string>>> GetAvailablePermissionGroupsAsync()
        {
            try
            {
                var endpoint = $"{_baseUrl}/permissions/groups";
                return await _api.GetAsync<List<string>>(endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener grupos de permisos disponibles");
                return ApiResponse<List<string>>.ErrorResponse($"Error al obtener grupos de permisos: {ex.Message}");
            }
        }

        /// <summary>
        /// Generar resumen de cambios antes de aplicar
        /// </summary>
        public async Task<ApiResponse<RolePermissionChangesSummary>> GetChangesSummaryAsync(Guid roleId, RolePermissionUpdateRequest request)
        {
            try
            {
                var endpoint = $"{_baseUrl}/{roleId}/permissions/changes-summary";
                return await _api.PostAsync<RolePermissionChangesSummary>(endpoint, request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar resumen de cambios para rol {RoleId}", roleId);
                return ApiResponse<RolePermissionChangesSummary>.ErrorResponse($"Error al generar resumen de cambios: {ex.Message}");
            }
        }

        #endregion
        
    }

}