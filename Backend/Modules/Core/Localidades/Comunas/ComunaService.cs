using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Backend.Utils.Data;
using Backend.Utils.Services;
using Shared.Models.Entities;

namespace Backend.Modules.Core.Localidades.Comunas
{
    public class ComunaService : BaseQueryService<Shared.Models.Entities.Comuna>
    {
        public ComunaService(AppDbContext context, ILogger<ComunaService> logger) 
            : base(context, logger)
        {
        }

    }
}