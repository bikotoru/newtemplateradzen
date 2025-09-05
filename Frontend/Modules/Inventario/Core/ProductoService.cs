using Frontend.Services;
using Shared.Models.Entities;
using Shared.Models.Requests;
using Shared.Models.Responses;
using Shared.Models.Builders;
using Shared.Models.QueryModels;

namespace Frontend.Modules.Inventario.Core
{
    public class ProductoService : BaseApiService<Shared.Models.Entities.Producto>
    {
        public ProductoService(API api, ILogger<ProductoService> logger) 
            : base(api, logger, "api/inventario/core/producto")
        {
        }

        // ✅ Hereda automáticamente todos los métodos base:
        
        // 📋 CRUD Individual:
        // - CreateAsync(CreateRequest<Producto>)
        // - UpdateAsync(UpdateRequest<Producto>)
        // - GetAllPagedAsync(page, pageSize)
        // - GetAllUnpagedAsync()
        // - GetByIdAsync(id)
        // - DeleteAsync(id)
        
        // 📦 CRUD por Lotes:
        // - CreateBatchAsync(CreateBatchRequest<Producto>)
        // - UpdateBatchAsync(UpdateBatchRequest<Producto>)
        
        
        // 🚀 Strongly Typed Query Builder:
        // - Query().Where(c => c.Active).Search("term").InFields(c => c.Nombre).ToListAsync()
        // - Query().Include(c => c.Usuario).OrderBy(c => c.Fecha).ToPagedResultAsync()
        
        // ⚡ Health Check:
        // - HealthCheckAsync()

        // ✅ Solo métodos custom permitidos aquí

        
    }

}