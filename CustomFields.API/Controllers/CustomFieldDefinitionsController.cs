using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Utils.Data;
using Shared.Models.Entities.SystemEntities;

namespace CustomFields.API.Controllers;

/// <summary>
/// API para gestión de definiciones de campos personalizados
/// </summary>
[ApiController]
[Route("api/test-customfields")]
public class CustomFieldDefinitionsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<CustomFieldDefinitionsController> _logger;

    public CustomFieldDefinitionsController(AppDbContext context, ILogger<CustomFieldDefinitionsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Crear una nueva definición de campo personalizado
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CustomFieldDefinitionResponse>> CreateCustomFieldDefinition([FromBody] CreateCustomFieldDefinitionRequest request)
    {
        try
        {
            _logger.LogInformation("Creando campo personalizado: {EntityName}.{FieldName} ({FieldType})",
                request.EntityName, request.FieldName, request.FieldType);

            // Validar que no exista un campo con el mismo nombre
            var existingField = await _context.Set<SystemCustomFieldDefinitions>()
                .Where(f => f.EntityName == request.EntityName &&
                           f.FieldName == request.FieldName &&
                           f.Active)
                .FirstOrDefaultAsync();

            if (existingField != null)
            {
                return BadRequest(new CustomFieldDefinitionResponse
                {
                    Success = false,
                    Message = $"Ya existe un campo con el nombre '{request.FieldName}' para la entidad '{request.EntityName}'"
                });
            }

            // Crear la nueva definición
            var fieldDefinition = new SystemCustomFieldDefinitions
            {
                Id = Guid.NewGuid(),
                EntityName = request.EntityName,
                FieldName = request.FieldName,
                DisplayName = request.DisplayName,
                Description = request.Description,
                FieldType = request.FieldType,
                IsRequired = request.IsRequired,
                SortOrder = request.SortOrder,
                IsEnabled = true,
                Version = 1,
                Active = request.Active,
                FechaCreacion = DateTime.UtcNow,
                FechaModificacion = DateTime.UtcNow
            };

            _context.Set<SystemCustomFieldDefinitions>().Add(fieldDefinition);
            await _context.SaveChangesAsync();

            var response = new CustomFieldDefinitionResponse
            {
                Success = true,
                Message = $"Campo personalizado creado exitosamente: {request.FieldName}",
                Data = new SimpleCustomFieldDto
                {
                    Id = fieldDefinition.Id,
                    EntityName = fieldDefinition.EntityName,
                    FieldName = fieldDefinition.FieldName,
                    DisplayName = fieldDefinition.DisplayName,
                    FieldType = fieldDefinition.FieldType,
                    IsRequired = fieldDefinition.IsRequired,
                    IsEnabled = fieldDefinition.IsEnabled,
                    SortOrder = fieldDefinition.SortOrder
                }
            };

            _logger.LogInformation("Campo personalizado creado exitosamente: {Id}", fieldDefinition.Id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creando campo personalizado");

            var errorResponse = new CustomFieldDefinitionResponse
            {
                Success = false,
                Message = $"Error creando campo personalizado: {ex.Message}",
                Data = null
            };

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Obtener todas las definiciones de campos personalizados para una entidad
    /// </summary>
    [HttpGet("entity/{entityName}")]
    public async Task<ActionResult<CustomFieldDefinitionListResponse>> GetCustomFieldDefinitions(string entityName)
    {
        try
        {
            var fields = await _context.Set<SystemCustomFieldDefinitions>()
                .Where(f => f.EntityName == entityName && f.Active)
                .OrderBy(f => f.SortOrder)
                .Select(f => new SimpleCustomFieldDto
                {
                    Id = f.Id,
                    EntityName = f.EntityName,
                    FieldName = f.FieldName,
                    DisplayName = f.DisplayName,
                    FieldType = f.FieldType,
                    IsRequired = f.IsRequired,
                    IsEnabled = f.IsEnabled,
                    SortOrder = f.SortOrder
                })
                .ToListAsync();

            var response = new CustomFieldDefinitionListResponse
            {
                Success = true,
                Data = fields,
                Message = $"Encontrados {fields.Count} campos personalizados para {entityName}",
                Count = fields.Count
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo campos personalizados para entidad {EntityName}", entityName);

            var errorResponse = new CustomFieldDefinitionListResponse
            {
                Success = false,
                Data = new List<SimpleCustomFieldDto>(),
                Message = $"Error obteniendo campos: {ex.Message}",
                Count = 0
            };

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Eliminar una definición de campo personalizado
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<CustomFieldDefinitionResponse>> DeleteCustomFieldDefinition(Guid id)
    {
        try
        {
            var field = await _context.Set<SystemCustomFieldDefinitions>()
                .Where(f => f.Id == id)
                .FirstOrDefaultAsync();

            if (field == null)
            {
                return NotFound(new CustomFieldDefinitionResponse
                {
                    Success = false,
                    Message = $"Campo con ID {id} no encontrado"
                });
            }

            // Soft delete
            field.Active = false;
            field.FechaModificacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var response = new CustomFieldDefinitionResponse
            {
                Success = true,
                Message = $"Campo personalizado eliminado: {field.FieldName}",
                Data = null
            };

            _logger.LogInformation("Campo personalizado eliminado: {Id}", id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando campo personalizado {Id}", id);

            var errorResponse = new CustomFieldDefinitionResponse
            {
                Success = false,
                Message = $"Error eliminando campo: {ex.Message}",
                Data = null
            };

            return StatusCode(500, errorResponse);
        }
    }
}

/// <summary>
/// Request para crear campo personalizado
/// </summary>
public class CreateCustomFieldDefinitionRequest
{
    public string EntityName { get; set; } = "";
    public string FieldName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Description { get; set; }
    public string FieldType { get; set; } = "";
    public bool IsRequired { get; set; } = false;
    public int SortOrder { get; set; } = 0;
    public bool Active { get; set; } = true;
}

/// <summary>
/// DTO simplificado para campo personalizado
/// </summary>
public class SimpleCustomFieldDto
{
    public Guid Id { get; set; }
    public string EntityName { get; set; } = "";
    public string FieldName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string FieldType { get; set; } = "";
    public bool IsRequired { get; set; }
    public bool IsEnabled { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// Respuesta para campo personalizado
/// </summary>
public class CustomFieldDefinitionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public SimpleCustomFieldDto? Data { get; set; }
}

/// <summary>
/// Respuesta para lista de campos personalizados
/// </summary>
public class CustomFieldDefinitionListResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public List<SimpleCustomFieldDto> Data { get; set; } = new();
    public int Count { get; set; }
}