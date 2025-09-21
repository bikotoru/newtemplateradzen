using System.Text.Json;
using Radzen;

namespace Frontend.Services;

/// <summary>
/// Servicio para manejar consultas avanzadas usando RadzenDataFilter
/// </summary>
public class AdvancedQueryService
{
    private readonly API _api;
    private readonly AvailableEntitiesService _entitiesService;
    private readonly ILogger<AdvancedQueryService> _logger;

    public AdvancedQueryService(
        API api,
        AvailableEntitiesService entitiesService,
        ILogger<AdvancedQueryService> logger)
    {
        _api = api;
        _entitiesService = entitiesService;
        _logger = logger;
    }

    /// <summary>
    /// Obtener entidades habilitadas para consultas avanzadas (desde SystemFormEntities)
    /// </summary>
    public async Task<List<AvailableEntityDto>> GetAdvancedQueryEntitiesAsync()
    {
        try
        {
            _logger.LogInformation("Getting entities enabled for advanced queries");

            // Usar el endpoint correcto de SystemFormEntities
            var response = await _api.GetAsync<SystemFormEntitiesResponse>("api/form-designer/entities/available",BackendType.FormBackend);

            if (response.Success && response.Data != null)
            {
                // Convertir SystemFormEntities a AvailableEntityDto
                var entities = response.Data.Entities.Select(entity => new AvailableEntityDto
                {
                    EntityName = entity.EntityName,
                    DisplayName = entity.DisplayName ?? entity.EntityName,
                    Description = entity.Description,
                    Category = entity.Category,
                    IconName = entity.IconName,
                    BackendApi = entity.BackendApi // Incluir el campo BackendApi
                }).ToList();

                _logger.LogInformation("Retrieved {Count} entities for advanced queries", entities.Count);
                return entities;
            }

            _logger.LogWarning("Failed to get advanced query entities: {Message}", response.Message);
            return new List<AvailableEntityDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting advanced query entities");
            return new List<AvailableEntityDto>();
        }
    }

    /// <summary>
    /// Obtener definiciones de campos para una entidad específica usando reflexión
    /// </summary>
    public async Task<List<EntityFieldDefinition>> GetEntityFieldDefinitionsAsync(string entityName)
    {
        try
        {
            _logger.LogInformation("Getting field definitions for entity: {EntityName}", entityName);

            // Por ahora, crear campos básicos usando reflexión del modelo
            var fields = GetBasicFieldDefinitionsForEntity(entityName);

            // TODO: En el futuro se puede integrar con endpoints de campos custom

            _logger.LogInformation("Retrieved {Count} field definitions for entity {EntityName}", fields.Count, entityName);
            return await Task.FromResult(fields);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting field definitions for entity {EntityName}", entityName);
            return new List<EntityFieldDefinition>();
        }
    }

    /// <summary>
    /// Crear definiciones de campos básicos para entidades comunes
    /// </summary>
    private List<EntityFieldDefinition> GetBasicFieldDefinitionsForEntity(string entityName)
    {
        var fields = new List<EntityFieldDefinition>();

        // Campos comunes para todas las entidades
        fields.AddRange(new[]
        {
            new EntityFieldDefinition
            {
                PropertyName = "Id",
                DisplayName = "ID",
                PropertyType = typeof(Guid),
                IsSearchable = true,
                FieldCategory = "System"
            },
            new EntityFieldDefinition
            {
                PropertyName = "FechaCreacion",
                DisplayName = "Fecha de Creación",
                PropertyType = typeof(DateTime),
                IsSearchable = true,
                FieldCategory = "System"
            },
            new EntityFieldDefinition
            {
                PropertyName = "FechaModificacion",
                DisplayName = "Fecha de Modificación",
                PropertyType = typeof(DateTime),
                IsSearchable = true,
                FieldCategory = "System"
            },
            new EntityFieldDefinition
            {
                PropertyName = "Active",
                DisplayName = "Activo",
                PropertyType = typeof(bool),
                IsSearchable = true,
                FieldCategory = "System"
            }
        });

        // Agregar campos específicos según la entidad
        switch (entityName.ToLower())
        {
            case "empleado":
                fields.AddRange(new[]
                {
                    new EntityFieldDefinition { PropertyName = "Nombres", DisplayName = "Nombres", PropertyType = typeof(string), IsSearchable = true, FieldCategory = "Business" },
                    new EntityFieldDefinition { PropertyName = "Apellidos", DisplayName = "Apellidos", PropertyType = typeof(string), IsSearchable = true, FieldCategory = "Business" },
                    new EntityFieldDefinition { PropertyName = "Rut", DisplayName = "RUT", PropertyType = typeof(string), IsSearchable = true, FieldCategory = "Business" },
                    new EntityFieldDefinition { PropertyName = "Email", DisplayName = "Email", PropertyType = typeof(string), IsSearchable = true, FieldCategory = "Business" },
                    new EntityFieldDefinition { PropertyName = "Telefono", DisplayName = "Teléfono", PropertyType = typeof(string), IsSearchable = true, FieldCategory = "Business" },
                    new EntityFieldDefinition { PropertyName = "FechaNacimiento", DisplayName = "Fecha de Nacimiento", PropertyType = typeof(DateTime), IsSearchable = true, FieldCategory = "Business" },
                    new EntityFieldDefinition { PropertyName = "Salario", DisplayName = "Salario", PropertyType = typeof(decimal), IsSearchable = true, FieldCategory = "Business" }
                });
                break;

            case "systemusers":
                fields.AddRange(new[]
                {
                    new EntityFieldDefinition { PropertyName = "Nombre", DisplayName = "Nombre", PropertyType = typeof(string), IsSearchable = true, FieldCategory = "Business" },
                    new EntityFieldDefinition { PropertyName = "Email", DisplayName = "Email", PropertyType = typeof(string), IsSearchable = true, FieldCategory = "Business" },
                    new EntityFieldDefinition { PropertyName = "Username", DisplayName = "Usuario", PropertyType = typeof(string), IsSearchable = true, FieldCategory = "Business" }
                });
                break;

            case "region":
                fields.AddRange(new[]
                {
                    new EntityFieldDefinition { PropertyName = "Nombre", DisplayName = "Nombre", PropertyType = typeof(string), IsSearchable = true, FieldCategory = "Business" },
                    new EntityFieldDefinition { PropertyName = "Codigo", DisplayName = "Código", PropertyType = typeof(string), IsSearchable = true, FieldCategory = "Business" }
                });
                break;
        }

        return fields;
    }

    /// <summary>
    /// Ejecutar consulta avanzada usando filtros de RadzenDataFilter
    /// </summary>
    public async Task<AdvancedQueryResult<object>> ExecuteAdvancedQueryAsync(string entityName, AdvancedQueryRequest request, string? backendApi = null)
    {
        try
        {
            _logger.LogInformation("Executing advanced query for entity: {EntityName}", entityName);

            // Convertir CompositeFilterDescriptor[] a QueryRequest.Filter usando Radzen
            var filterString = ConvertFiltersToLinqString(request.Filters, request.LogicalOperator, request.FilterCaseSensitivity);

            var queryRequest = new QueryRequest
            {
                Filter = filterString,
                OrderBy = request.OrderBy,
                Include = request.Include,
                Select = request.Select,
                Skip = request.Skip,
                Take = request.Take ?? 50 // Default limit
            };

            _logger.LogInformation("Generated filter string: {FilterString}", filterString);

            // Determinar endpoint y backend a usar
            string endpoint;
            BackendType targetBackend;

            if (!string.IsNullOrEmpty(backendApi) && backendApi.StartsWith("api/"))
            {
                // Usar URL específica de la base de datos con /query al final
                endpoint = $"{backendApi}/query";
                targetBackend = BackendType.GlobalBackend; // Asumir GlobalBackend para URLs específicas
                _logger.LogInformation("Using specific API endpoint: {Endpoint}", endpoint);
            }
            else
            {
                // Usar patrón estándar con BackendType
                targetBackend = GetBackendType(backendApi);
                endpoint = $"api/{entityName}/paged";
                _logger.LogInformation("Using standard pattern: {Endpoint} (BackendType: {BackendType})", endpoint, targetBackend);
            }

            // Ejecutar consulta con endpoint dinámico
            var response = await _api.PostAsync<object>(endpoint, queryRequest, targetBackend);

            if (response.Success && response.Data != null)
            {
                try
                {
                    // Convertir response.Data a JsonElement para inspeccionar su tipo
                    var jsonString = JsonSerializer.Serialize(response.Data);
                    var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

                    // Verificar si response.Data ya es directamente un array
                    if (jsonElement.ValueKind == JsonValueKind.Array)
                    {
                        // response.Data ya es el array de datos directamente
                        var items = JsonSerializer.Deserialize<List<object>>(jsonElement.GetRawText()) ?? new List<object>();
                        return new AdvancedQueryResult<object>
                        {
                            Success = true,
                            Data = items,
                            TotalCount = items.Count,
                            Page = 1,
                            PageSize = items.Count,
                            Message = "Query executed successfully"
                        };
                    }
                    // Si es un objeto, verificar si tiene la estructura PagedResult
                    else if (jsonElement.ValueKind == JsonValueKind.Object)
                    {
                        // Intentar como PagedResult primero (tiene propiedades data, totalCount, etc.)
                        if (jsonElement.TryGetProperty("data", out var dataProperty) &&
                            jsonElement.TryGetProperty("totalCount", out _))
                        {
                            // Es un PagedResult
                            var pagedResult = JsonSerializer.Deserialize<PagedResult<object>>(jsonElement.GetRawText());
                            if (pagedResult != null)
                            {
                                return new AdvancedQueryResult<object>
                                {
                                    Success = true,
                                    Data = pagedResult.Data,
                                    TotalCount = pagedResult.TotalCount,
                                    Page = pagedResult.Page,
                                    PageSize = pagedResult.PageSize,
                                    Message = "Query executed successfully"
                                };
                            }
                        }
                        // Si tiene una propiedad "data" que es un array
                        else if (jsonElement.TryGetProperty("data", out var dataProp) &&
                                 dataProp.ValueKind == JsonValueKind.Array)
                        {
                            // Formato envuelto: { "data": [...] }
                            var items = JsonSerializer.Deserialize<List<object>>(dataProp.GetRawText()) ?? new List<object>();
                            return new AdvancedQueryResult<object>
                            {
                                Success = true,
                                Data = items,
                                TotalCount = items.Count,
                                Page = 1,
                                PageSize = items.Count,
                                Message = "Query executed successfully"
                            };
                        }
                    }

                    // Si llegamos aquí, no pudimos parsear el response
                    _logger.LogWarning("Unexpected response format for entity {EntityName}. ValueKind: {ValueKind}",
                                      entityName, jsonElement.ValueKind);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing response data for entity {EntityName}", entityName);
                }
            }

            _logger.LogWarning("Advanced query failed for entity {EntityName}: {Message}", entityName, response.Message);
            return new AdvancedQueryResult<object>
            {
                Success = false,
                Message = response.Message ?? "Query execution failed",
                Data = new List<object>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing advanced query for entity {EntityName}", entityName);
            return new AdvancedQueryResult<object>
            {
                Success = false,
                Message = $"Error executing query: {ex.Message}",
                Data = new List<object>()
            };
        }
    }


    /// <summary>
    /// Guardar configuración de consulta avanzada
    /// </summary>
    public async Task<bool> SaveQueryConfigurationAsync(SavedQueryConfiguration config)
    {
        try
        {
            _logger.LogInformation("Saving query configuration: {Name} for entity {EntityName}", config.Name, config.EntityName);

            // Serializar filtros a JSON
            config.FiltersJson = JsonSerializer.Serialize(config.Filters, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Aquí podrías guardar en una tabla dedicada o como configuración de usuario
            // Por ahora, simulamos guardado exitoso
            await Task.Delay(100);

            _logger.LogInformation("Query configuration saved successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving query configuration");
            return false;
        }
    }

    /// <summary>
    /// Cargar configuraciones guardadas para una entidad
    /// </summary>
    public async Task<List<SavedQueryConfiguration>> GetSavedConfigurationsAsync(string entityName)
    {
        try
        {
            _logger.LogInformation("Getting saved configurations for entity: {EntityName}", entityName);

            // Aquí cargarías desde la base de datos
            // Por ahora retornamos lista vacía
            await Task.Delay(100);

            return new List<SavedQueryConfiguration>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting saved configurations for entity {EntityName}", entityName);
            return new List<SavedQueryConfiguration>();
        }
    }

    #region Helper Methods

    /// <summary>
    /// Convertir string de backend API a BackendType enum
    /// </summary>
    private BackendType GetBackendType(string? backendApi)
    {
        if (string.IsNullOrEmpty(backendApi))
            return BackendType.GlobalBackend; // Default

        return backendApi.ToLower() switch
        {
            "mainbackend" => BackendType.GlobalBackend,
            "globalbackend" => BackendType.GlobalBackend,
            "formbackend" => BackendType.FormBackend,
            "systembackend" => BackendType.GlobalBackend, // Mapear SystemBackend a GlobalBackend por ahora
            _ => BackendType.GlobalBackend // Default para valores desconocidos
        };
    }

    /// <summary>
    /// Convertir filtros de RadzenDataFilter a string LINQ
    /// Implementación manual basada en la lógica de Radzen
    /// </summary>
    private string ConvertFiltersToLinqString(
        CompositeFilterDescriptor[] filters,
        LogicalFilterOperator logicalOperator,
        FilterCaseSensitivity caseSensitivity)
    {
        if (filters == null || !filters.Any())
            return string.Empty;

        try
        {
            var filterParts = new List<string>();

            foreach (var filter in filters)
            {
                var filterPart = ConvertSingleFilter(filter, caseSensitivity);
                if (!string.IsNullOrEmpty(filterPart))
                {
                    filterParts.Add(filterPart);
                }
            }

            if (!filterParts.Any())
                return string.Empty;

            var operatorString = logicalOperator == LogicalFilterOperator.And ? " && " : " || ";
            return string.Join(operatorString, filterParts.Select(f => $"({f})"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting filters to LINQ string");
            return string.Empty;
        }
    }

    /// <summary>
    /// Convertir un filtro individual a string LINQ
    /// </summary>
    private string ConvertSingleFilter(CompositeFilterDescriptor filter, FilterCaseSensitivity caseSensitivity)
    {
        if (filter.Filters != null && filter.Filters.Any())
        {
            // Filtro anidado
            var nestedParts = new List<string>();
            foreach (var nestedFilter in filter.Filters)
            {
                var nestedPart = ConvertSingleFilter(nestedFilter, caseSensitivity);
                if (!string.IsNullOrEmpty(nestedPart))
                {
                    nestedParts.Add(nestedPart);
                }
            }

            if (!nestedParts.Any())
                return string.Empty;

            var operatorString = filter.LogicalFilterOperator == LogicalFilterOperator.And ? " && " : " || ";
            return $"({string.Join(operatorString, nestedParts.Select(f => $"({f})"))})";
        }

        // Filtro simple
        if (string.IsNullOrEmpty(filter.Property) || filter.FilterOperator == null)
            return string.Empty;

        var property = filter.Property;
        var value = filter.FilterValue;
        var op = filter.FilterOperator.Value;

        // Aplicar case sensitivity si es string
        if (caseSensitivity == FilterCaseSensitivity.CaseInsensitive && value is string)
        {
            property = $"{property}.ToLower()";
            value = value.ToString()?.ToLower();
        }

        return op switch
        {
            FilterOperator.Equals => $"{property} == {FormatValue(value)}",
            FilterOperator.NotEquals => $"{property} != {FormatValue(value)}",
            FilterOperator.GreaterThan => $"{property} > {FormatValue(value)}",
            FilterOperator.GreaterThanOrEquals => $"{property} >= {FormatValue(value)}",
            FilterOperator.LessThan => $"{property} < {FormatValue(value)}",
            FilterOperator.LessThanOrEquals => $"{property} <= {FormatValue(value)}",
            FilterOperator.Contains => $"{property}.Contains({FormatValue(value)})",
            FilterOperator.DoesNotContain => $"!{property}.Contains({FormatValue(value)})",
            FilterOperator.StartsWith => $"{property}.StartsWith({FormatValue(value)})",
            FilterOperator.EndsWith => $"{property}.EndsWith({FormatValue(value)})",
            FilterOperator.IsNull => $"{property} == null",
            FilterOperator.IsNotNull => $"{property} != null",
            FilterOperator.IsEmpty => $"{property} == \"\"",
            FilterOperator.IsNotEmpty => $"{property} != \"\"",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Formatear valor para query LINQ
    /// </summary>
    private string FormatValue(object? value)
    {
        return value switch
        {
            null => "null",
            string str => $"\"{str}\"",
            DateTime date => $"DateTime.Parse(\"{date:yyyy-MM-dd HH:mm:ss}\")",
            DateOnly date => $"DateOnly.Parse(\"{date:yyyy-MM-dd}\")",
            bool boolean => boolean.ToString().ToLower(),
            _ => value.ToString() ?? "null"
        };
    }

    /// <summary>
    /// Convertir tipo de datos SQL a Type .NET
    /// </summary>
    private Type GetTypeFromDataType(string dataType)
    {
        return dataType.ToLower() switch
        {
            "varchar" or "nvarchar" or "text" or "ntext" or "char" or "nchar" => typeof(string),
            "int" or "integer" => typeof(int),
            "bigint" => typeof(long),
            "decimal" or "numeric" or "money" => typeof(decimal),
            "float" or "real" => typeof(double),
            "bit" => typeof(bool),
            "datetime" or "datetime2" or "smalldatetime" => typeof(DateTime),
            "date" => typeof(DateOnly),
            "time" => typeof(TimeOnly),
            "uniqueidentifier" => typeof(Guid),
            _ => typeof(string) // Default to string
        };
    }

    /// <summary>
    /// Convertir tipo de campo custom a Type .NET
    /// </summary>
    private Type GetTypeFromFieldType(string fieldType)
    {
        return fieldType.ToLower() switch
        {
            "text" or "textarea" or "email" or "url" => typeof(string),
            "number" or "integer" => typeof(int),
            "decimal" or "currency" => typeof(decimal),
            "boolean" or "checkbox" => typeof(bool),
            "date" => typeof(DateOnly),
            "datetime" => typeof(DateTime),
            "time" => typeof(TimeOnly),
            _ => typeof(string) // Default to string
        };
    }

    #endregion
}

#region DTOs and Models

/// <summary>
/// Definición de campo para RadzenDataFilter
/// </summary>
public class EntityFieldDefinition
{
    public string PropertyName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public Type PropertyType { get; set; } = typeof(string);
    public bool IsNullable { get; set; }
    public bool IsSearchable { get; set; } = true;
    public string FieldCategory { get; set; } = ""; // "Table" or "Custom"
    public string? Description { get; set; }
}

/// <summary>
/// Request para consulta avanzada
/// </summary>
public class AdvancedQueryRequest
{
    public CompositeFilterDescriptor[] Filters { get; set; } = Array.Empty<CompositeFilterDescriptor>();
    public LogicalFilterOperator LogicalOperator { get; set; } = LogicalFilterOperator.And;
    public FilterCaseSensitivity FilterCaseSensitivity { get; set; } = FilterCaseSensitivity.CaseInsensitive;
    public string? OrderBy { get; set; }
    public string[]? Include { get; set; }
    public string? Select { get; set; }
    public int? Skip { get; set; }
    public int? Take { get; set; }
}

/// <summary>
/// Resultado de consulta avanzada
/// </summary>
public class AdvancedQueryResult<T>
{
    public bool Success { get; set; }
    public List<T> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public string Message { get; set; } = "";
}

/// <summary>
/// Configuración guardada de consulta
/// </summary>
public class SavedQueryConfiguration
{
    public Guid Id { get; set; }
    public string EntityName { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }

    // Filtros serializados como JSON
    public string FiltersJson { get; set; } = "";
    public CompositeFilterDescriptor[] Filters { get; set; } = Array.Empty<CompositeFilterDescriptor>();

    public LogicalFilterOperator LogicalOperator { get; set; } = LogicalFilterOperator.And;
    public FilterCaseSensitivity FilterCaseSensitivity { get; set; } = FilterCaseSensitivity.CaseInsensitive;
    public string? OrderBy { get; set; }
    public string[]? Include { get; set; }
    public string? Select { get; set; }
    public int? Take { get; set; }

    public bool IsDefault { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }

    // Helper methods
    public void SetFilters(CompositeFilterDescriptor[] filters)
    {
        Filters = filters;
        FiltersJson = JsonSerializer.Serialize(filters, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    public CompositeFilterDescriptor[] GetFilters()
    {
        if (!string.IsNullOrEmpty(FiltersJson))
        {
            try
            {
                return JsonSerializer.Deserialize<CompositeFilterDescriptor[]>(FiltersJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }) ?? Array.Empty<CompositeFilterDescriptor>();
            }
            catch
            {
                return Array.Empty<CompositeFilterDescriptor>();
            }
        }
        return Filters ?? Array.Empty<CompositeFilterDescriptor>();
    }
}

/// <summary>
/// Respuesta del endpoint de SystemFormEntities
/// </summary>
public class SystemFormEntitiesResponse
{
    public List<SystemFormEntities> Entities { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

#endregion