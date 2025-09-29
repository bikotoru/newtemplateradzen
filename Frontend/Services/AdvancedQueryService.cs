using System.Text.Json;
using System.Reflection;
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

            // Crear campos básicos dinámicamente ejecutando una consulta de muestra
            var fields = await GetFieldDefinitionsDynamicallyAsync(entityName);

            _logger.LogInformation("Retrieved {Count} field definitions for entity {EntityName}", fields.Count, entityName);
            return fields;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting field definitions for entity {EntityName}", entityName);
            return new List<EntityFieldDefinition>();
        }
    }

    /// <summary>
    /// Obtener definiciones de campos dinámicamente usando reflexión del modelo
    /// </summary>
    private async Task<List<EntityFieldDefinition>> GetFieldDefinitionsDynamicallyAsync(string entityName)
    {
        try
        {
            // Primero intentar obtener campos del modelo usando reflexión
            var modelFields = GetFieldsFromModel(entityName);
            if (modelFields.Any())
            {
                _logger.LogInformation("Found {Count} fields using model reflection for entity {EntityName}", modelFields.Count, entityName);
                return modelFields;
            }

            // Si no encuentra el modelo, intentar con datos de muestra como fallback
            _logger.LogWarning("Could not find model for entity {EntityName}, trying sample data approach", entityName);
            return await GetFieldsFromSampleData(entityName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting field definitions dynamically for entity {EntityName}", entityName);
            return CreateFallbackFieldDefinitions();
        }
    }

    /// <summary>
    /// Obtener campos del modelo usando reflexión
    /// </summary>
    private List<EntityFieldDefinition> GetFieldsFromModel(string entityName)
    {
        try
        {
            // Buscar el tipo del modelo en los assemblies cargados
            var modelType = FindModelType(entityName);
            if (modelType == null)
            {
                _logger.LogWarning("Could not find model type for entity {EntityName}", entityName);
                return new List<EntityFieldDefinition>();
            }

            var fields = new List<EntityFieldDefinition>();

            // Obtener todas las propiedades públicas del modelo
            var properties = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                // Crear definición de campo desde la propiedad
                var fieldDefinition = CreateFieldDefinitionFromProperty(property);
                if (fieldDefinition != null)
                {
                    fields.Add(fieldDefinition);
                }
            }

            // Aplicar reglas de visibilidad
            ApplyCommonFieldRules(fields);

            return fields.OrderBy(f => f.SortOrder).ThenBy(f => f.DisplayName).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fields from model for entity {EntityName}", entityName);
            return new List<EntityFieldDefinition>();
        }
    }

    /// <summary>
    /// Buscar el tipo del modelo en el namespace específico
    /// </summary>
    private Type? FindModelType(string entityName)
    {
        try
        {
            // Buscar en Shared.Models assembly primero (lugar principal de los modelos)
            var sharedModelsAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Shared.Models");

            if (sharedModelsAssembly != null)
            {
                // Buscar en el namespace específico: Shared.Models.Entities.{EntityName}
                var fullTypeName = $"Shared.Models.Entities.{entityName}";
                var modelType = sharedModelsAssembly.GetType(fullTypeName, false, true); // ignoreCase = true

                if (modelType != null)
                {
                    _logger.LogInformation("Found model type {TypeName} for entity {EntityName}", modelType.FullName, entityName);
                    return modelType;
                }

                // Si no se encuentra con el nombre exacto, buscar en todo el namespace Entities
                var allTypes = sharedModelsAssembly.GetTypes()
                    .Where(t => t.Namespace != null &&
                               t.Namespace.StartsWith("Shared.Models.Entities") &&
                               t.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase) &&
                               t.IsClass && !t.IsAbstract);

                var foundType = allTypes.FirstOrDefault();
                if (foundType != null)
                {
                    _logger.LogInformation("Found model type {TypeName} for entity {EntityName} in Entities namespace", foundType.FullName, entityName);
                    return foundType;
                }
            }

            // Fallback: buscar en otros assemblies si no se encuentra en Shared.Models
            var otherAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name != "Shared.Models" &&
                           (a.GetName().Name?.Contains("Models") == true || a.GetName().Name?.Contains("Entities") == true));

            foreach (var assembly in otherAssemblies)
            {
                var fallbackType = assembly.GetTypes()
                    .FirstOrDefault(t => t.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase) &&
                                        t.IsClass && !t.IsAbstract);

                if (fallbackType != null)
                {
                    _logger.LogInformation("Found model type {TypeName} for entity {EntityName} in assembly {AssemblyName}",
                                         fallbackType.FullName, entityName, assembly.GetName().Name);
                    return fallbackType;
                }
            }

            _logger.LogWarning("Could not find model type for entity {EntityName} in namespace Shared.Models.Entities", entityName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding model type for entity {EntityName}", entityName);
            return null;
        }
    }

    /// <summary>
    /// Crear definición de campo desde propiedad del modelo
    /// </summary>
    private EntityFieldDefinition? CreateFieldDefinitionFromProperty(PropertyInfo property)
    {
        var propertyName = property.Name;

        // Excluir CustomFields solamente
        if (propertyName.ToLower().Contains("customfield"))
        {
            return null;
        }

        // Para propiedades de navegación (clases complejas), incluirlas pero marcarlas como no searchables
        var isNavigationProperty = property.PropertyType.IsClass &&
                                  property.PropertyType != typeof(string) &&
                                  !property.PropertyType.IsArray;

        var displayName = ConvertToDisplayName(propertyName);
        var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        return new EntityFieldDefinition
        {
            PropertyName = propertyName,
            DisplayName = displayName,
            PropertyType = propertyType,
            IsNullable = IsNullableProperty(property),
            IsSearchable = !isNavigationProperty && IsSearchableProperty(property), // No searchable si es navegación
            FieldCategory = GetFieldCategory(propertyName),
            SortOrder = GetFieldSortOrder(propertyName)
        };
    }

    /// <summary>
    /// Verificar si una propiedad es nullable
    /// </summary>
    private bool IsNullableProperty(PropertyInfo property)
    {
        return Nullable.GetUnderlyingType(property.PropertyType) != null ||
               !property.PropertyType.IsValueType ||
               property.PropertyType == typeof(string);
    }

    /// <summary>
    /// Verificar si una propiedad es searchable
    /// </summary>
    private bool IsSearchableProperty(PropertyInfo property)
    {
        var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        // Los tipos básicos son searchables
        return propertyType.IsPrimitive ||
               propertyType == typeof(string) ||
               propertyType == typeof(DateTime) ||
               propertyType == typeof(decimal) ||
               propertyType == typeof(Guid);
    }

    /// <summary>
    /// Obtener campos de datos de muestra (método de fallback)
    /// </summary>
    private async Task<List<EntityFieldDefinition>> GetFieldsFromSampleData(string entityName)
    {
        try
        {
            // Obtener entidades disponibles para encontrar la configuración de la entidad
            var entitiesResponse = await _api.GetAsync<List<AvailableEntityDto>>("api/form-designer/entities/available", BackendType.FormBackend);
            if (!entitiesResponse.Success || entitiesResponse.Data == null)
            {
                return CreateFallbackFieldDefinitions();
            }

            var entity = entitiesResponse.Data.FirstOrDefault(e =>
                string.Equals(e.EntityName, entityName, StringComparison.OrdinalIgnoreCase));

            if (entity == null)
            {
                return CreateFallbackFieldDefinitions();
            }

            // Ejecutar una consulta para obtener un objeto de muestra
            var sampleRequest = new AdvancedQueryRequest
            {
                Filters = new CompositeFilterDescriptor[0],
                Take = 1
            };

            var sampleResponse = await ExecuteAdvancedQueryAsync(entityName, sampleRequest, entity.BackendApi);

            if (sampleResponse.Success && sampleResponse.Data != null && sampleResponse.Data.Any())
            {
                var sampleObject = sampleResponse.Data.First();
                return CreateFieldDefinitionsFromObject(sampleObject);
            }
            else
            {
                return CreateFallbackFieldDefinitions();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fields from sample data for entity {EntityName}", entityName);
            return CreateFallbackFieldDefinitions();
        }
    }

    /// <summary>
    /// Crear definiciones de campos analizando un objeto de muestra
    /// </summary>
    private List<EntityFieldDefinition> CreateFieldDefinitionsFromObject(object sampleObject)
    {
        var fields = new List<EntityFieldDefinition>();

        if (sampleObject is JsonElement jsonElement)
        {
            foreach (var property in jsonElement.EnumerateObject())
            {
                var fieldDefinition = CreateFieldDefinitionFromJsonProperty(property);
                if (fieldDefinition != null)
                {
                    fields.Add(fieldDefinition);
                }
            }
        }

        // Aplicar reglas de visibilidad para campos comunes
        ApplyCommonFieldRules(fields);

        return fields.OrderBy(f => f.SortOrder).ThenBy(f => f.DisplayName).ToList();
    }

    /// <summary>
    /// Crear definición de campo desde una propiedad JSON
    /// </summary>
    private EntityFieldDefinition? CreateFieldDefinitionFromJsonProperty(JsonProperty property)
    {
        var propertyName = property.Name;

        // Excluir CustomFields por ahora como solicitó el usuario
        if (propertyName.ToLower().Contains("customfield"))
        {
            return null;
        }

        var propertyType = GetPropertyTypeFromJsonValue(property.Value);
        var displayName = ConvertToDisplayName(propertyName);

        return new EntityFieldDefinition
        {
            PropertyName = propertyName,
            DisplayName = displayName,
            PropertyType = propertyType,
            IsSearchable = true,
            FieldCategory = GetFieldCategory(propertyName),
            SortOrder = GetFieldSortOrder(propertyName)
        };
    }

    /// <summary>
    /// Aplicar reglas de visibilidad para campos comunes según especificaciones del usuario
    /// </summary>
    private void ApplyCommonFieldRules(List<EntityFieldDefinition> fields)
    {
        var commonFields = new[] { "Id", "OrganizationId", "FechaCreacion", "FechaModificacion",
                                 "CreadorId", "ModificadorId", "Active" };

        var visibleCommonFields = new[] { "Id", "FechaCreacion", "FechaModificacion" };

        foreach (var field in fields)
        {
            if (commonFields.Contains(field.PropertyName, StringComparer.OrdinalIgnoreCase))
            {
                // Solo Id, FechaCreacion y FechaModificacion pueden ser visibles de los campos comunes
                if (!visibleCommonFields.Contains(field.PropertyName, StringComparer.OrdinalIgnoreCase))
                {
                    field.IsVisible = false; // Campo común pero no visible
                }
                else
                {
                    field.IsVisible = true;
                    field.IsSelectedByDefault = field.PropertyName.Equals("Id", StringComparison.OrdinalIgnoreCase); // Solo ID seleccionado por defecto
                }

                field.FieldCategory = "System";
                field.SortOrder = GetSystemFieldSortOrder(field.PropertyName);
            }
            else
            {
                // Campos de negocio - todos visibles pero no seleccionados por defecto
                field.IsVisible = true;
                field.IsSelectedByDefault = false;
                field.FieldCategory = "Business";
            }
        }
    }

    /// <summary>
    /// Obtener tipo de propiedad desde valor JSON
    /// </summary>
    private Type GetPropertyTypeFromJsonValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => typeof(string),
            JsonValueKind.Number => typeof(decimal),
            JsonValueKind.True or JsonValueKind.False => typeof(bool),
            JsonValueKind.Object => typeof(object),
            JsonValueKind.Array => typeof(Array),
            _ => typeof(string)
        };
    }

    /// <summary>
    /// Convertir nombre de propiedad a nombre para mostrar
    /// </summary>
    private string ConvertToDisplayName(string propertyName)
    {
        // Convertir PascalCase a palabras separadas
        return System.Text.RegularExpressions.Regex.Replace(propertyName,
            "([a-z])([A-Z])", "$1 $2");
    }

    /// <summary>
    /// Obtener categoría del campo
    /// </summary>
    private string GetFieldCategory(string propertyName)
    {
        var systemFields = new[] { "Id", "OrganizationId", "FechaCreacion", "FechaModificacion",
                                 "CreadorId", "ModificadorId", "Active" };

        return systemFields.Contains(propertyName, StringComparer.OrdinalIgnoreCase) ? "System" : "Business";
    }

    /// <summary>
    /// Obtener orden de clasificación para campos
    /// </summary>
    private int GetFieldSortOrder(string propertyName)
    {
        // ID siempre primero
        if (propertyName.Equals("Id", StringComparison.OrdinalIgnoreCase))
            return 1;

        return GetFieldCategory(propertyName) == "System" ? 100 : 200;
    }

    /// <summary>
    /// Obtener orden específico para campos del sistema
    /// </summary>
    private int GetSystemFieldSortOrder(string propertyName)
    {
        return propertyName.ToLower() switch
        {
            "id" => 1,
            "fechacreacion" => 2,
            "fechamodificacion" => 3,
            "organizationid" => 4,
            "creadorid" => 5,
            "modificadorid" => 6,
            "active" => 7,
            _ => 100
        };
    }

    /// <summary>
    /// Crear definiciones de campos básicas de respaldo
    /// </summary>
    private List<EntityFieldDefinition> CreateFallbackFieldDefinitions()
    {
        var fields = new List<EntityFieldDefinition>
        {
            new EntityFieldDefinition
            {
                PropertyName = "Id",
                DisplayName = "ID",
                PropertyType = typeof(Guid),
                IsSearchable = true,
                FieldCategory = "System",
                IsVisible = true,
                IsSelectedByDefault = true,
                SortOrder = 1
            },
            new EntityFieldDefinition
            {
                PropertyName = "FechaCreacion",
                DisplayName = "Fecha Creación",
                PropertyType = typeof(DateTime),
                IsSearchable = true,
                FieldCategory = "System",
                IsVisible = true,
                IsSelectedByDefault = false,
                SortOrder = 2
            },
            new EntityFieldDefinition
            {
                PropertyName = "FechaModificacion",
                DisplayName = "Fecha Modificación",
                PropertyType = typeof(DateTime),
                IsSearchable = true,
                FieldCategory = "System",
                IsVisible = true,
                IsSelectedByDefault = false,
                SortOrder = 3
            }
        };

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
                // Usar URL específica de la base de datos
                var baseEndpoint = $"{backendApi}/query";
                // Si hay Select, usar endpoint de select
                endpoint = !string.IsNullOrEmpty(request.Select) ? $"{backendApi}/select-paged" : baseEndpoint;
                targetBackend = BackendType.GlobalBackend; // Asumir GlobalBackend para URLs específicas
                _logger.LogInformation("Using specific API endpoint: {Endpoint}", endpoint);
            }
            else
            {
                // Usar patrón estándar con BackendType
                targetBackend = GetBackendType(backendApi);
                // Si hay Select, usar endpoint de select-paged
                endpoint = !string.IsNullOrEmpty(request.Select) ? $"api/{entityName}/select-paged" : $"api/{entityName}/paged";
                _logger.LogInformation("Using standard pattern: {Endpoint} (BackendType: {BackendType}) - Select: {HasSelect}", endpoint, targetBackend, !string.IsNullOrEmpty(request.Select));
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
                        var options = new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            PropertyNameCaseInsensitive = true
                        };
                        var items = JsonSerializer.Deserialize<List<object>>(jsonElement.GetRawText(), options) ?? new List<object>();
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
                            var options = new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                PropertyNameCaseInsensitive = true
                            };
                            var pagedResult = JsonSerializer.Deserialize<PagedResult<object>>(jsonElement.GetRawText(), options);
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
                            var options = new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                PropertyNameCaseInsensitive = true
                            };
                            var items = JsonSerializer.Deserialize<List<object>>(dataProp.GetRawText(), options) ?? new List<object>();
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
    public string FieldCategory { get; set; } = ""; // "System" or "Business"
    public string? Description { get; set; }
    public bool IsVisible { get; set; } = true; // Si el campo es visible para selección
    public bool IsSelectedByDefault { get; set; } = false; // Si el campo viene seleccionado por defecto
    public int SortOrder { get; set; } = 100; // Orden de clasificación
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