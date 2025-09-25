using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Backend.Utils.Data;
using Backend.Utils.Services;
using Shared.Models.Entities;

namespace Backend.Modules.Core.Localidades.Regions
{
    public class RegionService : BaseQueryService<Shared.Models.Entities.Region>
    {
        public RegionService(AppDbContext context, ILogger<RegionService> logger) 
            : base(context, logger)
        {
        }

    }
}