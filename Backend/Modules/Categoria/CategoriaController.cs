using Microsoft.AspNetCore.Mvc;
using Backend.Utils.Services;
using Shared.Models.Entities;
using Backend.Controllers;

namespace Backend.Modules.Categoria
{
    [Route("api/[controller]")]
    public class CategoriaController : BaseQueryController<Shared.Models.Entities.Categoria>
    {
        private readonly CategoriaService _categoriaService;

        public CategoriaController(CategoriaService categoriaService, ILogger<CategoriaController> logger, IServiceProvider serviceProvider)
            : base(categoriaService, logger, serviceProvider)
        {
            _categoriaService = categoriaService;
        }

    }
}