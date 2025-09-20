# 🔗 Integration Guide - Sistema Existente con Campos Personalizados

## 📋 Guía de Integración Completa

Esta documentación detalla cómo integrar el sistema de campos personalizados con la infraestructura existente de AgendaGes, aprovechando al máximo los sistemas ya implementados.

---

## 🏗️ Arquitectura de Integración

### **Diagrama de Componentes**
```
┌─────────────────────────────────┐    ┌─────────────────────────────────┐
│          Frontend Blazor        │    │       CustomFields.API          │
│  ┌─────────────────────────────┐ │    │  ┌─────────────────────────────┐ │
│  │   EmpleadoFormulario.razor  │ │    │  │ CustomFieldDefinitions      │ │
│  │  ┌─────────────────────────┐│ │    │  │       Controller            │ │
│  │  │  CustomFieldsSection    ││ │◄──►│  └─────────────────────────────┘ │
│  │  │      Component          ││ │    │  ┌─────────────────────────────┐ │
│  │  └─────────────────────────┘│ │    │  │ CustomFieldValidation       │ │
│  └─────────────────────────────┘ │    │  │      Controller             │ │
└─────────────────────────────────┘    │  └─────────────────────────────┘ │
                │                      └─────────────────────────────────┘
                ▼                                        │
┌─────────────────────────────────┐                      ▼
│         Backend.API             │    ┌─────────────────────────────────┐
│  ┌─────────────────────────────┐ │    │      Base de Datos Principal    │
│  │    EmpleadoController       │ │    │  ┌─────────────────────────────┐ │
│  │  ┌─────────────────────────┐│ │    │  │       empleados             │ │
│  │  │   BaseQueryService      ││ │    │  │  ┌─────────────────────────┐│ │
│  │  │   + Custom Fields       ││ │◄──►│  │  │  Custom (NVARCHAR)      ││ │
│  │  │     Integration         ││ │    │  │  └─────────────────────────┘│ │
│  │  └─────────────────────────┘│ │    │  └─────────────────────────────┘ │
│  └─────────────────────────────┘ │    │  ┌─────────────────────────────┐ │
│  ┌─────────────────────────────┐ │    │  │  custom_field_definitions   │ │
│  │  SimpleSaveInterceptor      │ │    │  └─────────────────────────────┘ │
│  │  + Custom Fields Validation │ │    └─────────────────────────────────┘
│  └─────────────────────────────┘ │
└─────────────────────────────────┘
```

---

## 🔧 Integración con BaseQueryService

### **Extensión del BaseQueryService Existente**

```csharp
// En Backend.Utils/Services/BaseQueryService.cs
public partial class BaseQueryService<T> where T : class
{
    private readonly ICustomFieldsHttpClient _customFieldsClient;
    
    // Constructor actualizado
    public BaseQueryService(
        DbContext context, 
        ILogger<BaseQueryService<T>> logger,
        ICustomFieldsHttpClient customFieldsClient)
    {
        _context = context;
        _logger = logger;
        _dbSet = _context.Set<T>();
        _customFieldsClient = customFieldsClient;
    }
    
    /// <summary>
    /// Obtener entidad con campos personalizados incluidos
    /// </summary>
    public virtual async Task<T?> GetWithCustomFieldsAsync(Guid id, SessionDataDto sessionData)
    {
        var entity = await GetAsync(id, sessionData);
        if (entity == null) return null;
        
        // Obtener definiciones de campos personalizados
        var entityName = typeof(T).Name;
        var customFieldDefinitions = await _customFieldsClient.GetDefinitionsByEntityAsync(
            entityName, 
            sessionData.OrganizationId);
        
        if (customFieldDefinitions?.Any() == true)
        {
            // Agregar metadatos de campos personalizados
            SetCustomFieldDefinitions(entity, customFieldDefinitions);
        }
        
        return entity;
    }
    
    /// <summary>
    /// Crear entidad con validación de campos personalizados
    /// </summary>
    public virtual async Task<T> CreateWithCustomFieldsAsync(
        CreateRequest<T> request, 
        SessionDataDto sessionData)
    {
        // Validar campos personalizados antes de crear
        await ValidateCustomFieldsAsync(request.Entity, sessionData, isCreate: true);
        
        // Procesar campos personalizados
        await ProcessCustomFieldsAsync(request.Entity, sessionData);
        
        // Crear usando método base
        return await CreateAsync(request, sessionData);
    }
    
    /// <summary>
    /// Actualizar entidad con validación de campos personalizados
    /// </summary>
    public virtual async Task<T> UpdateWithCustomFieldsAsync(
        UpdateRequest<T> request, 
        SessionDataDto sessionData)
    {
        // Validar campos personalizados antes de actualizar
        await ValidateCustomFieldsAsync(request.Entity, sessionData, isCreate: false);
        
        // Procesar campos personalizados
        await ProcessCustomFieldsAsync(request.Entity, sessionData);
        
        // Actualizar usando método base
        return await UpdateAsync(request, sessionData);
    }
    
    /// <summary>
    /// Buscar con filtros en campos personalizados
    /// </summary>
    public virtual async Task<PagedResult<T>> SearchWithCustomFieldsAsync(
        PagedRequest request,
        List<CustomFieldFilter>? customFilters = null,
        SessionDataDto? sessionData = null)
    {
        var query = _dbSet.AsQueryable();
        
        // Aplicar filtros normales
        query = ApplyFilters(query, request.Filters);
        
        // Aplicar filtros de campos personalizados
        if (customFilters?.Any() == true)
        {
            query = ApplyCustomFieldFilters(query, customFilters);
        }
        
        return await ExecutePagedQueryAsync(query, request);
    }
    
    private async Task ValidateCustomFieldsAsync(T entity, SessionDataDto sessionData, bool isCreate)
    {
        var entityName = typeof(T).Name;
        var customProperty = typeof(T).GetProperty("Custom");
        
        if (customProperty == null) return;
        
        var customData = customProperty.GetValue(entity)?.ToString();
        if (string.IsNullOrEmpty(customData)) return;
        
        var customValues = JsonSerializer.Deserialize<Dictionary<string, object>>(customData);
        
        var validationRequest = new CustomFieldValidationRequest
        {
            EntityName = entityName,
            OrganizationId = sessionData.OrganizationId,
            Values = customValues,
            ContextData = GetEntityContextData(entity),
            UserPermissions = sessionData.Permissions
        };
        
        var validationResult = await _customFieldsClient.ValidateAsync(validationRequest);
        
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage);
            throw new ValidationException($"Errores en campos personalizados: {string.Join(", ", errors)}");
        }
    }
    
    private async Task ProcessCustomFieldsAsync(T entity, SessionDataDto sessionData)
    {
        var entityName = typeof(T).Name;
        var customProperty = typeof(T).GetProperty("Custom");
        
        if (customProperty == null) return;
        
        var customData = customProperty.GetValue(entity)?.ToString();
        if (string.IsNullOrEmpty(customData)) return;
        
        // Sanitizar y procesar datos
        var customValues = JsonSerializer.Deserialize<Dictionary<string, object>>(customData);
        var processedValues = await ProcessCustomFieldValues(customValues, entityName, sessionData);
        
        // Actualizar entity con valores procesados
        var processedJson = JsonSerializer.Serialize(processedValues);
        customProperty.SetValue(entity, processedJson);
    }
    
    private async Task<Dictionary<string, object>> ProcessCustomFieldValues(
        Dictionary<string, object> values,
        string entityName,
        SessionDataDto sessionData)
    {
        var definitions = await _customFieldsClient.GetDefinitionsByEntityAsync(entityName, sessionData.OrganizationId);
        var processedValues = new Dictionary<string, object>();
        
        foreach (var kvp in values)
        {
            var definition = definitions?.FirstOrDefault(d => d.FieldName == kvp.Key);
            if (definition == null) continue; // Skip unknown fields
            
            var processedValue = ProcessFieldValue(kvp.Value, definition);
            if (processedValue != null)
            {
                processedValues[kvp.Key] = processedValue;
            }
        }
        
        return processedValues;
    }
    
    private object? ProcessFieldValue(object? value, CustomFieldDefinitionDto definition)
    {
        if (value == null) return null;
        
        switch (definition.FieldType.ToLower())
        {
            case "text":
            case "textarea":
                return SanitizeText(value.ToString());
                
            case "number":
                return ProcessNumber(value, definition);
                
            case "date":
                return ProcessDate(value);
                
            case "boolean":
                return ProcessBoolean(value);
                
            case "select":
                return ValidateSelectValue(value, definition);
                
            case "multiselect":
                return ValidateMultiSelectValue(value, definition);
                
            default:
                return value;
        }
    }
    
    private IQueryable<T> ApplyCustomFieldFilters(IQueryable<T> query, List<CustomFieldFilter> filters)
    {
        foreach (var filter in filters)
        {
            switch (filter.Operator.ToLower())
            {
                case "equals":
                    query = query.Where(e => EF.Functions.JsonValue(
                        EF.Property<string>(e, "Custom"), 
                        $"$.{filter.FieldName}") == filter.Value.ToString());
                    break;
                    
                case "contains":
                    query = query.Where(e => EF.Functions.JsonValue(
                        EF.Property<string>(e, "Custom"), 
                        $"$.{filter.FieldName}").Contains(filter.Value.ToString()));
                    break;
                    
                case "greater_than":
                    if (decimal.TryParse(filter.Value.ToString(), out var numValue))
                    {
                        query = query.Where(e => Convert.ToDecimal(EF.Functions.JsonValue(
                            EF.Property<string>(e, "Custom"), 
                            $"$.{filter.FieldName}")) > numValue);
                    }
                    break;
                    
                case "less_than":
                    if (decimal.TryParse(filter.Value.ToString(), out var numValue2))
                    {
                        query = query.Where(e => Convert.ToDecimal(EF.Functions.JsonValue(
                            EF.Property<string>(e, "Custom"), 
                            $"$.{filter.FieldName}")) < numValue2);
                    }
                    break;
                    
                case "is_not_null":
                    query = query.Where(e => EF.Functions.JsonValue(
                        EF.Property<string>(e, "Custom"), 
                        $"$.{filter.FieldName}") != null);
                    break;
            }
        }
        
        return query;
    }
    
    private Dictionary<string, object> GetEntityContextData(T entity)
    {
        var contextData = new Dictionary<string, object>();
        
        // Obtener propiedades relevantes para condiciones
        var properties = typeof(T).GetProperties()
            .Where(p => p.CanRead && IsSimpleType(p.PropertyType))
            .Take(20); // Limitar para performance
            
        foreach (var prop in properties)
        {
            try
            {
                var value = prop.GetValue(entity);
                if (value != null)
                {
                    contextData[prop.Name] = value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error reading property {PropertyName} for context", prop.Name);
            }
        }
        
        return contextData;
    }
    
    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive || 
               type == typeof(string) || 
               type == typeof(DateTime) || 
               type == typeof(decimal) || 
               type == typeof(Guid) ||
               (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                IsSimpleType(type.GetGenericArguments()[0]));
    }
}

// Custom Field Filter para búsquedas
public class CustomFieldFilter
{
    public string FieldName { get; set; } = null!;
    public string Operator { get; set; } = null!; // "equals", "contains", "greater_than", etc.
    public object Value { get; set; } = null!;
}
```

---

## 🔌 HTTP Client para CustomFields.API

### **ICustomFieldsHttpClient Interface**

```csharp
// En Backend.Utils/HttpClients/ICustomFieldsHttpClient.cs
public interface ICustomFieldsHttpClient
{
    Task<List<CustomFieldDefinitionDto>?> GetDefinitionsByEntityAsync(string entityName, Guid? organizationId = null);
    Task<CustomFieldDefinitionDto?> GetDefinitionAsync(Guid id);
    Task<CustomFieldDefinitionDto> CreateDefinitionAsync(CreateCustomFieldRequest request);
    Task<CustomFieldDefinitionDto> UpdateDefinitionAsync(Guid id, UpdateCustomFieldRequest request);
    Task<bool> DeleteDefinitionAsync(Guid id, bool force = false);
    Task<ValidationResult> ValidateAsync(CustomFieldValidationRequest request);
    Task<ConditionEvaluationResult> EvaluateConditionsAsync(ConditionEvaluationRequest request);
}

// Implementación
public class CustomFieldsHttpClient : ICustomFieldsHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CustomFieldsHttpClient> _logger;
    private readonly IMemoryCache _cache;
    
    public CustomFieldsHttpClient(
        HttpClient httpClient, 
        ILogger<CustomFieldsHttpClient> logger,
        IMemoryCache cache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cache = cache;
    }
    
    public async Task<List<CustomFieldDefinitionDto>?> GetDefinitionsByEntityAsync(
        string entityName, 
        Guid? organizationId = null)
    {
        var cacheKey = $"cf_definitions_{entityName}_{organizationId}";
        
        if (_cache.TryGetValue(cacheKey, out List<CustomFieldDefinitionDto>? cachedResult))
        {
            return cachedResult;
        }
        
        try
        {
            var url = $"api/v1/customfielddefinitions/entity/{entityName}";
            if (organizationId.HasValue)
            {
                url += $"?organizationId={organizationId}";
            }
            
            var response = await _httpClient.GetAsync(url);
            
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new List<CustomFieldDefinitionDto>();
            }
            
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var definitions = JsonSerializer.Deserialize<List<CustomFieldDefinitionDto>>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            // Cache por 15 minutos
            _cache.Set(cacheKey, definitions, TimeSpan.FromMinutes(15));
            
            return definitions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching custom field definitions for {EntityName}", entityName);
            return new List<CustomFieldDefinitionDto>();
        }
    }
    
    public async Task<ValidationResult> ValidateAsync(CustomFieldValidationRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/customfieldvalidation/validate", request);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ValidationResult>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }) ?? new ValidationResult { IsValid = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating custom fields for {EntityName}", request.EntityName);
            
            // En caso de error, permitir que continúe pero loggear
            return new ValidationResult 
            { 
                IsValid = true, // Fail gracefully
                Errors = new List<FieldValidationError>()
            };
        }
    }
    
    public async Task<CustomFieldDefinitionDto> CreateDefinitionAsync(CreateCustomFieldRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/v1/customfielddefinitions", request);
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<CustomFieldDefinitionDto>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        // Invalidar cache
        InvalidateEntityCache(request.EntityName, request.OrganizationId);
        
        return result!;
    }
    
    private void InvalidateEntityCache(string entityName, Guid? organizationId)
    {
        var cacheKey = $"cf_definitions_{entityName}_{organizationId}";
        _cache.Remove(cacheKey);
    }
}
```

### **Registro en DI Container**

```csharp
// En Backend/Program.cs o ServiceCollectionExtensions
public static IServiceCollection AddCustomFieldsIntegration(
    this IServiceCollection services, 
    IConfiguration configuration)
{
    // HTTP Client para CustomFields.API
    services.AddHttpClient<ICustomFieldsHttpClient, CustomFieldsHttpClient>(client =>
    {
        var baseUrl = configuration["CustomFieldsAPI:BaseUrl"] ?? "https://localhost:7002";
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("User-Agent", "AgendaGes.Backend/1.0");
    })
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());
    
    return services;
}

private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Console.WriteLine($"Retry {retryCount} for CustomFields.API after {timespan} seconds");
            });
}

private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (exception, timespan) =>
            {
                Console.WriteLine($"Circuit breaker opened for CustomFields.API for {timespan}");
            },
            onReset: () =>
            {
                Console.WriteLine("Circuit breaker reset for CustomFields.API");
            });
}
```

---

## 🔐 Integración con Sistema de Permisos

### **Extensión del SimpleSaveInterceptor**

```csharp
// En Backend.Utils/EFInterceptors/SimpleSaveInterceptor.cs
public partial class SimpleSaveInterceptor : SaveChangesInterceptor
{
    private readonly ICustomFieldsHttpClient _customFieldsClient;
    
    // Constructor actualizado (agregar parámetro)
    public SimpleSaveInterceptor(
        ILogger<SimpleSaveInterceptor> logger, 
        IServiceProvider serviceProvider,
        ICustomFieldsHttpClient customFieldsClient)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _customFieldsClient = customFieldsClient;
    }
    
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, 
        InterceptionResult<int> result, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔥🔥 [SimpleSaveInterceptor] SavingChangesAsync INTERCEPTADO!!!");
        
        try
        {
            ProcessFieldPermissions(eventData);
            
            // Procesar permisos de campos personalizados
            await ProcessCustomFieldPermissionsAsync(eventData);
            
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-lanzar excepciones de autorización
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validando permisos (incluyendo campos personalizados)");
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
    
    private async Task ProcessCustomFieldPermissionsAsync(DbContextEventData eventData)
    {
        var context = eventData.Context;
        if (context == null) return;
        
        // Verificar si hay una operación Force activa
        if (ForceOperationContext.IsForced)
        {
            var forceInfo = ForceOperationContext.Current;
            _logger.LogWarning($"🚀 FORCE MODE: Saltando validaciones de campos personalizados - Razón: {forceInfo?.Reason ?? "No especificada"}");
            return;
        }
        
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .Where(e => HasCustomField(e.Entity))
            .ToList();
            
        if (!entries.Any()) return;
        
        // Obtener permisos del usuario actual
        var userPermissions = await GetCurrentUserPermissionsAsync();
        if (userPermissions == null)
        {
            userPermissions = new List<string>(); // Sin permisos
        }
        
        foreach (var entry in entries)
        {
            await ValidateCustomFieldPermissionsAsync(entry, userPermissions);
        }
    }
    
    private async Task ValidateCustomFieldPermissionsAsync(EntityEntry entry, List<string> userPermissions)
    {
        var entity = entry.Entity;
        var entityType = entity.GetType();
        var entityName = entityType.Name;
        
        var customProperty = entityType.GetProperty("Custom");
        if (customProperty == null) return;
        
        var customData = customProperty.GetValue(entity)?.ToString();
        if (string.IsNullOrEmpty(customData)) return;
        
        try
        {
            var customValues = JsonSerializer.Deserialize<Dictionary<string, object>>(customData);
            if (customValues?.Any() != true) return;
            
            // Obtener organización
            var organizationId = GetOrganizationId(entity);
            
            // Obtener definiciones de campos personalizados
            var customFieldDefinitions = await _customFieldsClient.GetDefinitionsByEntityAsync(entityName, organizationId);
            if (customFieldDefinitions?.Any() != true) return;
            
            foreach (var fieldDefinition in customFieldDefinitions)
            {
                if (!customValues.ContainsKey(fieldDefinition.FieldName)) continue;
                
                await ValidateFieldPermission(entry, fieldDefinition, customValues, userPermissions);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Error deserializando campo Custom para validación de permisos en {EntityName}", entityName);
            // Continuar - no bloquear por errores de JSON
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validando permisos de campos personalizados para {EntityName}", entityName);
            // En caso de error de comunicación con CustomFields.API, permitir operación pero loggear
        }
    }
    
    private async Task ValidateFieldPermission(
        EntityEntry entry, 
        CustomFieldDefinitionDto fieldDefinition,
        Dictionary<string, object> customValues,
        List<string> userPermissions)
    {
        string? requiredPermission = null;
        
        if (entry.State == EntityState.Added && !string.IsNullOrEmpty(fieldDefinition.Permissions?.Create))
        {
            requiredPermission = fieldDefinition.Permissions.Create;
        }
        else if (entry.State == EntityState.Modified && !string.IsNullOrEmpty(fieldDefinition.Permissions?.Update))
        {
            // Verificar si este campo específico cambió
            var hasFieldChanged = await HasCustomFieldChangedAsync(entry, fieldDefinition.FieldName);
            if (hasFieldChanged)
            {
                requiredPermission = fieldDefinition.Permissions.Update;
            }
        }
        
        // Validar permiso
        if (!string.IsNullOrEmpty(requiredPermission) && 
            !userPermissions.Contains(requiredPermission))
        {
            var action = entry.State == EntityState.Added ? "crear" : "modificar";
            var errorMessage = $"🚨 ACCESO DENEGADO: No tiene permisos para {action} el campo personalizado '{fieldDefinition.DisplayName}'. Permiso requerido: {requiredPermission}";
            
            _logger.LogError(errorMessage);
            throw new UnauthorizedAccessException(errorMessage);
        }
    }
    
    private async Task<bool> HasCustomFieldChangedAsync(EntityEntry entry, string fieldName)
    {
        try
        {
            var originalCustom = entry.Property("Custom").OriginalValue?.ToString();
            var currentCustom = entry.Property("Custom").CurrentValue?.ToString();
            
            var originalValues = string.IsNullOrEmpty(originalCustom) 
                ? new Dictionary<string, object>() 
                : JsonSerializer.Deserialize<Dictionary<string, object>>(originalCustom);
                
            var currentValues = string.IsNullOrEmpty(currentCustom) 
                ? new Dictionary<string, object>() 
                : JsonSerializer.Deserialize<Dictionary<string, object>>(currentCustom);
            
            var originalValue = originalValues?.ContainsKey(fieldName) == true ? originalValues[fieldName] : null;
            var currentValue = currentValues?.ContainsKey(fieldName) == true ? currentValues[fieldName] : null;
            
            return !Equals(originalValue, currentValue);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error comparando valores de campo personalizado {FieldName}", fieldName);
            return true; // En caso de duda, asumir que cambió
        }
    }
    
    private static bool HasCustomField(object entity)
    {
        return entity.GetType().GetProperty("Custom") != null;
    }
    
    private static Guid? GetOrganizationId(object entity)
    {
        var orgProperty = entity.GetType().GetProperty("OrganizationId");
        return orgProperty?.GetValue(entity) as Guid?;
    }
}
```

---

## 🎯 Integración con Sistema de Auditoría

### **Extensión del AuditoriaSaveInterceptor**

```csharp
// En Backend.Utils/EFInterceptors/AuditoriaSaveInterceptor.cs (si existe)
// O crear nuevo interceptor para auditoría de campos personalizados

public class CustomFieldAuditInterceptor : SaveChangesInterceptor
{
    private readonly ILogger<CustomFieldAuditInterceptor> _logger;
    private readonly ICustomFieldsHttpClient _customFieldsClient;
    private readonly IServiceProvider _serviceProvider;
    
    public CustomFieldAuditInterceptor(
        ILogger<CustomFieldAuditInterceptor> logger,
        ICustomFieldsHttpClient customFieldsClient,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _customFieldsClient = customFieldsClient;
        _serviceProvider = serviceProvider;
    }
    
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, 
        InterceptionResult<int> result, 
        CancellationToken cancellationToken = default)
    {
        await ProcessCustomFieldAuditingAsync(eventData);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
    
    private async Task ProcessCustomFieldAuditingAsync(DbContextEventData eventData)
    {
        var context = eventData.Context;
        if (context == null) return;
        
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified && HasCustomField(e.Entity))
            .ToList();
            
        foreach (var entry in entries)
        {
            await ProcessEntityCustomFieldAuditAsync(entry);
        }
    }
    
    private async Task ProcessEntityCustomFieldAuditAsync(EntityEntry entry)
    {
        var entity = entry.Entity;
        var entityType = entity.GetType();
        var entityName = entityType.Name;
        
        var customProperty = entityType.GetProperty("Custom");
        if (customProperty == null || !entry.Property("Custom").IsModified) return;
        
        try
        {
            var originalCustom = entry.Property("Custom").OriginalValue?.ToString();
            var currentCustom = entry.Property("Custom").CurrentValue?.ToString();
            
            // Detectar cambios específicos en campos personalizados
            var changes = DetectCustomFieldChanges(originalCustom, currentCustom);
            if (!changes.Any()) return;
            
            // Obtener definiciones para nombres de display
            var organizationId = GetOrganizationId(entity);
            var definitions = await _customFieldsClient.GetDefinitionsByEntityAsync(entityName, organizationId);
            
            // Crear registros de auditoría
            foreach (var change in changes)
            {
                await CreateCustomFieldAuditEntryAsync(entity, change, definitions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando auditoría de campos personalizados para {EntityName}", entityName);
        }
    }
    
    private List<CustomFieldChange> DetectCustomFieldChanges(string? originalJson, string? currentJson)
    {
        var originalData = string.IsNullOrEmpty(originalJson) 
            ? new Dictionary<string, object>() 
            : JsonSerializer.Deserialize<Dictionary<string, object>>(originalJson) ?? new Dictionary<string, object>();
            
        var currentData = string.IsNullOrEmpty(currentJson) 
            ? new Dictionary<string, object>() 
            : JsonSerializer.Deserialize<Dictionary<string, object>>(currentJson) ?? new Dictionary<string, object>();
            
        var changes = new List<CustomFieldChange>();
        
        // Detectar campos modificados
        foreach (var current in currentData)
        {
            if (!originalData.ContainsKey(current.Key))
            {
                changes.Add(new CustomFieldChange
                {
                    FieldName = current.Key,
                    ChangeType = "CREATE",
                    OldValue = null,
                    NewValue = current.Value
                });
            }
            else if (!Equals(originalData[current.Key], current.Value))
            {
                changes.Add(new CustomFieldChange
                {
                    FieldName = current.Key,
                    ChangeType = "UPDATE",
                    OldValue = originalData[current.Key],
                    NewValue = current.Value
                });
            }
        }
        
        // Detectar campos eliminados
        foreach (var original in originalData)
        {
            if (!currentData.ContainsKey(original.Key))
            {
                changes.Add(new CustomFieldChange
                {
                    FieldName = original.Key,
                    ChangeType = "DELETE",
                    OldValue = original.Value,
                    NewValue = null
                });
            }
        }
        
        return changes;
    }
    
    private async Task CreateCustomFieldAuditEntryAsync(
        object entity, 
        CustomFieldChange change,
        List<CustomFieldDefinitionDto>? definitions)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var currentUserService = scope.ServiceProvider.GetService<ICurrentUserService>();
            
            var entityId = GetEntityId(entity);
            var entityType = entity.GetType();
            var organizationId = GetOrganizationId(entity);
            var currentUserId = await currentUserService?.GetCurrentUserIdAsync();
            
            // Obtener display name del campo
            var fieldDefinition = definitions?.FirstOrDefault(d => d.FieldName == change.FieldName);
            var fieldDisplayName = fieldDefinition?.DisplayName ?? change.FieldName;
            
            // Obtener comentario de Force operation si existe
            var forceComment = ForceOperationContext.Current?.Reason;
            
            var auditEntry = new SystemAuditoria
            {
                Id = Guid.NewGuid(),
                Tabla = $"{entityType.Name}.{change.FieldName}", // Incluir campo en tabla
                RegistroId = entityId,
                Action = change.ChangeType,
                ValorAnterior = change.OldValue != null ? JsonSerializer.Serialize(new { [change.FieldName] = change.OldValue }) : null,
                NuevoValor = change.NewValue != null ? JsonSerializer.Serialize(new { [change.FieldName] = change.NewValue }) : null,
                Comentario = forceComment ?? $"Cambio en campo personalizado: {fieldDisplayName}",
                FechaCreacion = DateTime.UtcNow,
                OrganizationId = organizationId,
                CreadorId = currentUserId,
                Active = true
            };
            
            context.SystemAuditoria.Add(auditEntry);
            await context.SaveChangesAsync();
            
            _logger.LogInformation("Auditoría creada para campo personalizado {FieldName} en {EntityType}", 
                change.FieldName, entityType.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creando registro de auditoría para campo personalizado {FieldName}", 
                change.FieldName);
        }
    }
    
    private static Guid GetEntityId(object entity)
    {
        var idProperty = entity.GetType().GetProperty("Id");
        return idProperty?.GetValue(entity) as Guid? ?? Guid.Empty;
    }
    
    private static Guid? GetOrganizationId(object entity)
    {
        var orgProperty = entity.GetType().GetProperty("OrganizationId");
        return orgProperty?.GetValue(entity) as Guid?;
    }
    
    private static bool HasCustomField(object entity)
    {
        return entity.GetType().GetProperty("Custom") != null;
    }
}

public class CustomFieldChange
{
    public string FieldName { get; set; } = null!;
    public string ChangeType { get; set; } = null!; // "CREATE", "UPDATE", "DELETE"
    public object? OldValue { get; set; }
    public object? NewValue { get; set; }
}
```

---

## 🚀 Integración Frontend

### **Actualización de EmpleadoFormulario.razor**

```razor
<!-- En Frontend/Modules/RRHH/Empleado/Empleados/EmpleadoFormulario.razor -->

@* Agregar nueva tab para campos personalizados *@
<CrmTab Id="tab-custom-fields" Title="Campos Adicionales" Icon="settings" IconColor="#7b68ee">
    <div class="scrollable-content">
        @if (isLoadingCustomFields)
        {
            <div class="loading-container">
                <RadzenProgressBar ProgressBarStyle="ProgressBarStyle.Primary" Value="100" ShowValue="false" />
                <p>Cargando campos personalizados...</p>
            </div>
        }
        else if (customFieldDefinitions?.Any() == true)
        {
            <CustomFieldsSection EntityName="Empleado"
                                CustomFieldDefinitions="@customFieldDefinitions"
                                CustomFieldValues="@customFieldValues"
                                FormData="@GetFormData()"
                                OnValuesChanged="@HandleCustomFieldsChanged"
                                ReadOnly="@(!CanEdit)"
                                UserPermissions="@CurrentUserPermissions" />
        }
        else
        {
            <div class="no-custom-fields">
                <RadzenIcon Icon="settings" />
                <h4>No hay campos personalizados configurados</h4>
                <p>Un administrador puede configurar campos adicionales para empleados.</p>
                
                @if (HasPermission("CUSTOMFIELDS.MANAGE"))
                {
                    <RadzenButton Text="Configurar Campos" 
                                 Icon="add" 
                                 ButtonStyle="ButtonStyle.Primary"
                                 Click="@NavigateToCustomFieldDesigner" />
                }
            </div>
        }
    </div>
</CrmTab>

@code {
    // Variables para campos personalizados
    private List<CustomFieldDefinitionDto>? customFieldDefinitions;
    private Dictionary<string, object>? customFieldValues = new();
    private bool isLoadingCustomFields = false;
    
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        
        // Cargar campos personalizados
        await LoadCustomFieldsAsync();
    }
    
    private async Task LoadCustomFieldsAsync()
    {
        isLoadingCustomFields = true;
        StateHasChanged();
        
        try
        {
            // Obtener definiciones de campos personalizados
            customFieldDefinitions = await CustomFieldsService.GetDefinitionsByEntityAsync("Empleado");
            
            // Si estamos editando, cargar valores existentes
            if (isEditMode && entity?.Custom != null)
            {
                try
                {
                    customFieldValues = JsonSerializer.Deserialize<Dictionary<string, object>>(entity.Custom);
                }
                catch (JsonException ex)
                {
                    Logger.LogWarning(ex, "Error deserializando campos personalizados del empleado {EmpleadoId}", entity.Id);
                    customFieldValues = new Dictionary<string, object>();
                }
            }
            else
            {
                customFieldValues = new Dictionary<string, object>();
                
                // Aplicar valores por defecto
                if (customFieldDefinitions?.Any() == true)
                {
                    foreach (var field in customFieldDefinitions.Where(f => !string.IsNullOrEmpty(f.DefaultValue)))
                    {
                        try
                        {
                            var defaultValue = JsonSerializer.Deserialize<object>(field.DefaultValue);
                            if (defaultValue != null)
                            {
                                customFieldValues[field.FieldName] = defaultValue;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning(ex, "Error aplicando valor por defecto para campo {FieldName}", field.FieldName);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error cargando campos personalizados para empleados");
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = "No se pudieron cargar los campos personalizados",
                Duration = 4000
            });
        }
        finally
        {
            isLoadingCustomFields = false;
            StateHasChanged();
        }
    }
    
    private async Task HandleCustomFieldsChanged(Dictionary<string, object> values)
    {
        customFieldValues = values;
        
        // Serializar y guardar en el campo Custom de la entidad
        entity.Custom = JsonSerializer.Serialize(values);
        
        // Marcar como modificado para validaciones
        StateHasChanged();
        
        // Opcional: Auto-save después de X segundos sin cambios
        // await ScheduleAutoSaveAsync();
    }
    
    private Dictionary<string, object> GetFormData()
    {
        // Crear diccionario con datos del formulario principal para condiciones
        var formData = new Dictionary<string, object>();
        
        if (entity != null)
        {
            // Agregar campos relevantes para condiciones
            if (entity.EstadoId.HasValue) formData["estado_empleado"] = entity.EstadoId.Value.ToString();
            if (entity.CargoId.HasValue) formData["cargo"] = entity.CargoId.Value.ToString();
            if (!string.IsNullOrEmpty(entity.Nombre)) formData["nombre"] = entity.Nombre;
            if (entity.FechaIngreso.HasValue) formData["fecha_ingreso"] = entity.FechaIngreso.Value;
            if (entity.SueldoBase.HasValue) formData["sueldo_base"] = entity.SueldoBase.Value;
            // Agregar más campos según necesidades
        }
        
        return formData;
    }
    
    private void NavigateToCustomFieldDesigner()
    {
        NavigationManager.NavigateTo("/admin/custom-fields/designer?entity=Empleado");
    }
    
    // Override del método SaveForm para incluir validación de campos personalizados
    protected override async Task<bool> SaveForm()
    {
        // Validar campos personalizados antes de guardar
        if (customFieldDefinitions?.Any() == true && customFieldValues?.Any() == true)
        {
            var validationResult = await CustomFieldsService.ValidateAsync(new CustomFieldValidationRequest
            {
                EntityName = "Empleado",
                OrganizationId = SessionData.OrganizationId,
                Values = customFieldValues,
                ContextData = GetFormData(),
                UserPermissions = CurrentUserPermissions
            });
            
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error en Campos Personalizados",
                    Detail = errors,
                    Duration = 6000
                });
                
                return false;
            }
        }
        
        // Proceder con guardado normal
        return await base.SaveForm();
    }
}
```

---

## 📊 Integración con Sistema de Reportes

### **Extensión para Incluir Campos Personalizados en Reportes**

```csharp
// En Backend/Services/ReportService.cs (si existe)
public class ReportService
{
    private readonly ICustomFieldsHttpClient _customFieldsClient;
    
    public async Task<byte[]> GenerateEmployeeReportAsync(
        EmployeeReportRequest request,
        bool includeCustomFields = false)
    {
        var query = BuildEmployeeQuery(request);
        var employees = await query.ToListAsync();
        
        List<CustomFieldDefinitionDto>? customFieldDefinitions = null;
        
        if (includeCustomFields)
        {
            customFieldDefinitions = await _customFieldsClient.GetDefinitionsByEntityAsync(
                "Empleado", 
                request.OrganizationId);
        }
        
        return GenerateExcelReport(employees, customFieldDefinitions);
    }
    
    private byte[] GenerateExcelReport(
        List<Empleado> employees, 
        List<CustomFieldDefinitionDto>? customFields)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Empleados");
        
        var col = 1;
        
        // Columnas estándar
        worksheet.Cell(1, col++).Value = "ID";
        worksheet.Cell(1, col++).Value = "Nombre";
        worksheet.Cell(1, col++).Value = "Apellido";
        worksheet.Cell(1, col++).Value = "Email";
        worksheet.Cell(1, col++).Value = "Fecha Ingreso";
        worksheet.Cell(1, col++).Value = "Sueldo Base";
        
        // Columnas de campos personalizados
        var customFieldColumns = new Dictionary<string, int>();
        if (customFields?.Any() == true)
        {
            foreach (var field in customFields.OrderBy(f => f.SortOrder))
            {
                worksheet.Cell(1, col).Value = field.DisplayName;
                customFieldColumns[field.FieldName] = col;
                col++;
            }
        }
        
        // Datos
        var row = 2;
        foreach (var employee in employees)
        {
            col = 1;
            
            // Datos estándar
            worksheet.Cell(row, col++).Value = employee.Id.ToString();
            worksheet.Cell(row, col++).Value = employee.Nombre ?? "";
            worksheet.Cell(row, col++).Value = employee.Apellido ?? "";
            worksheet.Cell(row, col++).Value = employee.Email ?? "";
            worksheet.Cell(row, col++).Value = employee.FechaIngreso?.ToString("dd/MM/yyyy") ?? "";
            worksheet.Cell(row, col++).Value = employee.SueldoBase?.ToString("N0") ?? "";
            
            // Datos de campos personalizados
            if (!string.IsNullOrEmpty(employee.Custom) && customFieldColumns.Any())
            {
                try
                {
                    var customData = JsonSerializer.Deserialize<Dictionary<string, object>>(employee.Custom);
                    
                    foreach (var kvp in customFieldColumns)
                    {
                        var fieldName = kvp.Key;
                        var columnIndex = kvp.Value;
                        
                        if (customData?.ContainsKey(fieldName) == true)
                        {
                            var value = customData[fieldName]?.ToString() ?? "";
                            worksheet.Cell(row, columnIndex).Value = value;
                        }
                    }
                }
                catch (JsonException ex)
                {
                    // Log error but continue
                    Console.WriteLine($"Error deserializing custom fields for employee {employee.Id}: {ex.Message}");
                }
            }
            
            row++;
        }
        
        // Auto-fit columns
        worksheet.Columns().AdjustToContents();
        
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
```

---

## ✅ Checklist de Integración

### **Backend Integration:**
- [ ] Registrar `ICustomFieldsHttpClient` en DI container
- [ ] Configurar HTTP client con retry policies
- [ ] Extender `BaseQueryService` con métodos de campos personalizados  
- [ ] Actualizar `SimpleSaveInterceptor` para validar permisos
- [ ] Crear/actualizar interceptor de auditoría
- [ ] Configurar cache para definiciones de campos
- [ ] Agregar endpoints para filtros con campos personalizados

### **Frontend Integration:**
- [ ] Crear componente `CustomFieldsSection`
- [ ] Actualizar formularios principales (Empleado, Empresa, etc.)
- [ ] Implementar validación en tiempo real
- [ ] Agregar navegación al diseñador de campos
- [ ] Integrar con sistema de permisos existente
- [ ] Crear componentes para diferentes tipos de campo
- [ ] Implementar preview y debug tools

### **Database Integration:**
- [ ] Ejecutar scripts de creación de tablas
- [ ] Crear índices optimizados
- [ ] Configurar triggers si es necesario
- [ ] Migrar datos existentes si aplica
- [ ] Configurar backup automático

### **Testing Integration:**
- [ ] Tests unitarios para servicios extendidos
- [ ] Tests de integración entre APIs
- [ ] Tests de performance con campos personalizados
- [ ] Tests de seguridad y permisos
- [ ] Tests de UI end-to-end

### **Production Readiness:**
- [ ] Configurar monitoring y alertas
- [ ] Documentar APIs y servicios
- [ ] Configurar logging estructurado
- [ ] Preparar scripts de deployment
- [ ] Crear runbooks para troubleshooting

---

**🎯 Esta integración aprovecha al máximo la infraestructura existente manteniendo la arquitectura limpia y escalable del sistema actual.**