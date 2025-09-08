using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Backend.Utils.Data;
using Backend.Utils.Services;
using Shared.Models.Entities;

namespace Backend.Modules.Ventas.Ventas
{
    public class VentaService : BaseQueryService<Shared.Models.Entities.Venta>
    {
        public VentaService(AppDbContext context, ILogger<VentaService> logger) 
            : base(context, logger)
        {
        }

    }
}