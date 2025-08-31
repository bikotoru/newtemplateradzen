using Microsoft.AspNetCore.Mvc;
using Backend.Controllers;
using Shared.Models.Entities;
using Shared.Models.QueryModels;

namespace Backend.Modules.Categoria
{
    [Route("api/[controller]")]
    public class CategoriaController : BaseQueryController
    {
        private readonly CategoriaService _categoriaService;

        public CategoriaController(CategoriaService categoriaService, ILogger<CategoriaController> logger) 
            : base(logger)
        {
            _categoriaService = categoriaService;
        }

        [HttpGet]
        public async Task<ActionResult<List<Shared.Models.Entities.Categoria>>> GetAll()
        {
            try
            {
                var categorias = await _categoriaService.GetAllAsync();
                return Ok(categorias);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all categorias");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Shared.Models.Entities.Categoria>> GetById(Guid id)
        {
            try
            {
                var categoria = await _categoriaService.GetByIdAsync(id);
                if (categoria == null)
                    return NotFound($"Categoria with ID {id} not found");

                return Ok(categoria);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categoria by ID: {Id}", id);
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<ActionResult<Shared.Models.Entities.Categoria>> Create([FromBody] CreateCategoriaRequest request)
        {
            try
            {
                var categoria = await _categoriaService.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = categoria.Id }, categoria);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating categoria");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Shared.Models.Entities.Categoria>> Update(Guid id, [FromBody] UpdateCategoriaRequest request)
        {
            try
            {
                var categoria = await _categoriaService.UpdateAsync(id, request);
                if (categoria == null)
                    return NotFound($"Categoria with ID {id} not found");

                return Ok(categoria);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating categoria: {Id}", id);
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                var result = await _categoriaService.DeleteAsync(id);
                if (!result)
                    return NotFound($"Categoria with ID {id} not found");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting categoria: {Id}", id);
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        // Heredamos automáticamente los métodos Query, Select, QueryPaged, SelectPaged del BaseQueryController
        // Estos métodos genéricos siguen funcionando para queries complejas
    }

    // DTOs para las requests
    public class CreateCategoriaRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public Guid? OrganizationId { get; set; }
    }

    public class UpdateCategoriaRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool Active { get; set; } = true;
    }
}