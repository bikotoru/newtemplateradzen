using Frontend.Services;
using Shared.Models.Entities;
using Shared.Models.Requests;
using Shared.Models.Responses;
using Shared.Models.Builders;

namespace Frontend.Modules.Categoria
{
    public class CategoriaService : BaseApiService<Shared.Models.Entities.Categoria>
    {
        public CategoriaService(HttpClient httpClient, ILogger<CategoriaService> logger) 
            : base(httpClient, logger, "api/categoria")
        {
        }

        // ✅ Hereda automáticamente todos los métodos base SEALED:
        // - CreateAsync(CreateRequest<Categoria>)
        // - UpdateAsync(UpdateRequest<Categoria>)
        // - GetAllPagedAsync(page, pageSize)
        // - GetAllUnpagedAsync()
        // - GetByIdAsync(id)
        // - DeleteAsync(id)
        // - CreateBatchAsync(CreateBatchRequest<Categoria>)
        // - UpdateBatchAsync(UpdateBatchRequest<Categoria>)
        // - HealthCheckAsync()

        // ✅ Solo métodos custom permitidos aquí



    }

}