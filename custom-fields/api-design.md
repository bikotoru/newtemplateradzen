# 🚀 API Design - CustomFields.API

## 📋 Arquitectura de la Nueva API Separada

Esta documentación detalla el diseño completo de la nueva **CustomFields.API** que manejará toda la lógica relacionada con campos personalizados de manera independiente a la API principal.

---

## 🏗️ Justificación de API Separada

### **¿Por qué una API separada?**

✅ **Aislamiento de responsabilidades**: Lógica compleja de campos personalizados no contamina la API principal  
✅ **Escalabilidad independiente**: Puede escalar según demanda específica  
✅ **Desarrollo paralelo**: Equipos pueden trabajar independientemente  
✅ **Despliegue independiente**: Updates sin afectar funcionalidad core  
✅ **Testing aislado**: Pruebas más focalizadas y simples  
✅ **Performance especializada**: Optimizaciones específicas para este dominio  

### **Comunicación entre APIs**
- **API Principal** → **CustomFields.API**: HTTP calls para obtener definiciones y validar
- **CustomFields.API** ← **Base de Datos**: Acceso directo a tablas de campos personalizados
- **Sincronización**: Cache distribuido para performance

---

## 🎯 Estructura del Proyecto CustomFields.API

```
CustomFields.API/
├── Controllers/
│   ├── CustomFieldDefinitionsController.cs
│   ├── CustomFieldValidationController.cs
│   ├── CustomFieldTemplatesController.cs
│   └── HealthController.cs
├── Services/
│   ├── Interfaces/
│   │   ├── ICustomFieldDefinitionService.cs
│   │   ├── ICustomFieldValidationService.cs
│   │   ├── ICustomFieldCacheService.cs
│   │   └── ICustomFieldPermissionService.cs
│   └── Implementation/
│       ├── CustomFieldDefinitionService.cs
│       ├── CustomFieldValidationService.cs
│       ├── CustomFieldCacheService.cs
│       └── CustomFieldPermissionService.cs
├── Models/
│   ├── Entities/
│   │   ├── CustomFieldDefinition.cs
│   │   ├── CustomFieldAuditLog.cs
│   │   └── CustomFieldTemplate.cs
│   ├── DTOs/
│   │   ├── CustomFieldDefinitionDto.cs
│   │   ├── CreateCustomFieldRequest.cs
│   │   ├── UpdateCustomFieldRequest.cs
│   │   └── CustomFieldValidationRequest.cs
│   └── Responses/
│       ├── CustomFieldResponse.cs
│       ├── ValidationResult.cs
│       └── ApiResponse.cs
├── Data/
│   ├── CustomFieldsDbContext.cs
│   ├── Configurations/
│   │   ├── CustomFieldDefinitionConfiguration.cs
│   │   └── CustomFieldAuditLogConfiguration.cs
│   └── Migrations/
├── Extensions/
│   ├── ServiceCollectionExtensions.cs
│   ├── ValidationExtensions.cs
│   └── CacheExtensions.cs
├── Middleware/
│   ├── ErrorHandlingMiddleware.cs
│   ├── RequestLoggingMiddleware.cs
│   └── RateLimitingMiddleware.cs
├── Validators/
│   ├── CustomFieldDefinitionValidator.cs
│   ├── CustomFieldConditionValidator.cs
│   └── CustomFieldPermissionValidator.cs
└── Program.cs
```

---

## 🔗 Controllers y Endpoints

### **1. CustomFieldDefinitionsController**

```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class CustomFieldDefinitionsController : ControllerBase
{
    private readonly ICustomFieldDefinitionService _definitionService;
    private readonly ILogger<CustomFieldDefinitionsController> _logger;

    [HttpGet("entity/{entityName}")]
    [ProducesResponseType(typeof(List<CustomFieldDefinitionDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetDefinitionsByEntity(
        string entityName,
        [FromQuery] Guid? organizationId = null,
        [FromQuery] bool includeDisabled = false)
    {
        // GET /api/v1/customfielddefinitions/entity/Empleado?organizationId=xxx
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CustomFieldDefinitionDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetDefinition(Guid id)
    {
        // GET /api/v1/customfielddefinitions/12345678-1234-1234-1234-123456789012
    }

    [HttpPost]
    [ProducesResponseType(typeof(CustomFieldDefinitionDto), 201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [RateLimit(requests: 10, timeWindow: 60)] // Max 10 campos por minuto
    public async Task<IActionResult> CreateDefinition([FromBody] CreateCustomFieldRequest request)
    {
        // POST /api/v1/customfielddefinitions
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CustomFieldDefinitionDto), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateDefinition(Guid id, [FromBody] UpdateCustomFieldRequest request)
    {
        // PUT /api/v1/customfielddefinitions/12345678-1234-1234-1234-123456789012
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)] // Conflict if field has data
    public async Task<IActionResult> DeleteDefinition(Guid id, [FromQuery] bool force = false)
    {
        // DELETE /api/v1/customfielddefinitions/12345678-1234-1234-1234-123456789012?force=true
    }

    [HttpPost("{id:guid}/toggle")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ToggleFieldStatus(Guid id, [FromBody] ToggleFieldRequest request)
    {
        // POST /api/v1/customfielddefinitions/12345678-1234-1234-1234-123456789012/toggle
        // Body: { "enabled": true, "reason": "Campo reactivado por solicitud del usuario" }
    }

    [HttpPost("bulk")]
    [ProducesResponseType(typeof(BulkOperationResult), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public async Task<IActionResult> BulkCreateDefinitions([FromBody] BulkCreateCustomFieldRequest request)
    {
        // POST /api/v1/customfielddefinitions/bulk
        // Para crear múltiples campos de una vez (ej: desde template)
    }

    [HttpGet("entity/{entityName}/export")]
    [ProducesResponseType(typeof(CustomFieldExportDto), 200)]
    public async Task<IActionResult> ExportEntityConfiguration(
        string entityName,
        [FromQuery] Guid? organizationId = null)
    {
        // GET /api/v1/customfielddefinitions/entity/Empleado/export
        // Exportar configuración para backup o migración
    }

    [HttpPost("entity/{entityName}/import")]
    [ProducesResponseType(typeof(ImportResult), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public async Task<IActionResult> ImportEntityConfiguration(
        string entityName,
        [FromBody] CustomFieldImportDto importData)
    {
        // POST /api/v1/customfielddefinitions/entity/Empleado/import
        // Importar configuración desde backup o migración
    }
}
```

### **2. CustomFieldValidationController**

```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class CustomFieldValidationController : ControllerBase
{
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ValidationResult), 200)]
    public async Task<IActionResult> ValidateCustomFieldValues([FromBody] CustomFieldValidationRequest request)
    {
        // POST /api/v1/customfieldvalidation/validate
        // Validar valores de campos personalizados antes de guardar
    }

    [HttpPost("validate-conditions")]
    [ProducesResponseType(typeof(ConditionEvaluationResult), 200)]
    public async Task<IActionResult> EvaluateConditions([FromBody] ConditionEvaluationRequest request)
    {
        // POST /api/v1/customfieldvalidation/validate-conditions
        // Evaluar condiciones (show_if, required_if, etc.)
    }

    [HttpPost("validate-permissions")]
    [ProducesResponseType(typeof(PermissionValidationResult), 200)]
    public async Task<IActionResult> ValidatePermissions([FromBody] PermissionValidationRequest request)
    {
        // POST /api/v1/customfieldvalidation/validate-permissions
        // Validar permisos de usuario para campos específicos
    }

    [HttpGet("rules/{entityName}")]
    [ProducesResponseType(typeof(ValidationRulesDto), 200)]
    public async Task<IActionResult> GetValidationRules(
        string entityName,
        [FromQuery] Guid? organizationId = null)
    {
        // GET /api/v1/customfieldvalidation/rules/Empleado
        // Obtener todas las reglas de validación para una entidad
    }
}
```

### **3. CustomFieldTemplatesController (Fase Futura)**

```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class CustomFieldTemplatesController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<CustomFieldTemplateDto>), 200)]
    public async Task<IActionResult> GetTemplates(
        [FromQuery] string? category = null,
        [FromQuery] string? targetEntity = null)
    {
        // GET /api/v1/customfieldtemplates?category=RRHH&targetEntity=Empleado
    }

    [HttpPost("{templateId:guid}/apply")]
    [ProducesResponseType(typeof(ApplyTemplateResult), 200)]
    public async Task<IActionResult> ApplyTemplate(
        Guid templateId,
        [FromBody] ApplyTemplateRequest request)
    {
        // POST /api/v1/customfieldtemplates/12345678-1234-1234-1234-123456789012/apply
        // Aplicar template a una entidad/organización
    }
}
```

---

## 📄 DTOs y Models

### **Request/Response Models**

```csharp
// Request para crear campo personalizado
public class CreateCustomFieldRequest
{
    public string EntityName { get; set; } = null!;
    public string FieldName { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? Description { get; set; }
    public string FieldType { get; set; } = null!;
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public int SortOrder { get; set; }
    public Dictionary<string, object>? ValidationConfig { get; set; }
    public Dictionary<string, object>? UIConfig { get; set; }
    public List<CustomFieldConditionDto>? Conditions { get; set; }
    public CustomFieldPermissionsDto? Permissions { get; set; }
    public List<string>? Tags { get; set; }
}

// Response con definición completa
public class CustomFieldDefinitionDto
{
    public Guid Id { get; set; }
    public string EntityName { get; set; } = null!;
    public string FieldName { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? Description { get; set; }
    public string FieldType { get; set; } = null!;
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; }
    public int Version { get; set; }
    
    // Configuraciones como objetos tipados
    public CustomFieldValidationConfigDto? ValidationConfig { get; set; }
    public CustomFieldUIConfigDto? UIConfig { get; set; }
    public List<CustomFieldConditionDto>? Conditions { get; set; }
    public CustomFieldPermissionsDto? Permissions { get; set; }
    
    // Metadatos
    public List<string>? Tags { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaModificacion { get; set; }
    public Guid? OrganizationId { get; set; }
    public string? CreatedByName { get; set; }
    public string? ModifiedByName { get; set; }
}

// Configuración de validaciones tipada
public class CustomFieldValidationConfigDto
{
    public bool Required { get; set; }
    public string? RequiredMessage { get; set; }
    
    // Text/TextArea
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public string? Pattern { get; set; }
    public string? PatternMessage { get; set; }
    
    // Number
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public decimal? Step { get; set; }
    
    // Date
    public DateTime? MinDate { get; set; }
    public DateTime? MaxDate { get; set; }
    
    // Select
    public bool AllowEmpty { get; set; } = true;
    public int? MinSelections { get; set; }
    public int? MaxSelections { get; set; }
    
    // Custom rules
    public List<CustomValidationRuleDto>? CustomRules { get; set; }
}

// Configuración UI tipada
public class CustomFieldUIConfigDto
{
    public string? Placeholder { get; set; }
    public string? HelpText { get; set; }
    
    // TextArea
    public int? Rows { get; set; }
    
    // Number
    public string? Prefix { get; set; }
    public string? Suffix { get; set; }
    
    // Boolean
    public string? Style { get; set; } // "checkbox", "switch", "radio"
    public string? TrueLabel { get; set; }
    public string? FalseLabel { get; set; }
    
    // Select/MultiSelect
    public List<SelectOptionDto>? Options { get; set; }
    public bool ShowSelectAll { get; set; }
    
    // Date
    public string? Format { get; set; }
    public bool ShowCalendar { get; set; } = true;
}

// Condición para campos dinámicos
public class CustomFieldConditionDto
{
    public string Type { get; set; } = null!; // "show_if", "required_if", "readonly_if"
    public string SourceField { get; set; } = null!;
    public string Operator { get; set; } = null!;
    public object? Value { get; set; }
    public List<object>? Values { get; set; } // Para operadores IN/NOT_IN
}

// Permisos del campo
public class CustomFieldPermissionsDto
{
    public string? Create { get; set; }
    public string? Update { get; set; }
    public string? View { get; set; }
    public bool AutoGeneratePermissions { get; set; } = true;
}

// Request de validación
public class CustomFieldValidationRequest
{
    public string EntityName { get; set; } = null!;
    public Guid? OrganizationId { get; set; }
    public Dictionary<string, object> Values { get; set; } = new();
    public Dictionary<string, object>? ContextData { get; set; } // Datos de la entidad principal para condiciones
    public List<string>? UserPermissions { get; set; }
}

// Resultado de validación
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<FieldValidationError> Errors { get; set; } = new();
    public Dictionary<string, FieldState> FieldStates { get; set; } = new(); // visible, required, readonly
}

public class FieldValidationError
{
    public string FieldName { get; set; } = null!;
    public string ErrorCode { get; set; } = null!;
    public string ErrorMessage { get; set; } = null!;
    public object? AttemptedValue { get; set; }
}

public class FieldState
{
    public bool Visible { get; set; } = true;
    public bool Required { get; set; }
    public bool ReadOnly { get; set; }
    public string? VisibilityReason { get; set; }
    public string? RequiredReason { get; set; }
    public string? ReadOnlyReason { get; set; }
}
```

---

## 🔧 Services Implementation

### **CustomFieldDefinitionService**

```csharp
public interface ICustomFieldDefinitionService
{
    Task<List<CustomFieldDefinitionDto>> GetDefinitionsByEntityAsync(string entityName, Guid? organizationId = null, bool includeDisabled = false);
    Task<CustomFieldDefinitionDto?> GetDefinitionAsync(Guid id);
    Task<CustomFieldDefinitionDto> CreateDefinitionAsync(CreateCustomFieldRequest request, Guid userId);
    Task<CustomFieldDefinitionDto> UpdateDefinitionAsync(Guid id, UpdateCustomFieldRequest request, Guid userId);
    Task<bool> DeleteDefinitionAsync(Guid id, bool force = false);
    Task<bool> ToggleDefinitionStatusAsync(Guid id, bool enabled, string? reason = null);
    Task<BulkOperationResult> BulkCreateDefinitionsAsync(List<CreateCustomFieldRequest> requests, Guid userId);
    Task<CustomFieldExportDto> ExportEntityConfigurationAsync(string entityName, Guid? organizationId = null);
    Task<ImportResult> ImportEntityConfigurationAsync(string entityName, CustomFieldImportDto importData, Guid userId);
}

public class CustomFieldDefinitionService : ICustomFieldDefinitionService
{
    private readonly CustomFieldsDbContext _context;
    private readonly ICustomFieldCacheService _cacheService;
    private readonly ICustomFieldPermissionService _permissionService;
    private readonly ILogger<CustomFieldDefinitionService> _logger;
    
    public async Task<List<CustomFieldDefinitionDto>> GetDefinitionsByEntityAsync(
        string entityName, 
        Guid? organizationId = null, 
        bool includeDisabled = false)
    {
        // Intentar obtener del cache primero
        var cacheKey = $"custom_fields_{entityName}_{organizationId}_{includeDisabled}";
        var cachedResult = await _cacheService.GetAsync<List<CustomFieldDefinitionDto>>(cacheKey);
        
        if (cachedResult != null)
        {
            _logger.LogDebug("Returning cached custom field definitions for {EntityName}", entityName);
            return cachedResult;
        }
        
        // Consultar base de datos
        var query = _context.CustomFieldDefinitions
            .AsNoTracking()
            .Where(cf => cf.EntityName == entityName && cf.Active);
            
        if (organizationId.HasValue)
        {
            query = query.Where(cf => cf.OrganizationId == organizationId.Value);
        }
        
        if (!includeDisabled)
        {
            query = query.Where(cf => cf.IsEnabled);
        }
        
        var definitions = await query
            .OrderBy(cf => cf.SortOrder)
            .ThenBy(cf => cf.DisplayName)
            .Include(cf => cf.Creator)
            .Include(cf => cf.Modifier)
            .ToListAsync();
        
        var result = definitions.Select(MapToDto).ToList();
        
        // Guardar en cache
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(30));
        
        return result;
    }
    
    public async Task<CustomFieldDefinitionDto> CreateDefinitionAsync(CreateCustomFieldRequest request, Guid userId)
    {
        // Validaciones
        await ValidateCreateRequest(request);
        
        // Verificar que no existe un campo con el mismo nombre
        var existingField = await _context.CustomFieldDefinitions
            .FirstOrDefaultAsync(cf => 
                cf.EntityName == request.EntityName && 
                cf.FieldName == request.FieldName && 
                cf.OrganizationId == request.OrganizationId);
                
        if (existingField != null)
        {
            throw new BusinessException($"Ya existe un campo con el nombre '{request.FieldName}' para la entidad '{request.EntityName}'");
        }
        
        // Generar permisos automáticamente si se requiere
        var permissions = request.Permissions;
        if (permissions?.AutoGeneratePermissions == true)
        {
            permissions = await _permissionService.GeneratePermissionsAsync(request.EntityName, request.FieldName);
        }
        
        // Crear entidad
        var definition = new CustomFieldDefinition
        {
            Id = Guid.NewGuid(),
            EntityName = request.EntityName,
            FieldName = request.FieldName,
            DisplayName = request.DisplayName,
            Description = request.Description,
            FieldType = request.FieldType,
            IsRequired = request.IsRequired,
            DefaultValue = request.DefaultValue,
            SortOrder = request.SortOrder,
            ValidationConfig = JsonSerializer.Serialize(request.ValidationConfig ?? new Dictionary<string, object>()),
            UIConfig = JsonSerializer.Serialize(request.UIConfig ?? new Dictionary<string, object>()),
            ConditionsConfig = request.Conditions?.Any() == true ? JsonSerializer.Serialize(request.Conditions) : null,
            PermissionCreate = permissions?.Create,
            PermissionUpdate = permissions?.Update,
            PermissionView = permissions?.View,
            Tags = request.Tags?.Any() == true ? string.Join(",", request.Tags) : null,
            IsEnabled = true,
            Version = 1,
            FechaCreacion = DateTime.UtcNow,
            FechaModificacion = DateTime.UtcNow,
            CreadorId = userId,
            ModificadorId = userId,
            Active = true
        };
        
        _context.CustomFieldDefinitions.Add(definition);
        
        // Crear permisos en sistema si es necesario
        if (permissions != null && permissions.AutoGeneratePermissions)
        {
            await _permissionService.CreatePermissionsAsync(definition);
        }
        
        await _context.SaveChangesAsync();
        
        // Invalidar cache
        await _cacheService.InvalidateEntityCacheAsync(request.EntityName, request.OrganizationId);
        
        _logger.LogInformation("Created custom field definition {FieldName} for entity {EntityName}", 
            request.FieldName, request.EntityName);
        
        return MapToDto(definition);
    }
    
    private async Task ValidateCreateRequest(CreateCustomFieldRequest request)
    {
        var validator = new CustomFieldDefinitionValidator();
        var validationResult = await validator.ValidateAsync(request);
        
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }
    }
    
    private CustomFieldDefinitionDto MapToDto(CustomFieldDefinition entity)
    {
        return new CustomFieldDefinitionDto
        {
            Id = entity.Id,
            EntityName = entity.EntityName,
            FieldName = entity.FieldName,
            DisplayName = entity.DisplayName,
            Description = entity.Description,
            FieldType = entity.FieldType,
            IsRequired = entity.IsRequired,
            DefaultValue = entity.DefaultValue,
            SortOrder = entity.SortOrder,
            IsEnabled = entity.IsEnabled,
            Version = entity.Version,
            ValidationConfig = !string.IsNullOrEmpty(entity.ValidationConfig) 
                ? JsonSerializer.Deserialize<CustomFieldValidationConfigDto>(entity.ValidationConfig) 
                : null,
            UIConfig = !string.IsNullOrEmpty(entity.UIConfig) 
                ? JsonSerializer.Deserialize<CustomFieldUIConfigDto>(entity.UIConfig) 
                : null,
            Conditions = !string.IsNullOrEmpty(entity.ConditionsConfig) 
                ? JsonSerializer.Deserialize<List<CustomFieldConditionDto>>(entity.ConditionsConfig) 
                : null,
            Permissions = new CustomFieldPermissionsDto
            {
                Create = entity.PermissionCreate,
                Update = entity.PermissionUpdate,
                View = entity.PermissionView
            },
            Tags = !string.IsNullOrEmpty(entity.Tags) ? entity.Tags.Split(',').ToList() : null,
            FechaCreacion = entity.FechaCreacion,
            FechaModificacion = entity.FechaModificacion,
            OrganizationId = entity.OrganizationId,
            CreatedByName = entity.Creator?.Nombre,
            ModifiedByName = entity.Modifier?.Nombre
        };
    }
}
```

---

## 🔐 Security y Rate Limiting

### **Configuración de Seguridad**

```csharp
// Program.cs - Configuración de seguridad
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // JWT Authentication
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
                };
            });
        
        // Rate Limiting
        builder.Services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("CustomFieldsAPI", policy =>
            {
                policy.PermitLimit = 100;
                policy.Window = TimeSpan.FromMinutes(1);
                policy.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                policy.QueueLimit = 10;
            });
            
            options.AddFixedWindowLimiter("CustomFieldsCreation", policy =>
            {
                policy.PermitLimit = 10;
                policy.Window = TimeSpan.FromMinutes(1);
            });
        });
        
        // CORS para API principal
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("MainAPI", policy =>
            {
                policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>())
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });
        
        var app = builder.Build();
        
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseCors("MainAPI");
        
        app.MapControllers().RequireRateLimiting("CustomFieldsAPI");
    }
}

// Rate Limiting Attribute personalizado
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RateLimitAttribute : Attribute
{
    public int Requests { get; set; }
    public int TimeWindow { get; set; } // en segundos
    
    public RateLimitAttribute(int requests, int timeWindow)
    {
        Requests = requests;
        TimeWindow = timeWindow;
    }
}
```

---

## 📊 Logging y Monitoring

### **Structured Logging**

```csharp
public class CustomFieldsLoggingExtensions
{
    public static class LoggerExtensions
    {
        private static readonly Action<ILogger, string, string, Exception?> _customFieldCreated =
            LoggerMessage.Define<string, string>(
                LogLevel.Information,
                new EventId(1001, "CustomFieldCreated"),
                "Custom field '{FieldName}' created for entity '{EntityName}'");
                
        private static readonly Action<ILogger, string, string, Exception?> _customFieldValidationFailed =
            LoggerMessage.Define<string, string>(
                LogLevel.Warning,
                new EventId(1002, "CustomFieldValidationFailed"),
                "Validation failed for custom field '{FieldName}' in entity '{EntityName}'");
                
        private static readonly Action<ILogger, string, TimeSpan, Exception?> _customFieldQueryPerformance =
            LoggerMessage.Define<string, TimeSpan>(
                LogLevel.Information,
                new EventId(1003, "CustomFieldQueryPerformance"),
                "Custom field query for entity '{EntityName}' completed in {Duration}");
        
        public static void CustomFieldCreated(this ILogger logger, string fieldName, string entityName)
            => _customFieldCreated(logger, fieldName, entityName, null);
            
        public static void CustomFieldValidationFailed(this ILogger logger, string fieldName, string entityName)
            => _customFieldValidationFailed(logger, fieldName, entityName, null);
            
        public static void CustomFieldQueryPerformance(this ILogger logger, string entityName, TimeSpan duration)
            => _customFieldQueryPerformance(logger, entityName, duration, null);
    }
}
```

### **Health Checks**

```csharp
// HealthController.cs
[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly CustomFieldsDbContext _context;
    private readonly ICustomFieldCacheService _cacheService;
    
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var health = new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Database = await CheckDatabaseHealth(),
            Cache = await CheckCacheHealth(),
            Version = GetType().Assembly.GetName().Version?.ToString()
        };
        
        return Ok(health);
    }
    
    [HttpGet("detailed")]
    public async Task<IActionResult> GetDetailed()
    {
        var health = new
        {
            Database = await GetDatabaseStats(),
            Cache = await GetCacheStats(),
            Performance = await GetPerformanceMetrics()
        };
        
        return Ok(health);
    }
    
    private async Task<object> GetDatabaseStats()
    {
        return new
        {
            TotalDefinitions = await _context.CustomFieldDefinitions.CountAsync(),
            ActiveDefinitions = await _context.CustomFieldDefinitions.CountAsync(cf => cf.Active && cf.IsEnabled),
            EntitiesWithFields = await _context.CustomFieldDefinitions
                .Where(cf => cf.Active && cf.IsEnabled)
                .Select(cf => cf.EntityName)
                .Distinct()
                .CountAsync(),
            OrganizationsWithFields = await _context.CustomFieldDefinitions
                .Where(cf => cf.Active && cf.IsEnabled)
                .Select(cf => cf.OrganizationId)
                .Distinct()
                .CountAsync()
        };
    }
}
```

---

## 🔧 Configuración y Deployment

### **appsettings.json**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=AgendaGes_CustomFields;Trusted_Connection=true;TrustServerCertificate=true;"
  },
  "Jwt": {
    "Key": "your-super-secret-key-here",
    "Issuer": "AgendaGes.CustomFields.API",
    "Audience": "AgendaGes.Frontend",
    "ExpireMinutes": 60
  },
  "Cache": {
    "ConnectionString": "localhost:6379",
    "DefaultExpiration": "00:30:00",
    "KeyPrefix": "customfields:"
  },
  "RateLimit": {
    "GeneralLimit": 100,
    "CreationLimit": 10,
    "WindowMinutes": 1
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "CustomFields": "Information"
    },
    "ApplicationInsights": {
      "LogLevel": {
        "Default": "Information"
      }
    }
  },
  "AllowedOrigins": [
    "https://localhost:7001",
    "https://agendages.app"
  ],
  "MainAPI": {
    "BaseUrl": "https://localhost:7000/api",
    "Timeout": "00:00:30"
  }
}
```

### **Dockerfile**

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CustomFields.API/CustomFields.API.csproj", "CustomFields.API/"]
COPY ["Shared.Models/Shared.Models.csproj", "Shared.Models/"]
RUN dotnet restore "CustomFields.API/CustomFields.API.csproj"
COPY . .
WORKDIR "/src/CustomFields.API"
RUN dotnet build "CustomFields.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CustomFields.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CustomFields.API.dll"]
```

---

## ✅ Testing Strategy

### **Test Structure**
```
CustomFields.API.Tests/
├── Unit/
│   ├── Services/
│   ├── Controllers/
│   └── Validators/
├── Integration/
│   ├── Controllers/
│   └── Database/
├── Performance/
│   └── LoadTests/
└── Fixtures/
    ├── TestData.cs
    └── DatabaseFixture.cs
```

### **Métricas de Performance**
- **Endpoint Response Time**: < 100ms para GETs, < 200ms para POSTs
- **Database Query Time**: < 50ms para consultas simples
- **Cache Hit Rate**: > 90% para definiciones
- **API Throughput**: > 1000 requests/minute
- **Error Rate**: < 0.1%

---

**🎯 Esta API separada proporciona una arquitectura limpia, escalable y mantenible para gestionar campos personalizados sin contaminar la lógica principal del sistema.**