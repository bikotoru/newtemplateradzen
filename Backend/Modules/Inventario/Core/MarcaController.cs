using Microsoft.AspNetCore.Mvc;
using Backend.Utils.Services;
using Shared.Models.Entities;
using Backend.Controllers;

namespace Backend.Modules.Inventario.Core
{
    [Route("api/inventario/core/marca")]
    public class MarcaController : BaseQueryController<Shared.Models.Entities.Marca>
    {
        private readonly MarcaService _marcaService;

        public MarcaController(MarcaService marcaService, ILogger<MarcaController> logger, IServiceProvider serviceProvider)
            : base(marcaService, logger, serviceProvider)
        {
            _marcaService = marcaService;
        }

    }
}