using Microsoft.AspNetCore.Mvc;
using Backend.Utils.Services;
using Shared.Models.Entities;
using Backend.Controllers;

namespace Backend.Modules.Core.Localidades.Regions
{
    [Route("api/core/localidades/region")]
    public class RegionController : BaseQueryController<Shared.Models.Entities.Region>
    {
        private readonly RegionService _regionService;

        public RegionController(RegionService regionService, ILogger<RegionController> logger, IServiceProvider serviceProvider)
            : base(regionService, logger, serviceProvider)
        {
            _regionService = regionService;
        }

    }
}