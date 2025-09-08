using Microsoft.AspNetCore.Mvc;
using Backend.Utils.Services;
using Shared.Models.Entities;
using Backend.Controllers;

namespace Backend.Modules.Ventas.Ventas
{
    [Route("api/ventas/venta")]
    public class VentaController : BaseQueryController<Shared.Models.Entities.Venta>
    {
        private readonly VentaService _ventaService;

        public VentaController(VentaService ventaService, ILogger<VentaController> logger, IServiceProvider serviceProvider)
            : base(ventaService, logger, serviceProvider)
        {
            _ventaService = ventaService;
        }

    }
}