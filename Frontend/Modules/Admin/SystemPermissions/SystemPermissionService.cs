using Frontend.Services;
using Shared.Models.Entities;
using Shared.Models.Requests;
using Shared.Models.Responses;
using Shared.Models.Builders;
using Shared.Models.QueryModels;

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

        
    }

}