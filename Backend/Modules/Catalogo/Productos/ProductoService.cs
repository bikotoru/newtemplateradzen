using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Backend.Utils.Data;
using Backend.Utils.Services;
using Shared.Models.Entities;

namespace Backend.Modules.Catalogo.Productos
{
    public class ProductoService : BaseQueryService<Shared.Models.Entities.Producto>
    {
        public ProductoService(AppDbContext context, ILogger<ProductoService> logger) 
            : base(context, logger)
        {
        }

    }
}