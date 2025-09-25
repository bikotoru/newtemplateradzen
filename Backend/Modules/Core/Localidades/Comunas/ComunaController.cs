using Microsoft.AspNetCore.Mvc;
using Backend.Utils.Services;
using Shared.Models.Entities;
using Backend.Controllers;

namespace Backend.Modules.Core.Localidades.Comunas
{
    [Route("api/core/localidades/comuna")]
    public class ComunaController : BaseQueryController<Shared.Models.Entities.Comuna>
    {
        private readonly ComunaService _comunaService;

        public ComunaController(ComunaService comunaService, ILogger<ComunaController> logger, IServiceProvider serviceProvider)
            : base(comunaService, logger, serviceProvider)
        {
            _comunaService = comunaService;
        }

    }
}