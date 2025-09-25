using Frontend.Services;
using Shared.Models.Entities;
using Shared.Models.Requests;
using Shared.Models.Responses;
using Shared.Models.Builders;
using Shared.Models.QueryModels;

namespace Frontend.Modules.Core.Localidades.Comunas
{
    public class ComunaService : BaseApiService<Shared.Models.Entities.Comuna>
    {
        public ComunaService(API api, ILogger<ComunaService> logger) 
            : base(api, logger, "api/core/localidades/comuna")
        {
        }

        // ✅ Hereda automáticamente todos los métodos base:
        
        // 📋 CRUD Individual:
        // - CreateAsync(CreateRequest<Comuna>)
        // - UpdateAsync(UpdateRequest<Comuna>)
        // - GetAllPagedAsync(page, pageSize)
        // - GetAllUnpagedAsync()
        // - GetByIdAsync(id)
        // - DeleteAsync(id)
        
        // 📦 CRUD por Lotes:
        // - CreateBatchAsync(CreateBatchRequest<Comuna>)
        // - UpdateBatchAsync(UpdateBatchRequest<Comuna>)
        
        
        // 🚀 Strongly Typed Query Builder:
        // - Query().Where(c => c.Active).Search("term").InFields(c => c.Nombre).ToListAsync()
        // - Query().Include(c => c.Usuario).OrderBy(c => c.Fecha).ToPagedResultAsync()
        
        // ⚡ Health Check:
        // - HealthCheckAsync()

        // ✅ Solo métodos custom permitidos aquí

        
    }

}