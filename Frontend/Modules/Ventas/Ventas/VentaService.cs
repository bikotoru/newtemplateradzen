using Frontend.Services;
using Shared.Models.Entities;
using Shared.Models.Requests;
using Shared.Models.Responses;
using Shared.Models.Builders;
using Shared.Models.QueryModels;

namespace Frontend.Modules.Ventas.Ventas
{
    public class VentaService : BaseApiService<Shared.Models.Entities.Venta>
    {
        public VentaService(API api, ILogger<VentaService> logger) 
            : base(api, logger, "api/ventas/venta")
        {
        }

        // ✅ Hereda automáticamente todos los métodos base:
        
        // 📋 CRUD Individual:
        // - CreateAsync(CreateRequest<Venta>)
        // - UpdateAsync(UpdateRequest<Venta>)
        // - GetAllPagedAsync(page, pageSize)
        // - GetAllUnpagedAsync()
        // - GetByIdAsync(id)
        // - DeleteAsync(id)
        
        // 📦 CRUD por Lotes:
        // - CreateBatchAsync(CreateBatchRequest<Venta>)
        // - UpdateBatchAsync(UpdateBatchRequest<Venta>)
        
        
        // 🚀 Strongly Typed Query Builder:
        // - Query().Where(c => c.Active).Search("term").InFields(c => c.Nombre).ToListAsync()
        // - Query().Include(c => c.Usuario).OrderBy(c => c.Fecha).ToPagedResultAsync()
        
        // ⚡ Health Check:
        // - HealthCheckAsync()

        // ✅ Solo métodos custom permitidos aquí

        
    }

}