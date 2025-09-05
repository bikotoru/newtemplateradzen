using Frontend.Services;
using Shared.Models.Entities;
using Shared.Models.Requests;
using Shared.Models.Responses;
using Shared.Models.Builders;
using Shared.Models.QueryModels;

namespace Frontend.Modules.Inventario.Core
{
    public class MarcaService : BaseApiService<Shared.Models.Entities.Marca>
    {
        public MarcaService(API api, ILogger<MarcaService> logger) 
            : base(api, logger, "api/inventario/core/marca")
        {
        }

        // ✅ Hereda automáticamente todos los métodos base:
        
        // 📋 CRUD Individual:
        // - CreateAsync(CreateRequest<Marca>)
        // - UpdateAsync(UpdateRequest<Marca>)
        // - GetAllPagedAsync(page, pageSize)
        // - GetAllUnpagedAsync()
        // - GetByIdAsync(id)
        // - DeleteAsync(id)
        
        // 📦 CRUD por Lotes:
        // - CreateBatchAsync(CreateBatchRequest<Marca>)
        // - UpdateBatchAsync(UpdateBatchRequest<Marca>)
        
        
        // 🚀 Strongly Typed Query Builder:
        // - Query().Where(c => c.Active).Search("term").InFields(c => c.Nombre).ToListAsync()
        // - Query().Include(c => c.Usuario).OrderBy(c => c.Fecha).ToPagedResultAsync()
        
        // ⚡ Health Check:
        // - HealthCheckAsync()

        // ✅ Solo métodos custom permitidos aquí

        
    }

}