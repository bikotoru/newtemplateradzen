using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Backend.Utils.Data;
using Backend.Utils.Services;
using Shared.Models.Entities;

namespace Backend.Modules.Catalogo.Marcas
{
    public class MarcaService : BaseQueryService<Shared.Models.Entities.Marca>
    {
        public MarcaService(AppDbContext context, ILogger<MarcaService> logger) 
            : base(context, logger)
        {
        }

    }
}