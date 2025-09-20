using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Utils.Data;
using Shared.Models.Entities.SystemEntities;

namespace CustomFields.API.Controllers;

/// <summary>
/// API para obtener entidades disponibles para campos de referencia
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AvailableEntitiesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<AvailableEntitiesController> _logger;

    public AvailableEntitiesController(AppDbContext context, ILogger<AvailableEntitiesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Obtener todas las entidades disponibles para campos de referencia
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<AvailableEntitiesResponse>> GetAvailableEntities()
    {
        try
        {
            _logger.LogInformation("Getting available entities for reference fields");

            // Obtener entidades desde system_form_entities
            var formEntities = await _context.SystemFormEntities
                .Where(e => e.Active && e.AllowCustomFields)
                .AsNoTracking()
                .OrderBy(e => e.DisplayName)
                .Select(e => new AvailableEntityDto
                {
                    EntityName = e.EntityName,
                    DisplayName = e.DisplayName,
                    Description = e.Description,
                    Category = e.Category,
                    IconName = e.IconName
                })
                .ToListAsync();

            var response = new AvailableEntitiesResponse
            {
                Success = true,
                Data = formEntities,
                Message = $"Found {formEntities.Count} available entities",
                Count = formEntities.Count
            };

            _logger.LogInformation("Successfully retrieved {Count} available entities", formEntities.Count);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available entities");

            var errorResponse = new AvailableEntitiesResponse
            {
                Success = false,
                Data = new List<AvailableEntityDto>(),
                Message = $"Error getting available entities: {ex.Message}",
                Count = 0
            };

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Obtener entidades disponibles por categoría
    /// </summary>
    [HttpGet("by-category/{category}")]
    public async Task<ActionResult<AvailableEntitiesResponse>> GetAvailableEntitiesByCategory(string category)
    {
        try
        {
            _logger.LogInformation("Getting available entities for category: {Category}", category);

            var formEntities = await _context.SystemFormEntities
                .Where(e => e.Active && e.AllowCustomFields && e.Category == category)
                .AsNoTracking()
                .OrderBy(e => e.DisplayName)
                .Select(e => new AvailableEntityDto
                {
                    EntityName = e.EntityName,
                    DisplayName = e.DisplayName,
                    Description = e.Description,
                    Category = e.Category,
                    IconName = e.IconName
                })
                .ToListAsync();

            var response = new AvailableEntitiesResponse
            {
                Success = true,
                Data = formEntities,
                Message = $"Found {formEntities.Count} entities in category {category}",
                Count = formEntities.Count
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entities by category {Category}", category);

            var errorResponse = new AvailableEntitiesResponse
            {
                Success = false,
                Data = new List<AvailableEntityDto>(),
                Message = $"Error getting entities by category: {ex.Message}",
                Count = 0
            };

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Obtener categorías disponibles
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<CategoriesResponse>> GetAvailableCategories()
    {
        try
        {
            var categories = await _context.SystemFormEntities
                .Where(e => e.Active && e.AllowCustomFields && !string.IsNullOrEmpty(e.Category))
                .AsNoTracking()
                .Select(e => e.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var response = new CategoriesResponse
            {
                Success = true,
                Data = categories,
                Message = $"Found {categories.Count} categories",
                Count = categories.Count
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories");

            var errorResponse = new CategoriesResponse
            {
                Success = false,
                Data = new List<string>(),
                Message = $"Error getting categories: {ex.Message}",
                Count = 0
            };

            return StatusCode(500, errorResponse);
        }
    }
}

/// <summary>
/// DTO para entidad disponible
/// </summary>
public class AvailableEntityDto
{
    public string EntityName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? IconName { get; set; }
}

/// <summary>
/// Respuesta de entidades disponibles
/// </summary>
public class AvailableEntitiesResponse
{
    public bool Success { get; set; }
    public List<AvailableEntityDto> Data { get; set; } = new();
    public string Message { get; set; } = "";
    public int Count { get; set; }
}

/// <summary>
/// Respuesta de categorías
/// </summary>
public class CategoriesResponse
{
    public bool Success { get; set; }
    public List<string> Data { get; set; } = new();
    public string Message { get; set; } = "";
    public int Count { get; set; }
}