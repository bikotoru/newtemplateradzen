# 📋 Fase 1: Fundación y Primer Campo Funcional

## 🎯 Objetivo
Crear la infraestructura básica para campos personalizados y lograr que **UN campo de texto simple** funcione end-to-end en el formulario de Empleado.

## ⏱️ Duración Estimada: 1-2 semanas

---

## 🗂️ Estructura de Archivos a Crear

### **Backend: CustomFields.API (Nueva API Separada)**
```
/CustomFields.API/
├── Controllers/
│   ├── CustomFieldDefinitionsController.cs
│   └── CustomFieldValuesController.cs
├── Services/
│   ├── ICustomFieldDefinitionService.cs
│   ├── CustomFieldDefinitionService.cs
│   ├── ICustomFieldValidationService.cs
│   └── CustomFieldValidationService.cs
├── Models/
│   ├── CustomFieldDefinition.cs
│   ├── CustomFieldValue.cs
│   └── DTOs/
│       ├── CreateCustomFieldRequest.cs
│       ├── UpdateCustomFieldRequest.cs
│       └── CustomFieldDefinitionDto.cs
├── Data/
│   └── CustomFieldsDbContext.cs
├── Extensions/
│   └── ServiceCollectionExtensions.cs
└── Program.cs
```

### **Frontend: Componentes Blazor**
```
/Frontend/Components/CustomFields/
├── CustomFieldsSection.razor
├── CustomFieldsSection.razor.cs
├── Editors/
│   ├── TextFieldEditor.razor
│   ├── NumberFieldEditor.razor
│   └── DateFieldEditor.razor
└── Designer/
    ├── CustomFieldDesigner.razor
    └── CustomFieldDesigner.razor.cs
```

---

## 🗄️ Base de Datos: Extensión de SystemConfig

### **Opción 1: Extender SystemConfig Existente**
```sql
-- Agregar campos a system_config existente
ALTER TABLE system_config ADD EntityName NVARCHAR(100) NULL;
ALTER TABLE system_config ADD FieldType NVARCHAR(50) NULL;
ALTER TABLE system_config ADD DisplayName NVARCHAR(255) NULL;
ALTER TABLE system_config ADD IsRequired BIT DEFAULT 0;
ALTER TABLE system_config ADD ValidationRules NVARCHAR(MAX) NULL; -- JSON
ALTER TABLE system_config ADD UIConfig NVARCHAR(MAX) NULL; -- JSON para configuración UI
```

### **Opción 2: Nueva Tabla Especializada (RECOMENDADA)**
```sql
-- Nueva tabla para definiciones de campos personalizados
CREATE TABLE custom_field_definitions (
    Id UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
    EntityName NVARCHAR(100) NOT NULL, -- "Empleado", "Empresa", etc.
    FieldName NVARCHAR(100) NOT NULL,  -- "telefono_emergencia"
    DisplayName NVARCHAR(255) NOT NULL, -- "Teléfono de Emergencia"
    FieldType NVARCHAR(50) NOT NULL,   -- "text", "number", "date", etc.
    IsRequired BIT DEFAULT 0,
    DefaultValue NVARCHAR(MAX) NULL,
    ValidationConfig NVARCHAR(MAX) NULL, -- JSON: {"maxLength": 15, "pattern": "phone"}
    UIConfig NVARCHAR(MAX) NULL,        -- JSON: {"placeholder": "Ej: +56912345678"}
    SortOrder INT DEFAULT 0,
    
    -- Campos de auditoría estándar
    FechaCreacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
    FechaModificacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
    OrganizationId UNIQUEIDENTIFIER NULL,
    CreadorId UNIQUEIDENTIFIER NULL,
    ModificadorId UNIQUEIDENTIFIER NULL,
    Active BIT DEFAULT 1 NOT NULL,
    
    -- Índices
    CONSTRAINT UQ_custom_field_definitions_entity_field_org 
        UNIQUE (EntityName, FieldName, OrganizationId)
);

-- Índices para performance
CREATE INDEX IX_custom_field_definitions_entity_org 
    ON custom_field_definitions(EntityName, OrganizationId, Active);
```

---

## 🔧 Implementación Paso a Paso

### **Paso 1: CustomFields.API - Infraestructura Básica**

#### **1.1 CustomFieldDefinition Model**
```csharp
public class CustomFieldDefinition
{
    public Guid Id { get; set; }
    public string EntityName { get; set; } = null!; // "Empleado"
    public string FieldName { get; set; } = null!;  // "telefono_emergencia"
    public string DisplayName { get; set; } = null!; // "Teléfono de Emergencia"
    public string FieldType { get; set; } = null!;  // "text"
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public string? ValidationConfig { get; set; } // JSON
    public string? UIConfig { get; set; }         // JSON
    public int SortOrder { get; set; }
    
    // Campos base heredados de BaseEntity pattern
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaModificacion { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? CreadorId { get; set; }
    public Guid? ModificadorId { get; set; }
    public bool Active { get; set; }
}
```

#### **1.2 Controller Mínimo**
```csharp
[ApiController]
[Route("api/[controller]")]
public class CustomFieldDefinitionsController : ControllerBase
{
    // Solo métodos esenciales para Fase 1:
    // GET /api/customfielddefinitions/{entityName} - Obtener campos por entidad
    // POST /api/customfielddefinitions - Crear campo
    // PUT /api/customfielddefinitions/{id} - Actualizar campo
    // DELETE /api/customfielddefinitions/{id} - Eliminar campo
}
```

### **Paso 2: Integración con API Principal**

#### **2.1 Extensión del Servicio de Empleado**
```csharp
// En EmpleadoService existente
public async Task<List<CustomFieldDefinition>> GetCustomFieldsAsync(string entityName)
{
    // Llamada HTTP a CustomFields.API
    var response = await _httpClient.GetAsync($"customfields/definitions/{entityName}");
    return await response.Content.ReadFromJsonAsync<List<CustomFieldDefinition>>();
}

public async Task<EmpleadoDto> GetEmpleadoWithCustomFieldsAsync(Guid id)
{
    var empleado = await GetAsync(id);
    var customFields = await GetCustomFieldsAsync("Empleado");
    
    // Deserializar campo Custom de empleado
    var customData = JsonSerializer.Deserialize<Dictionary<string, object>>(empleado.Custom ?? "{}");
    
    return new EmpleadoDto 
    {
        // Campos normales...
        CustomFieldDefinitions = customFields,
        CustomFieldValues = customData
    };
}
```

### **Paso 3: Frontend - Componente CustomFieldsSection**

#### **3.1 Componente Base**
```razor
<!-- CustomFieldsSection.razor -->
@if (CustomFieldDefinitions?.Any() == true)
{
    <div class="custom-fields-section">
        <h4>Campos Adicionales</h4>
        
        @foreach (var field in CustomFieldDefinitions.OrderBy(f => f.SortOrder))
        {
            <div class="custom-field-container">
                @switch (field.FieldType.ToLower())
                {
                    case "text":
                        <TextFieldEditor Field="@field" 
                                       Value="@GetFieldValue(field.FieldName)" 
                                       OnValueChanged="@(v => SetFieldValue(field.FieldName, v))" />
                        break;
                    
                    default:
                        <p>Tipo de campo no soportado: @field.FieldType</p>
                        break;
                }
            </div>
        }
    </div>
}

@code {
    [Parameter] public List<CustomFieldDefinition>? CustomFieldDefinitions { get; set; }
    [Parameter] public Dictionary<string, object>? CustomFieldValues { get; set; }
    [Parameter] public EventCallback<Dictionary<string, object>> OnValuesChanged { get; set; }
    
    // Métodos para obtener/establecer valores...
}
```

#### **3.2 Editor de Texto Simple**
```razor
<!-- TextFieldEditor.razor -->
<ValidatedInput FieldName="@Field.FieldName" Value="@Value?.ToString()">
    <RadzenFormField Text="@Field.DisplayName" Style="width: 100%">
        <RadzenTextBox Value="@(Value?.ToString())" 
                       Placeholder="@GetPlaceholder()" 
                       @oninput="@(args => HandleValueChange(args.Value?.ToString()))" />
    </RadzenFormField>
</ValidatedInput>

@code {
    [Parameter] public CustomFieldDefinition Field { get; set; } = null!;
    [Parameter] public object? Value { get; set; }
    [Parameter] public EventCallback<object?> OnValueChanged { get; set; }
    
    private void HandleValueChange(string? newValue)
    {
        OnValueChanged.InvokeAsync(newValue);
    }
    
    private string GetPlaceholder()
    {
        var uiConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(Field.UIConfig ?? "{}");
        return uiConfig.TryGetValue("placeholder", out var placeholder) 
            ? placeholder.ToString() ?? "" 
            : $"Ingrese {Field.DisplayName.ToLower()}";
    }
}
```

### **Paso 4: Integración en EmpleadoFormulario.razor**

```razor
<!-- Agregar en EmpleadoFormulario.razor -->
<CrmTab Id="tab-custom" Title="Campos Adicionales" Icon="settings">
    <CustomFieldsSection CustomFieldDefinitions="@customFieldDefinitions"
                        CustomFieldValues="@customFieldValues"
                        OnValuesChanged="@HandleCustomFieldsChanged" />
</CrmTab>

@code {
    private List<CustomFieldDefinition>? customFieldDefinitions;
    private Dictionary<string, object>? customFieldValues;
    
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        
        // Cargar definiciones de campos personalizados
        customFieldDefinitions = await EmpleadoService.GetCustomFieldDefinitionsAsync("Empleado");
        
        // Si estamos editando, cargar valores existentes
        if (isEditMode && entity?.Custom != null)
        {
            customFieldValues = JsonSerializer.Deserialize<Dictionary<string, object>>(entity.Custom);
        }
        else
        {
            customFieldValues = new Dictionary<string, object>();
        }
    }
    
    private async Task HandleCustomFieldsChanged(Dictionary<string, object> values)
    {
        customFieldValues = values;
        
        // Serializar y guardar en el campo Custom de la entidad
        entity.Custom = JsonSerializer.Serialize(values);
        
        // Marcar como modificado para validaciones
        StateHasChanged();
    }
}
```

---

## ✅ Criterios de Éxito - Fase 1

### **Funcionalidad Mínima Requerida:**
1. ✅ **API Funcionando**: CustomFields.API responde correctamente
2. ✅ **Base de Datos**: Tabla creada y operaciones CRUD funcionando
3. ✅ **Campo Texto**: Un campo de texto personalizado se puede crear y configurar
4. ✅ **Formulario**: El campo aparece en EmpleadoFormulario.razor
5. ✅ **Persistencia**: Los valores se guardan en el campo Custom como JSON
6. ✅ **Validación Básica**: Required/Optional funciona correctamente
7. ✅ **Multitenancy**: Campos por organización funcionan

### **Pruebas de Aceptación:**
1. **Crear Campo**: Admin puede crear un campo "Teléfono de Emergencia" de tipo texto
2. **Mostrar Campo**: El campo aparece en tab "Campos Adicionales" del formulario de empleado
3. **Guardar Valor**: Al llenar el campo y guardar, el valor persiste correctamente
4. **Cargar Valor**: Al editar empleado existente, el valor se muestra correctamente
5. **Validación**: Si se marca como requerido, la validación funciona
6. **Organización**: Campos creados en una organización no aparecen en otra

---

## 🚨 Puntos Críticos de Esta Fase

### **⚠️ Decisiones Arquitectónicas Importantes:**
1. **API Separada vs Integrada**: Se opta por API separada para aislamiento
2. **JSON en Campo Custom**: Aprovecha campo existente vs nueva tabla de valores
3. **Validación Client-Side**: Integra con FormValidator existente
4. **Componentización**: Cada tipo de campo como componente separado

### **⚠️ Riesgos y Mitigaciones:**
1. **Serialización JSON**: Verificar que tipos complejos se serialicen correctamente
2. **Performance**: Evaluar impacto de cargar definiciones en cada formulario
3. **Validaciones**: Asegurar que validaciones custom no rompan sistema existente
4. **Multitenancy**: Verificar aislamiento correcto entre organizaciones

### **⚠️ Testing Crítico:**
- Pruebas de integración entre APIs
- Validación de JSON serialization/deserialization
- Pruebas de permisos y multitenancy
- Pruebas de performance con múltiples campos

---

## 📈 Métricas de Éxito

- **Tiempo de respuesta**: < 200ms para cargar campos personalizados
- **Usabilidad**: Admin puede crear campo en < 2 minutos
- **Estabilidad**: 0 errores en serialización/deserialización JSON
- **Compatibilidad**: Sistema existente funciona sin cambios

---

**🎯 Al completar Fase 1, tendremos un campo personalizado completamente funcional que servirá como base sólida para las siguientes fases.**