using Frontend.Services;
using System.Text.Json;

namespace Frontend.Services;

/// <summary>
/// Servicio para obtener entidades disponibles desde la API
/// </summary>
public class AvailableEntitiesService
{
    private readonly API _api;
    private readonly ILogger<AvailableEntitiesService> _logger;
    private readonly string _baseUrl = "api/AvailableEntities";

    public AvailableEntitiesService(API api, ILogger<AvailableEntitiesService> logger)
    {
        _api = api;
        _logger = logger;
    }

    /// <summary>
    /// Obtener todas las entidades disponibles para campos de referencia
    /// </summary>
    public async Task<AvailableEntitiesResponse> GetAvailableEntitiesAsync()
    {
        try
        {
            _logger.LogInformation("Getting available entities from API");

            var response = await _api.GetAsync<AvailableEntitiesResponse>(_baseUrl);

            if (response.Success && response.Data != null)
            {
                _logger.LogInformation("Successfully retrieved {Count} available entities", response.Data.Count);
                return response.Data;
            }

            _logger.LogWarning("Failed to get available entities: {Message}", response.Message);
            return new AvailableEntitiesResponse
            {
                Success = false,
                Message = response.Message ?? "Error getting available entities",
                Data = new List<AvailableEntityDto>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available entities");
            return new AvailableEntitiesResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}",
                Data = new List<AvailableEntityDto>()
            };
        }
    }

    /// <summary>
    /// Obtener entidades por categoría
    /// </summary>
    public async Task<AvailableEntitiesResponse> GetAvailableEntitiesByCategoryAsync(string category)
    {
        try
        {
            _logger.LogInformation("Getting available entities for category: {Category}", category);

            var response = await _api.GetAsync<AvailableEntitiesResponse>($"{_baseUrl}/by-category/{category}");

            if (response.Success && response.Data != null)
            {
                return response.Data;
            }

            return new AvailableEntitiesResponse
            {
                Success = false,
                Message = response.Message ?? "Error getting entities by category",
                Data = new List<AvailableEntityDto>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entities by category {Category}", category);
            return new AvailableEntitiesResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}",
                Data = new List<AvailableEntityDto>()
            };
        }
    }

    /// <summary>
    /// Obtener categorías disponibles
    /// </summary>
    public async Task<CategoriesResponse> GetAvailableCategoriesAsync()
    {
        try
        {
            var response = await _api.GetAsync<CategoriesResponse>($"{_baseUrl}/categories");

            if (response.Success && response.Data != null)
            {
                return response.Data;
            }

            return new CategoriesResponse
            {
                Success = false,
                Message = response.Message ?? "Error getting categories",
                Data = new List<string>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories");
            return new CategoriesResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}",
                Data = new List<string>()
            };
        }
    }

    /// <summary>
    /// Obtener campos disponibles para una entidad específica
    /// </summary>
    public async Task<EntityFieldsResponse> GetEntityFieldsAsync(string entityName)
    {
        try
        {
            _logger.LogInformation("Getting fields for entity: {EntityName}", entityName);

            var response = await _api.GetAsync<EntityFieldsResponse>($"{_baseUrl}/{entityName}/fields");

            if (response.Success && response.Data != null)
            {
                _logger.LogInformation("Successfully retrieved {TableFields} table fields and {CustomFields} custom fields for entity {EntityName}",
                    response.Data.Data.TableFields.Count, response.Data.Data.CustomFields.Count, entityName);

                return response.Data;
            }

            _logger.LogWarning("Failed to get fields for entity {EntityName}: {Message}", entityName, response.Message);
            return new EntityFieldsResponse
            {
                Success = false,
                Message = response.Message ?? $"Error getting fields for entity {entityName}",
                Data = new EntityFieldsDto()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fields for entity {EntityName}", entityName);
            return new EntityFieldsResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}",
                Data = new EntityFieldsDto()
            };
        }
    }
}

/// <summary>
/// DTO para entidad disponible (duplicado del API para evitar dependencias)
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