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

    /// <summary>
    /// Obtener campos disponibles para una entidad específica (excluyendo strings + custom fields)
    /// </summary>
    [HttpGet("{entityName}/fields")]
    public async Task<ActionResult<EntityFieldsResponse>> GetEntityFields(string entityName)
    {
        try
        {
            _logger.LogInformation("Getting fields for entity: {EntityName}", entityName);

            // 1. Obtener información de la entidad desde system_form_entities
            var formEntity = await _context.SystemFormEntities
                .Where(e => e.EntityName == entityName && e.Active)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (formEntity == null)
            {
                return NotFound(new EntityFieldsResponse
                {
                    Success = false,
                    Message = $"Entity '{entityName}' not found or not active",
                    Data = new EntityFieldsDto()
                });
            }

            // 2. Obtener campos de la tabla física (excluyendo strings)
            var tableFields = await GetTableFields(formEntity.TableName);

            // 3. Obtener campos custom de la entidad
            var customFields = await GetCustomFields(entityName);

            // 4. Combinar resultados
            var result = new EntityFieldsDto
            {
                EntityName = entityName,
                DisplayName = formEntity.DisplayName,
                TableName = formEntity.TableName,
                TableFields = tableFields,
                CustomFields = customFields,
                DefaultValueField = "Id" // Siempre Id por defecto
            };

            var response = new EntityFieldsResponse
            {
                Success = true,
                Data = result,
                Message = $"Found {tableFields.Count} table fields and {customFields.Count} custom fields for entity {entityName}"
            };

            _logger.LogInformation("Successfully retrieved fields for entity {EntityName}: {TableFields} table fields, {CustomFields} custom fields",
                entityName, tableFields.Count, customFields.Count);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fields for entity {EntityName}", entityName);

            var errorResponse = new EntityFieldsResponse
            {
                Success = false,
                Data = new EntityFieldsDto(),
                Message = $"Error getting fields for entity: {ex.Message}"
            };

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Obtener campos de la tabla física excluyendo tipos string
    /// </summary>
    private async Task<List<TableFieldDto>> GetTableFields(string tableName)
    {
        try
        {
            // Query para obtener columnas de INFORMATION_SCHEMA excluyendo tipos string
            var sql = @"
                SELECT
                    COLUMN_NAME as ColumnName,
                    DATA_TYPE as DataType,
                    IS_NULLABLE as IsNullable,
                    COLUMN_DEFAULT as DefaultValue
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = {0}
                AND DATA_TYPE NOT IN ('varchar', 'nvarchar', 'text', 'ntext', 'char', 'nchar')
                AND COLUMN_NAME != 'CustomFields'  -- Excluir la columna CustomFields
                ORDER BY ORDINAL_POSITION";

            var fields = await _context.Database
                .SqlQueryRaw<TableFieldRaw>(sql, tableName)
                .AsNoTracking()
                .ToListAsync();

            return fields.Select(f => new TableFieldDto
            {
                FieldName = f.ColumnName,
                DataType = f.DataType,
                IsNullable = f.IsNullable == "YES",
                DefaultValue = f.DefaultValue,
                DisplayName = GetFriendlyFieldName(f.ColumnName)
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting table fields for table: {TableName}", tableName);
            return new List<TableFieldDto>();
        }
    }

    /// <summary>
    /// Obtener campos custom para la entidad
    /// </summary>
    private async Task<List<CustomFieldDto>> GetCustomFields(string entityName)
    {
        try
        {
            var customFields = await _context.Set<Shared.Models.Entities.SystemEntities.SystemCustomFieldDefinitions>()
                .Where(f => f.EntityName == entityName && f.Active)
                .AsNoTracking()
                .OrderBy(f => f.SortOrder)
                .Select(f => new CustomFieldDto
                {
                    FieldName = f.FieldName,
                    DisplayName = f.DisplayName,
                    FieldType = f.FieldType,
                    Description = f.Description
                })
                .ToListAsync();

            return customFields;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting custom fields for entity: {EntityName}", entityName);
            return new List<CustomFieldDto>();
        }
    }

    /// <summary>
    /// Convertir nombre de campo técnico a nombre amigable
    /// </summary>
    private string GetFriendlyFieldName(string columnName)
    {
        // Convertir nombres técnicos a nombres amigables
        return columnName switch
        {
            "Id" => "ID",
            "FechaCreacion" => "Fecha de Creación",
            "FechaModificacion" => "Fecha de Modificación",
            "CreadorId" => "Creador",
            "ModificadorId" => "Modificador",
            "OrganizationId" => "Organización",
            "Active" => "Activo",
            "SortOrder" => "Orden",
            _ => columnName // Usar el nombre original si no hay mapeo específico
        };
    }
}

/// <summary>
/// DTO para campos de tabla física
/// </summary>
public class TableFieldDto
{
    public string FieldName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string DataType { get; set; } = "";
    public bool IsNullable { get; set; }
    public string? DefaultValue { get; set; }
}

/// <summary>
/// DTO para campos custom
/// </summary>
public class CustomFieldDto
{
    public string FieldName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string FieldType { get; set; } = "";
    public string? Description { get; set; }
}

/// <summary>
/// DTO completo de campos de entidad
/// </summary>
public class EntityFieldsDto
{
    public string EntityName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string TableName { get; set; } = "";
    public List<TableFieldDto> TableFields { get; set; } = new();
    public List<CustomFieldDto> CustomFields { get; set; } = new();
    public string DefaultValueField { get; set; } = "Id";
}

/// <summary>
/// Respuesta de campos de entidad
/// </summary>
public class EntityFieldsResponse
{
    public bool Success { get; set; }
    public EntityFieldsDto Data { get; set; } = new();
    public string Message { get; set; } = "";
}

/// <summary>
/// Clase auxiliar para raw SQL query
/// </summary>
public class TableFieldRaw
{
    public string ColumnName { get; set; } = "";
    public string DataType { get; set; } = "";
    public string IsNullable { get; set; } = "";
    public string? DefaultValue { get; set; }
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