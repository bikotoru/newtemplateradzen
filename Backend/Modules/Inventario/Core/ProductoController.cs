using Microsoft.AspNetCore.Mvc;
using Backend.Utils.Services;
using Shared.Models.Entities;
using Backend.Controllers;

namespace Backend.Modules.Inventario.Core
{
    [Route("api/inventario/core/producto")]
    public class ProductoController : BaseQueryController<Shared.Models.Entities.Producto>
    {
        private readonly ProductoService _productoService;

        public ProductoController(ProductoService productoService, ILogger<ProductoController> logger, IServiceProvider serviceProvider)
            : base(productoService, logger, serviceProvider)
        {
            _productoService = productoService;
        }

    }
}