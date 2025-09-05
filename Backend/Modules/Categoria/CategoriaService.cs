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

    }
}