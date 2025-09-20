# 🔧 Fase 4: Extensibilidad y Automatización

## 🎯 Objetivos
- Sistema de plugins para tipos de campos personalizados
- Workflows y automatizaciones basadas en custom fields
- API pública para integraciones externas
- Sistema de eventos y triggers

## ⏱️ Duración Estimada: 3-4 semanas
## 🚨 Prioridad: MEDIA

---

## 🔌 Sistema de Plugins

### 1. **Plugin Architecture**

#### Plugin Interface
```csharp
public interface ICustomFieldPlugin
{
    string Name { get; }
    string Version { get; }
    string Author { get; }
    string Description { get; }
    List<CustomFieldType> SupportedTypes { get; }

    Task<ValidationResult> ValidateAsync(object value, ValidationConfig config);
    Task<object> TransformValueAsync(object value, TransformationConfig config);
    Task<string> RenderAsync(object value, UIConfig config);
}

public abstract class CustomFieldPluginBase : ICustomFieldPlugin
{
    public abstract string Name { get; }
    public abstract string Version { get; }
    public abstract string Author { get; }
    public abstract string Description { get; }
    public abstract List<CustomFieldType> SupportedTypes { get; }

    public virtual Task<ValidationResult> ValidateAsync(object value, ValidationConfig config)
    {
        return Task.FromResult(ValidationResult.Success());
    }

    public virtual Task<object> TransformValueAsync(object value, TransformationConfig config)
    {
        return Task.FromResult(value);
    }

    public virtual Task<string> RenderAsync(object value, UIConfig config)
    {
        return Task.FromResult(value?.ToString() ?? "");
    }
}
```

#### Plugin Examples
```csharp
// Plugin para campos de geolocalización
public class GeolocationFieldPlugin : CustomFieldPluginBase
{
    public override string Name => "Geolocation Field";
    public override string Version => "1.0.0";
    public override string Author => "CustomFields Team";
    public override string Description => "GPS coordinates and address lookup";
    public override List<CustomFieldType> SupportedTypes => new() { CustomFieldType.Geolocation };

    public override async Task<ValidationResult> ValidateAsync(object value, ValidationConfig config)
    {
        if (value is GeolocationValue geo)
        {
            if (Math.Abs(geo.Latitude) > 90 || Math.Abs(geo.Longitude) > 180)
                return ValidationResult.Error("Invalid GPS coordinates");
        }
        return ValidationResult.Success();
    }
}

// Plugin para campos de código de barras
public class BarcodeFieldPlugin : CustomFieldPluginBase
{
    public override string Name => "Barcode Field";
    public override string Version => "1.0.0";
    public override string Author => "CustomFields Team";
    public override string Description => "Barcode and QR code scanner";
    public override List<CustomFieldType> SupportedTypes => new() { CustomFieldType.Barcode };

    public override async Task<ValidationResult> ValidateAsync(object value, ValidationConfig config)
    {
        if (value is string barcode && !string.IsNullOrEmpty(barcode))
        {
            // Validar formato de código de barras
            return ValidateBarcodeFormat(barcode) ?
                ValidationResult.Success() :
                ValidationResult.Error("Invalid barcode format");
        }
        return ValidationResult.Success();
    }
}
```

### 2. **Plugin Management System**

#### Plugin Registry
```csharp
public interface IPluginRegistry
{
    Task<List<ICustomFieldPlugin>> GetAvailablePluginsAsync();
    Task<ICustomFieldPlugin> GetPluginAsync(string name);
    Task RegisterPluginAsync(ICustomFieldPlugin plugin);
    Task UnregisterPluginAsync(string name);
    Task<bool> IsPluginActiveAsync(string name, Guid organizationId);
    Task ActivatePluginAsync(string name, Guid organizationId);
    Task DeactivatePluginAsync(string name, Guid organizationId);
}

public class PluginRegistry : IPluginRegistry
{
    private readonly Dictionary<string, ICustomFieldPlugin> _plugins = new();
    private readonly AppDbContext _context;

    public async Task RegisterPluginAsync(ICustomFieldPlugin plugin)
    {
        _plugins[plugin.Name] = plugin;

        // Guardar información del plugin en BD
        var pluginInfo = new SystemPlugin
        {
            Name = plugin.Name,
            Version = plugin.Version,
            Author = plugin.Author,
            Description = plugin.Description,
            IsSystemPlugin = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.SystemPlugins.Add(pluginInfo);
        await _context.SaveChangesAsync();
    }
}
```

#### Plugin Configuration UI
```razor
@* Frontend/Components/Admin/PluginManager.razor *@
<RadzenDataGrid Data="@availablePlugins" TItem="PluginInfo">
    <Columns>
        <RadzenDataGridColumn Property="Name" Title="Plugin Name" />
        <RadzenDataGridColumn Property="Version" Title="Version" />
        <RadzenDataGridColumn Property="Author" Title="Author" />
        <RadzenDataGridColumn Property="Description" Title="Description" />
        <RadzenDataGridColumn Title="Status">
            <Template Context="plugin">
                <RadzenSwitch @bind-Value="plugin.IsActive"
                             Change="@(async (bool active) => await TogglePlugin(plugin, active))" />
            </Template>
        </RadzenDataGridColumn>
        <RadzenDataGridColumn Title="Actions">
            <Template Context="plugin">
                <RadzenButton Icon="settings" Size="ButtonSize.Small"
                             Click="@(() => ConfigurePlugin(plugin))" />
                <RadzenButton Icon="delete" Size="ButtonSize.Small"
                             ButtonStyle="ButtonStyle.Danger"
                             Click="@(() => UninstallPlugin(plugin))" />
            </Template>
        </RadzenDataGridColumn>
    </Columns>
</RadzenDataGrid>
```

## 🔄 Sistema de Workflows

### 1. **Workflow Engine**

#### Workflow Definition
```csharp
public class CustomFieldWorkflow
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string EntityName { get; set; }
    public List<WorkflowTrigger> Triggers { get; set; }
    public List<WorkflowAction> Actions { get; set; }
    public bool IsActive { get; set; }
    public Guid OrganizationId { get; set; }
}

public class WorkflowTrigger
{
    public TriggerType Type { get; set; } // OnCreate, OnUpdate, OnDelete, OnFieldChange
    public string FieldName { get; set; } // Para OnFieldChange
    public List<Condition> Conditions { get; set; }
}

public class WorkflowAction
{
    public ActionType Type { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
}

public enum ActionType
{
    SendEmail,
    SendNotification,
    UpdateField,
    CreateRecord,
    CallWebhook,
    RunScript,
    SendSMS,
    GenerateDocument
}
```

#### Workflow Examples
```json
// Workflow: Notificar cuando salario > 100k
{
    "name": "High Salary Notification",
    "entityName": "Empleado",
    "triggers": [{
        "type": "OnFieldChange",
        "fieldName": "Sueldo",
        "conditions": [{
            "field": "Sueldo",
            "operator": "greater_than",
            "value": 100000
        }]
    }],
    "actions": [{
        "type": "SendEmail",
        "parameters": {
            "to": "hr@company.com",
            "subject": "High Salary Alert",
            "template": "high_salary_template"
        }
    }]
}
```

### 2. **Workflow Designer**

#### Visual Workflow Builder
```razor
@* Frontend/Components/Workflows/WorkflowDesigner.razor *@
<div class="workflow-canvas">
    <div class="workflow-sidebar">
        <h4>Triggers</h4>
        <div class="trigger-items">
            @foreach (var trigger in availableTriggers)
            {
                <div class="trigger-item" draggable="true"
                     @ondragstart="@((e) => StartDrag(trigger))">
                    <RadzenIcon Icon="@trigger.Icon" />
                    @trigger.Name
                </div>
            }
        </div>

        <h4>Actions</h4>
        <div class="action-items">
            @foreach (var action in availableActions)
            {
                <div class="action-item" draggable="true"
                     @ondragstart="@((e) => StartDrag(action))">
                    <RadzenIcon Icon="@action.Icon" />
                    @action.Name
                </div>
            }
        </div>
    </div>

    <div class="workflow-designer">
        @RenderWorkflowSteps()
    </div>
</div>
```

## 🌐 API Pública

### 1. **RESTful API para Integraciones**

#### API Endpoints
```csharp
[ApiController]
[Route("api/v1/custom-fields")]
[Authorize(ApiKey = true)] // Autenticación por API Key
public class PublicCustomFieldsController : ControllerBase
{
    /// <summary>
    /// Get custom field definitions for an entity
    /// </summary>
    [HttpGet("{entityName}/definitions")]
    public async Task<ApiResponse<List<CustomFieldDefinitionDto>>> GetDefinitions(
        string entityName,
        [FromQuery] string? category = null,
        [FromQuery] bool? includeInactive = false)
    {
        // Implementation
    }

    /// <summary>
    /// Get custom field values for a specific record
    /// </summary>
    [HttpGet("{entityName}/{recordId}/values")]
    public async Task<ApiResponse<Dictionary<string, object>>> GetValues(
        string entityName,
        Guid recordId)
    {
        // Implementation
    }

    /// <summary>
    /// Update custom field values for a record
    /// </summary>
    [HttpPut("{entityName}/{recordId}/values")]
    public async Task<ApiResponse<bool>> UpdateValues(
        string entityName,
        Guid recordId,
        [FromBody] Dictionary<string, object> values)
    {
        // Implementation with validation
    }

    /// <summary>
    /// Bulk update custom field values
    /// </summary>
    [HttpPut("{entityName}/bulk-update")]
    public async Task<ApiResponse<BulkUpdateResult>> BulkUpdate(
        string entityName,
        [FromBody] BulkUpdateRequest request)
    {
        // Implementation
    }
}
```

#### API Documentation
```csharp
// Swagger configuration with examples
public class CustomFieldsApiDocumentation
{
    public static void ConfigureSwagger(SwaggerGenOptions options)
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Custom Fields API",
            Version = "v1",
            Description = "Public API for managing custom fields and values"
        });

        // Add examples
        options.SchemaFilter<CustomFieldsExampleSchemaFilter>();

        // API Key authentication
        options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = "X-API-Key"
        });
    }
}
```

### 2. **Webhooks System**

#### Webhook Configuration
```csharp
public class CustomFieldWebhook
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
    public List<WebhookEvent> Events { get; set; }
    public string EntityName { get; set; }
    public string SecretKey { get; set; }
    public bool IsActive { get; set; }
    public int RetryCount { get; set; }
    public TimeSpan Timeout { get; set; }
}

public enum WebhookEvent
{
    CustomFieldCreated,
    CustomFieldUpdated,
    CustomFieldDeleted,
    CustomFieldValueChanged,
    FormSubmitted
}
```

#### Webhook Delivery
```csharp
public class WebhookDeliveryService
{
    public async Task DeliverWebhookAsync(CustomFieldWebhook webhook, object payload)
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = webhook.Timeout;

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Add signature header for security
        var signature = GenerateSignature(json, webhook.SecretKey);
        content.Headers.Add("X-CustomFields-Signature", signature);

        var response = await httpClient.PostAsync(webhook.Url, content);

        if (!response.IsSuccessStatusCode)
        {
            await HandleWebhookFailure(webhook, response);
        }
    }
}
```

## 🔗 Sistema de Eventos

### 1. **Event System**

#### Event Bus Implementation
```csharp
public interface ICustomFieldEventBus
{
    Task PublishAsync<T>(T @event) where T : ICustomFieldEvent;
    void Subscribe<T>(Func<T, Task> handler) where T : ICustomFieldEvent;
}

public interface ICustomFieldEvent
{
    Guid EventId { get; }
    DateTime Timestamp { get; }
    string EventType { get; }
    Guid OrganizationId { get; }
}

public class CustomFieldValueChangedEvent : ICustomFieldEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string EventType => "CustomFieldValueChanged";
    public Guid OrganizationId { get; set; }

    public string EntityName { get; set; }
    public Guid RecordId { get; set; }
    public string FieldName { get; set; }
    public object OldValue { get; set; }
    public object NewValue { get; set; }
    public Guid UserId { get; set; }
}
```

### 2. **Event Handlers**

#### Built-in Event Handlers
```csharp
public class AuditEventHandler
{
    public async Task Handle(CustomFieldValueChangedEvent @event)
    {
        // Log change to audit table
        var auditEntry = new SystemCustomFieldAuditLog
        {
            EventId = @event.EventId,
            EntityName = @event.EntityName,
            RecordId = @event.RecordId,
            FieldName = @event.FieldName,
            OldValue = JsonSerializer.Serialize(@event.OldValue),
            NewValue = JsonSerializer.Serialize(@event.NewValue),
            UserId = @event.UserId,
            Timestamp = @event.Timestamp
        };

        await _context.SystemCustomFieldAuditLog.AddAsync(auditEntry);
        await _context.SaveChangesAsync();
    }
}

public class NotificationEventHandler
{
    public async Task Handle(CustomFieldValueChangedEvent @event)
    {
        // Check if there are notification rules for this field
        var rules = await GetNotificationRules(@event.EntityName, @event.FieldName);

        foreach (var rule in rules)
        {
            if (rule.ShouldTrigger(@event.OldValue, @event.NewValue))
            {
                await _notificationService.SendNotificationAsync(rule, @event);
            }
        }
    }
}
```

## 📊 Telemetría y Monitoreo

### 1. **Performance Monitoring**

#### Custom Metrics
```csharp
public class CustomFieldsMetrics
{
    private readonly IMetricsLogger _metrics;

    public void RecordFieldRender(string fieldType, TimeSpan duration)
    {
        _metrics.Counter("custom_fields.renders.total")
               .WithTag("field_type", fieldType)
               .Increment();

        _metrics.Histogram("custom_fields.render.duration")
               .WithTag("field_type", fieldType)
               .Record(duration.TotalMilliseconds);
    }

    public void RecordPluginExecution(string pluginName, string operation, TimeSpan duration, bool success)
    {
        _metrics.Counter("custom_fields.plugin.executions")
               .WithTag("plugin", pluginName)
               .WithTag("operation", operation)
               .WithTag("success", success.ToString())
               .Increment();
    }
}
```

### 2. **Health Checks**

#### System Health Monitoring
```csharp
public class CustomFieldsHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context)
    {
        try
        {
            // Check database connectivity
            var dbCheck = await CheckDatabaseHealthAsync();

            // Check plugin system
            var pluginCheck = await CheckPluginSystemHealthAsync();

            // Check webhook delivery
            var webhookCheck = await CheckWebhookHealthAsync();

            if (dbCheck && pluginCheck && webhookCheck)
                return HealthCheckResult.Healthy("All systems operational");
            else
                return HealthCheckResult.Degraded("Some systems experiencing issues");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Critical system failure", ex);
        }
    }
}
```

## 🛠️ Plan de Implementación

### Semana 1: Plugin System Foundation
- **Días 1-2**: Plugin interface y registry
- **Días 3-4**: Plugin management UI
- **Día 5**: Ejemplo de plugins básicos

### Semana 2: Workflow Engine
- **Días 1-2**: Workflow definition y engine
- **Días 3-4**: Workflow designer UI
- **Día 5**: Triggers y actions básicos

### Semana 3: Public API
- **Días 1-2**: RESTful API endpoints
- **Días 3-4**: API documentation y testing
- **Día 5**: Authentication y rate limiting

### Semana 4: Events y Monitoring
- **Días 1-2**: Event system y webhooks
- **Días 3-4**: Telemetría y health checks
- **Día 5**: Testing integral y optimización

## 📈 Métricas de Éxito

- ✅ 5+ plugins funcionando correctamente
- ✅ Workflow engine procesando 1000+ eventos/día
- ✅ API pública con 99.9% uptime
- ✅ Event system latency < 50ms
- ✅ Webhooks delivery rate > 95%
- ✅ Plugin performance overhead < 10%

## 📁 Archivos Nuevos

### Backend
- `CustomFields.API/Plugins/ICustomFieldPlugin.cs`
- `CustomFields.API/Plugins/PluginRegistry.cs`
- `CustomFields.API/Workflows/WorkflowEngine.cs`
- `CustomFields.API/Controllers/PublicCustomFieldsController.cs`
- `CustomFields.API/Controllers/WebhooksController.cs`
- `CustomFields.API/Events/ICustomFieldEventBus.cs`
- `CustomFields.API/Events/CustomFieldEvents.cs`
- `CustomFields.API/Health/CustomFieldsHealthCheck.cs`

### Frontend
- `Frontend/Components/Admin/PluginManager.razor`
- `Frontend/Components/Workflows/WorkflowDesigner.razor`
- `Frontend/Components/Webhooks/WebhookManager.razor`

### Plugins (Examples)
- `CustomFields.Plugins.Geolocation/GeolocationFieldPlugin.cs`
- `CustomFields.Plugins.Barcode/BarcodeFieldPlugin.cs`
- `CustomFields.Plugins.RichText/RichTextFieldPlugin.cs`