using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Backend.Utils.Data;
using Backend.Utils.Services;
using Shared.Models.Entities;

namespace Backend.Modules.Categoria
{
    public class CategoriaService : BaseQueryService<Shared.Models.Entities.Categoria>
    {
        public CategoriaService(AppDbContext context, ILogger<CategoriaService> logger) 
            : base(context, logger)
        {
        }

        // ✅ Hereda automáticamente todos los métodos base:
        // - CreateAsync(CreateRequest<Categoria>)
        // - UpdateAsync(UpdateRequest<Categoria>)
        // - GetAllPagedAsync(page, pageSize)
        // - GetAllUnpagedAsync()
        // - GetByIdAsync(id)
        // - DeleteAsync(id)
        // - CreateBatchAsync(CreateBatchRequest<Categoria>)
        // - UpdateBatchAsync(UpdateBatchRequest<Categoria>)

    }
}