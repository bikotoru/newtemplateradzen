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

        public CategoriaController(CategoriaService categoriaService, ILogger<CategoriaController> logger)
            : base(categoriaService, logger)
        {
            _categoriaService = categoriaService;
        }

        // ✅ Hereda automáticamente todos los endpoints SEALED:
        // POST /api/categoria/create
        // PUT /api/categoria/update  
        // GET /api/categoria/all?page=1&pageSize=10&all=false
        // GET /api/categoria/{id}
        // DELETE /api/categoria/{id}
        // POST /api/categoria/create-batch
        // PUT /api/categoria/update-batch
        // GET /api/categoria/health

    }
        // ✅ Los DTOs antiguos ya no son necesarios
        // ✅ Ahora se usan CreateRequest<Categoria> y UpdateRequest<Categoria>
        // ✅ Con EntityRequestBuilder para construcción fuertemente tipada
}