# Guía de Mejores Prácticas - Sistema de Campos Personalizados

## 🎯 Introducción

Esta guía proporciona las mejores prácticas para desarrolladores que integran y mantienen el sistema de campos personalizados en sus formularios y componentes.

## 📋 Integración Básica

### 1. Agregar al Formulario

```html
@using Frontend.Modules.Admin.CustomFields

<!-- En cualquier formulario existente -->
<CustomFieldsSection EntityName="Empleado"
                   SectionTitle="📋 Información Adicional"
                   OrganizationId="@currentOrganizationId"
                   UserId="@currentUserId"
                   @bind-FieldValues="customFieldValues"
                   ReadOnly="false" />
```

### 2. Configurar en el Code-Behind

```csharp
@code {
    private Dictionary<string, object?> customFieldValues = new();
    private Guid? currentOrganizationId = Guid.NewGuid();
    private Guid? currentUserId = Guid.NewGuid();

    protected override async Task OnInitializedAsync()
    {
        // Cargar valores existentes si es una edición
        if (editingEntity != null)
        {
            customFieldValues = await LoadCustomFieldValues(editingEntity.Id);
        }
    }
}
```

## ⚡ Optimizaciones de Rendimiento

### 1. Lazy Loading de Campos

```csharp
// Solo cargar campos cuando sean necesarios
private async Task<bool> HasCustomFields(string entityName)
{
    var response = await Http.GetAsync($"api/customfielddefinitions/{entityName}/count");
    return await response.Content.ReadFromJsonAsync<int>() > 0;
}
```

### 2. Cache de Definiciones

```csharp
// Implementar cache para evitar múltiples llamadas API
private static readonly MemoryCache _fieldDefinitionsCache = new(new MemoryCacheOptions
{
    SizeLimit = 100,
    CompactionPercentage = 0.25
});

private async Task<List<CustomFieldDefinitionDto>> GetCachedFieldDefinitions(string entityName)
{
    var cacheKey = $"fields_{entityName}_{OrganizationId}";

    if (_fieldDefinitionsCache.TryGetValue(cacheKey, out List<CustomFieldDefinitionDto>? cached))
    {
        return cached!;
    }

    var fields = await LoadCustomFields();
    _fieldDefinitionsCache.Set(cacheKey, fields, TimeSpan.FromMinutes(5));
    return fields;
}
```

### 3. Debounce en Validaciones

```csharp
private Timer? _validationTimer;

private async Task OnFieldValueChanged(string fieldName, object? newValue)
{
    FieldValues[fieldName] = newValue;

    // Debounce validaciones para evitar múltiples llamadas
    _validationTimer?.Dispose();
    _validationTimer = new Timer(async _ =>
    {
        await InvokeAsync(async () =>
        {
            await EvaluateConditions();
            await FieldValuesChanged.InvokeAsync(FieldValues);
        });
    }, null, 500, Timeout.Infinite);
}
```

## 🔒 Seguridad y Permisos

### 1. Validación de Permisos

```csharp
private async Task<bool> CanUserAccessField(CustomFieldDefinitionDto field, string action)
{
    var permission = $"{field.EntityName}.{field.FieldName}.{action.ToUpper()}";
    return await PermissionService.HasPermissionAsync(CurrentUserId, permission);
}

private async Task<List<CustomFieldDefinitionDto>> FilterFieldsByPermissions(
    List<CustomFieldDefinitionDto> fields)
{
    var filteredFields = new List<CustomFieldDefinitionDto>();

    foreach (var field in fields)
    {
        if (await CanUserAccessField(field, "VIEW"))
        {
            filteredFields.Add(field);
        }
    }

    return filteredFields;
}
```

### 2. Sanitización de Valores

```csharp
private object? SanitizeFieldValue(CustomFieldDefinitionDto field, object? value)
{
    if (value == null) return null;

    return field.FieldType switch
    {
        "text" => HtmlEncoder.Default.Encode(value.ToString() ?? ""),
        "textarea" => HtmlEncoder.Default.Encode(value.ToString() ?? ""),
        "number" => ValidateNumericValue(value, field.ValidationConfig),
        "date" => ValidateDateValue(value),
        "boolean" => bool.TryParse(value.ToString(), out var boolVal) ? boolVal : false,
        "select" => ValidateSelectValue(value, field.UIConfig?.Options),
        "multiselect" => ValidateMultiSelectValue(value, field.UIConfig?.Options),
        _ => value
    };
}
```

## 📊 Validaciones Avanzadas

### 1. Validaciones Personalizadas

```csharp
public class CustomFieldValidator
{
    public async Task<ValidationResult> ValidateField(
        CustomFieldDefinitionDto field,
        object? value,
        Dictionary<string, object?> contextValues)
    {
        var errors = new List<string>();

        // Validación de requerido
        if (field.IsRequired && IsValueEmpty(value))
        {
            errors.Add($"El campo '{field.DisplayName}' es requerido");
        }

        // Validaciones específicas por tipo
        switch (field.FieldType)
        {
            case "text":
                await ValidateTextField(field, value?.ToString(), errors);
                break;
            case "number":
                await ValidateNumericField(field, value, errors);
                break;
            case "date":
                await ValidateDateField(field, value, errors);
                break;
        }

        // Validaciones condicionales
        if (field.ConditionalValidations?.Any() == true)
        {
            await ValidateConditionalRules(field, value, contextValues, errors);
        }

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }
}
```

### 2. Validación en Cliente y Servidor

```csharp
// Cliente: Validación inmediata
private async Task<bool> ValidateFieldClient(string fieldName, object? value)
{
    var field = customFields?.FirstOrDefault(f => f.FieldName == fieldName);
    if (field == null) return true;

    var validator = new ClientFieldValidator();
    var result = await validator.ValidateAsync(field, value);

    if (!result.IsValid)
    {
        ShowFieldErrors(fieldName, result.Errors);
        return false;
    }

    return true;
}

// Servidor: Validación final antes de guardar
private async Task<bool> ValidateAllFieldsServer()
{
    var request = new CustomFieldValidationRequest
    {
        EntityName = EntityName,
        OrganizationId = OrganizationId,
        Values = FieldValues.ToDictionary(kv => kv.Key, kv => kv.Value ?? "")
    };

    var response = await Http.PostAsJsonAsync("api/customfields/validate", request);
    if (!response.IsSuccessStatusCode)
    {
        var errors = await response.Content.ReadFromJsonAsync<ValidationErrors>();
        ShowValidationErrors(errors);
        return false;
    }

    return true;
}
```

## 🎨 Personalización de UI

### 1. Temas y Estilos

```html
<!-- Personalizar apariencia por entidad -->
<CustomFieldsSection EntityName="Empleado"
                   SectionTitle="📋 Información del Empleado"
                   CssClass="employee-custom-fields"
                   Theme="primary" />

<style>
    .employee-custom-fields .rz-fieldset {
        border: 2px solid var(--rz-primary);
        border-radius: 12px;
    }

    .employee-custom-fields .rz-formfield {
        margin-bottom: 1.5rem;
    }
</style>
```

### 2. Campos Personalizados por Tipo

```csharp
private RenderFragment RenderCustomFieldType(CustomFieldDefinitionDto field)
{
    return field.FieldType switch
    {
        "rich-text" => RenderRichTextEditor(field),
        "file-upload" => RenderFileUpload(field),
        "signature" => RenderSignaturePad(field),
        "rating" => RenderRatingComponent(field),
        _ => RenderStandardField(field)
    };
}

private RenderFragment RenderRichTextEditor(CustomFieldDefinitionDto field)
{
    return builder =>
    {
        builder.OpenComponent<RadzenHtmlEditor>(0);
        builder.AddAttribute(1, "Value", GetFieldValue(field.FieldName)?.ToString());
        builder.AddAttribute(2, "ValueChanged", EventCallback.Factory.Create<string>(
            this, async value => await OnFieldValueChanged(field.FieldName, value)));
        builder.CloseComponent();
    };
}
```

## 🔄 Ciclo de Vida de Campos

### 1. Estados de Campo

```csharp
public enum CustomFieldState
{
    Draft,      // En diseño
    Active,     // Activo y visible
    Deprecated, // Obsoleto pero con datos
    Archived    // Archivado
}

private async Task TransitionFieldState(Guid fieldId, CustomFieldState newState)
{
    var request = new UpdateFieldStateRequest
    {
        FieldId = fieldId,
        NewState = newState,
        Reason = "State transition",
        UserId = CurrentUserId
    };

    await Http.PutAsJsonAsync($"api/customfields/{fieldId}/state", request);
    await LoadFields(); // Recargar lista
}
```

### 2. Migración de Datos

```csharp
public class FieldMigrationService
{
    public async Task<bool> CanSafelyChangeFieldType(Guid fieldId, string newFieldType)
    {
        var response = await Http.GetAsync($"api/customfields/{fieldId}/migration/check?newType={newFieldType}");
        var result = await response.Content.ReadFromJsonAsync<MigrationAnalysis>();
        return result.IsSafe;
    }

    public async Task MigrateFieldData(Guid fieldId, string newFieldType, MigrationStrategy strategy)
    {
        var request = new FieldMigrationRequest
        {
            FieldId = fieldId,
            NewFieldType = newFieldType,
            Strategy = strategy,
            BackupData = true
        };

        await Http.PostAsJsonAsync($"api/customfields/{fieldId}/migrate", request);
    }
}
```

## 📈 Monitoreo y Analytics

### 1. Métricas de Uso

```csharp
private async Task TrackFieldUsage(string fieldName, string action)
{
    var metric = new FieldUsageMetric
    {
        FieldName = fieldName,
        EntityName = EntityName,
        Action = action,
        UserId = CurrentUserId,
        Timestamp = DateTime.UtcNow,
        OrganizationId = OrganizationId
    };

    // Fire and forget
    _ = Task.Run(async () =>
    {
        try
        {
            await Http.PostAsJsonAsync("api/analytics/field-usage", metric);
        }
        catch
        {
            // Log silently, no interruption to user experience
        }
    });
}
```

### 2. Análisis de Performance

```csharp
public class FieldPerformanceMonitor
{
    private readonly ILogger<FieldPerformanceMonitor> _logger;

    public async Task<PerformanceReport> AnalyzeFieldPerformance(string entityName)
    {
        using var activity = Activity.StartActivity("FieldPerformanceAnalysis");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var fields = await LoadCustomFields();
            var loadTime = stopwatch.ElapsedMilliseconds;

            stopwatch.Restart();
            await EvaluateConditions();
            var evaluationTime = stopwatch.ElapsedMilliseconds;

            return new PerformanceReport
            {
                FieldCount = fields.Count,
                LoadTimeMs = loadTime,
                EvaluationTimeMs = evaluationTime,
                TotalTimeMs = loadTime + evaluationTime,
                Recommendations = GeneratePerformanceRecommendations(fields.Count, loadTime, evaluationTime)
            };
        }
        finally
        {
            stopwatch.Stop();
        }
    }
}
```

## 🚀 Tips de Implementación

### 1. Lazy Loading Inteligente
- Cargar campos solo cuando el usuario interactúa con la sección
- Usar intersection observer para campos fuera del viewport

### 2. Batch Operations
- Agrupar múltiples cambios en una sola llamada API
- Implementar debouncing para validaciones

### 3. Error Handling Robusto
```csharp
private async Task HandleFieldError(string fieldName, Exception ex)
{
    _logger.LogError(ex, "Error in custom field {FieldName}", fieldName);

    // Mostrar error amigable al usuario
    NotificationService.Notify(new NotificationMessage
    {
        Severity = NotificationSeverity.Warning,
        Summary = "Campo Temporal No Disponible",
        Detail = $"El campo '{fieldName}' no está disponible temporalmente. Los datos se guardarán cuando se restablezca la conexión.",
        Duration = 5000
    });

    // Marcar campo como offline
    await MarkFieldAsOffline(fieldName);
}
```

### 4. Testing Strategies
```csharp
[Test]
public async Task CustomFieldsSection_Should_Handle_Network_Errors()
{
    // Arrange
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.When("*/api/customfielddefinitions/*")
           .Respond(HttpStatusCode.ServiceUnavailable);

    // Act
    var component = RenderComponent<CustomFieldsSection>(parameters => parameters
        .Add(p => p.EntityName, "Empleado"));

    // Assert
    Assert.Contains("Error cargando campos", component.Find(".error-message").TextContent);
}
```

## 📋 Checklist de Integración

- [ ] ✅ Componente CustomFieldsSection integrado
- [ ] 🔒 Permisos de usuario validados
- [ ] 📊 Validaciones implementadas
- [ ] 🎨 Estilos personalizados aplicados
- [ ] 🚀 Optimizaciones de performance implementadas
- [ ] 📈 Métricas de uso configuradas
- [ ] 🧪 Tests unitarios creados
- [ ] 📚 Documentación actualizada
- [ ] 🐛 Manejo de errores implementado
- [ ] 🔄 Estados offline manejados

## 🎉 Conclusión

El sistema de campos personalizados está diseñado para ser:
- **Intuitivo**: Wizard visual para usuarios no-técnicos
- **Flexible**: Soporte para múltiples tipos y validaciones
- **Seguro**: Sistema de permisos granular
- **Performante**: Optimizaciones de cache y lazy loading
- **Mantenible**: Código modular y bien documentado

Siguiendo estas mejores prácticas, podrás integrar exitosamente el sistema en cualquier formulario existente y mantener un alto nivel de calidad y rendimiento.