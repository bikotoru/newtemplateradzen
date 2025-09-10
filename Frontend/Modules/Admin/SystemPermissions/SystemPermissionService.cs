using Frontend.Services;
using Shared.Models.Entities;
using Shared.Models.Requests;
using Shared.Models.Responses;
using Shared.Models.Builders;
using Shared.Models.QueryModels;
using Shared.Models.DTOs.UserPermissions;
using Shared.Models.DTOs.RolePermissions;

namespace Frontend.Modules.Admin.SystemPermissions
{
    public class SystemPermissionService : BaseApiService<Shared.Models.Entities.SystemEntities.SystemPermissions>
    {
        public SystemPermissionService(API api, ILogger<SystemPermissionService> logger) 
            : base(api, logger, "api/admin/systempermission")
        {
        }

        // ✅ Hereda automáticamente todos los métodos base:
        
        // 📋 CRUD Individual:
        // - CreateAsync(CreateRequest<SystemPermission>)
        // - UpdateAsync(UpdateRequest<SystemPermission>)
        // - GetAllPagedAsync(page, pageSize)
        // - GetAllUnpagedAsync()
        // - GetByIdAsync(id)
        // - DeleteAsync(id)
        
        // 📦 CRUD por Lotes:
        // - CreateBatchAsync(CreateBatchRequest<SystemPermission>)
        // - UpdateBatchAsync(UpdateBatchRequest<SystemPermission>)
        
        
        // 🚀 Strongly Typed Query Builder:
        // - Query().Where(c => c.Active).Search("term").InFields(c => c.Nombre).ToListAsync()
        // - Query().Include(c => c.Usuario).OrderBy(c => c.Fecha).ToPagedResultAsync()
        
        // ⚡ Health Check:
        // - HealthCheckAsync()

        // ✅ Solo métodos custom permitidos aquí

        /// <summary>
        /// Validar ActionKey único en tiempo real
        /// </summary>
        public async Task<ApiResponse<bool>> ValidateActionKeyAsync(string actionKey, Guid? excludeId = null)
        {
            var request = new { ActionKey = actionKey, ExcludeId = excludeId };
            return await _api.PostAsync<bool>($"{_baseUrl}/validate-action-key", request);
        }

        /// <summary>
        /// Obtener grupos existentes para el dropdown
        /// </summary>
        public async Task<ApiResponse<List<string>>> GetGruposExistentesAsync()
        {
            return await _api.GetAsync<List<string>>($"{_baseUrl}/grupos-existentes");
        }

        /// <summary>
        /// Obtener usuarios que tienen un permiso específico (paginado)
        /// </summary>
        public async Task<PagedResult<PermissionUserDto>?> GetPermissionUsersPagedAsync(PermissionUserSearchRequest request)
        {
            var response = await _api.PostAsync<PagedResult<PermissionUserDto>>($"{_baseUrl}/permission-users-paged", request);
            return response.Success ? response.Data : null;
        }

        /// <summary>
        /// Obtener roles que tienen un permiso específico (paginado)
        /// </summary>
        public async Task<PagedResult<PermissionRoleDto>?> GetPermissionRolesPagedAsync(PermissionRoleSearchRequest request)
        {
            var response = await _api.PostAsync<PagedResult<PermissionRoleDto>>($"{_baseUrl}/permission-roles-paged", request);
            return response.Success ? response.Data : null;
        }
    }

}