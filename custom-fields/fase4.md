# 📋 Fase 4: Diseñador Visual (La Fase Crítica)

## 🎯 Objetivo
Crear el diseñador visual que permite a los administradores configurar campos personalizados de manera intuitiva. **Esta es la fase donde la mayoría de proyectos fallan** - requiere UI/UX excepcional para ser adoptado.

## ⏱️ Duración Estimada: 2-3 semanas
**⚠️ Esta es la fase más compleja y crítica del proyecto**

---

## 🎨 Arquitectura del Diseñador

### **Componente Principal: CustomFieldDesigner.razor**
```
/Frontend/Modules/Admin/CustomFields/
├── CustomFieldDesigner.razor              # Página principal del diseñador
├── CustomFieldDesigner.razor.cs
├── Components/
│   ├── FieldDefinitionForm.razor          # Formulario para definir campo
│   ├── FieldDefinitionForm.razor.cs
│   ├── FieldTypeSelector.razor            # Selector de tipo de campo
│   ├── ValidationBuilder.razor            # Constructor de validaciones
│   ├── ConditionBuilder.razor             # Constructor de condiciones (IF)
│   ├── PermissionSelector.razor           # Selector de permisos
│   ├── FieldPreview.razor                 # Preview en tiempo real
│   └── FormPreview.razor                  # Preview del formulario completo
├── Models/
│   ├── FieldDesignerState.cs             # Estado del diseñador
│   ├── FieldPreviewData.cs               # Datos para preview
│   └── ValidationRuleBuilderModel.cs     # Modelo para constructor de validaciones
└── Services/
    ├── CustomFieldDesignerService.cs      # Lógica del diseñador
    └── FieldPreviewService.cs             # Servicio para preview
```

---

## 🏗️ Diseño de Interfaz - Split View

### **Layout Principal**
```razor
<!-- CustomFieldDesigner.razor -->
<div class="custom-field-designer">
    
    <!-- Header con acciones principales -->
    <div class="designer-header">
        <div class="entity-selector">
            <RadzenDropDown @bind-Value="selectedEntity" 
                           Data="@availableEntities" 
                           TextProperty="Name" 
                           ValueProperty="Value"
                           Placeholder="Seleccionar entidad..." />
        </div>
        
        <div class="header-actions">
            <RadzenButton Text="Nuevo Campo" 
                         Icon="add" 
                         ButtonStyle="ButtonStyle.Primary"
                         Click="@CreateNewField" />
            
            <RadzenButton Text="Vista Previa Completa" 
                         Icon="preview" 
                         ButtonStyle="ButtonStyle.Light"
                         Click="@ShowFullPreview" />
                         
            <RadzenButton Text="Guardar Configuración" 
                         Icon="save" 
                         ButtonStyle="ButtonStyle.Success"
                         Click="@SaveConfiguration" />
        </div>
    </div>
    
    <!-- Contenido principal - Split View -->
    <div class="designer-content">
        
        <!-- Panel Izquierdo: Lista y Configuración -->
        <div class="left-panel">
            
            <!-- Lista de campos existentes -->
            <div class="fields-list">
                <h4>Campos Personalizados</h4>
                
                <div class="fields-container">
                    @foreach (var field in customFields)
                    {
                        <div class="field-item @(selectedField?.Id == field.Id ? "selected" : "")"
                             @onclick="() => SelectField(field)">
                            
                            <div class="field-header">
                                <span class="field-name">@field.DisplayName</span>
                                <span class="field-type">@field.FieldType</span>
                            </div>
                            
                            <div class="field-details">
                                <span class="field-required">@(field.IsRequired ? "Requerido" : "Opcional")</span>
                                @if (HasConditions(field))
                                {
                                    <span class="field-conditional">🔄 Condicional</span>
                                }
                                @if (HasPermissions(field))
                                {
                                    <span class="field-permissions">🔐 Permisos</span>
                                }
                            </div>
                            
                            <div class="field-actions">
                                <RadzenButton Icon="edit" 
                                             Size="ButtonSize.Small" 
                                             ButtonStyle="ButtonStyle.Light"
                                             Click="() => EditField(field)" />
                                <RadzenButton Icon="delete" 
                                             Size="ButtonSize.Small" 
                                             ButtonStyle="ButtonStyle.Danger"
                                             Click="() => DeleteField(field)" />
                            </div>
                        </div>
                    }
                </div>
            </div>
            
            <!-- Formulario de configuración -->
            @if (selectedField != null)
            {
                <div class="field-configuration">
                    <FieldDefinitionForm Field="@selectedField" 
                                        OnFieldChanged="@HandleFieldChanged"
                                        AvailableFields="@GetAvailableFieldsForConditions()" />
                </div>
            }
        </div>
        
        <!-- Panel Derecho: Preview en Tiempo Real -->
        <div class="right-panel">
            <div class="preview-container">
                <h4>Vista Previa</h4>
                
                <!-- Tabs para diferentes tipos de preview -->
                <RadzenTabs>
                    <Tabs>
                        <RadzenTabsItem Text="Campo Individual">
                            @if (selectedField != null)
                            {
                                <FieldPreview Field="@selectedField" 
                                            SampleData="@GetSampleData()" />
                            }
                        </RadzenTabsItem>
                        
                        <RadzenTabsItem Text="Formulario Completo">
                            <FormPreview EntityName="@selectedEntity" 
                                        CustomFields="@customFields"
                                        SampleData="@GetSampleEntityData()" />
                        </RadzenTabsItem>
                        
                        <RadzenTabsItem Text="Diferentes Estados">
                            <StatePreview Field="@selectedField" />
                        </RadzenTabsItem>
                    </Tabs>
                </RadzenTabs>
            </div>
        </div>
    </div>
</div>
```

---

## 🔧 Componentes Especializados

### **1. FieldDefinitionForm - Formulario Principal**
```razor
<!-- FieldDefinitionForm.razor -->
<div class="field-definition-form">
    
    <!-- Información básica -->
    <RadzenFieldset Text="Información Básica">
        <div class="form-grid">
            <RadzenFormField Text="Nombre del Campo">
                <RadzenTextBox @bind-Value="Field.FieldName" 
                              Placeholder="ej: telefono_emergencia" />
            </RadzenFormField>
            
            <RadzenFormField Text="Etiqueta">
                <RadzenTextBox @bind-Value="Field.DisplayName" 
                              Placeholder="ej: Teléfono de Emergencia" />
            </RadzenFormField>
            
            <RadzenFormField Text="Tipo de Campo">
                <FieldTypeSelector @bind-Value="Field.FieldType" 
                                  OnTypeChanged="@HandleTypeChanged" />
            </RadzenFormField>
            
            <RadzenFormField Text="Requerido">
                <RadzenSwitch @bind-Value="Field.IsRequired" />
            </RadzenFormField>
        </div>
    </RadzenFieldset>
    
    <!-- Configuración específica por tipo -->
    @if (!string.IsNullOrEmpty(Field.FieldType))
    {
        <RadzenFieldset Text="Configuración del Campo">
            @switch (Field.FieldType.ToLower())
            {
                case "text":
                case "textarea":
                    <TextFieldConfiguration @bind-Config="textConfig" />
                    break;
                    
                case "number":
                    <NumberFieldConfiguration @bind-Config="numberConfig" />
                    break;
                    
                case "select":
                case "multiselect":
                    <SelectFieldConfiguration @bind-Config="selectConfig" />
                    break;
                    
                // ... otros tipos
            }
        </RadzenFieldset>
    }
    
    <!-- Constructor de validaciones -->
    <RadzenFieldset Text="Validaciones">
        <ValidationBuilder Field="@Field" 
                          @bind-ValidationConfig="validationConfig" />
    </RadzenFieldset>
    
    <!-- Constructor de condiciones -->
    <RadzenFieldset Text="Condiciones (Opcional)">
        <ConditionBuilder Field="@Field"
                         AvailableFields="@AvailableFields"
                         @bind-Conditions="conditions" />
    </RadzenFieldset>
    
    <!-- Selector de permisos -->
    <RadzenFieldset Text="Permisos">
        <PermissionSelector Field="@Field" 
                           @bind-Permissions="permissions" />
    </RadzenFieldset>
    
</div>
```

### **2. FieldTypeSelector - Selector Visual de Tipos**
```razor
<!-- FieldTypeSelector.razor -->
<div class="field-type-selector">
    @foreach (var fieldType in availableFieldTypes)
    {
        <div class="field-type-option @(Value == fieldType.Value ? "selected" : "")"
             @onclick="() => SelectType(fieldType.Value)">
            
            <div class="type-icon">
                <RadzenIcon Icon="@fieldType.Icon" />
            </div>
            
            <div class="type-info">
                <div class="type-name">@fieldType.Name</div>
                <div class="type-description">@fieldType.Description</div>
            </div>
            
            @if (fieldType.HasAdvancedConfig)
            {
                <div class="type-badge">
                    <span class="badge badge-info">Configurable</span>
                </div>
            }
        </div>
    }
</div>

@code {
    private List<FieldTypeOption> availableFieldTypes = new()
    {
        new("text", "Texto", "Campo de texto corto", "edit", false),
        new("textarea", "Área de Texto", "Campo de texto largo", "subject", true),
        new("number", "Número", "Campo numérico", "functions", true),
        new("date", "Fecha", "Campo de fecha", "calendar_today", true),
        new("boolean", "Verdadero/Falso", "Campo de sí/no", "check_box", true),
        new("select", "Lista Desplegable", "Selección única", "arrow_drop_down", true),
        new("multiselect", "Selección Múltiple", "Selección múltiple", "check_box_outline_blank", true)
    };
}
```

### **3. ValidationBuilder - Constructor de Validaciones**
```razor
<!-- ValidationBuilder.razor -->
<div class="validation-builder">
    
    <!-- Validaciones básicas -->
    <div class="basic-validations">
        <RadzenCheckBox @bind-Value="validationConfig.Required" 
                       Name="required" />
        <RadzenLabel Text="Campo requerido" Component="required" />
    </div>
    
    <!-- Validaciones específicas por tipo -->
    @if (IsTextType())
    {
        <div class="text-validations">
            <div class="validation-row">
                <RadzenFormField Text="Longitud mínima">
                    <RadzenNumeric @bind-Value="validationConfig.MinLength" TValue="int?" />
                </RadzenFormField>
                
                <RadzenFormField Text="Longitud máxima">
                    <RadzenNumeric @bind-Value="validationConfig.MaxLength" TValue="int?" />
                </RadzenFormField>
            </div>
            
            <div class="validation-row">
                <RadzenFormField Text="Patrón (RegEx)">
                    <RadzenTextBox @bind-Value="validationConfig.Pattern" 
                                  Placeholder="ej: [0-9]{9}" />
                </RadzenTextBox>
                
                <RadzenFormField Text="Mensaje de error">
                    <RadzenTextBox @bind-Value="validationConfig.PatternMessage" 
                                  Placeholder="Formato inválido" />
                </RadzenFormField>
            </div>
        </div>
    }
    
    @if (IsNumberType())
    {
        <div class="number-validations">
            <div class="validation-row">
                <RadzenFormField Text="Valor mínimo">
                    <RadzenNumeric @bind-Value="validationConfig.Min" TValue="decimal?" />
                </RadzenFormField>
                
                <RadzenFormField Text="Valor máximo">
                    <RadzenNumeric @bind-Value="validationConfig.Max" TValue="decimal?" />
                </RadzenFormField>
            </div>
        </div>
    }
    
    <!-- Validaciones personalizadas -->
    <div class="custom-validations">
        <h5>Validaciones Personalizadas</h5>
        
        @foreach (var rule in customRules)
        {
            <div class="custom-rule">
                <RadzenDropDown @bind-Value="rule.RuleType" 
                               Data="@availableCustomRules"
                               TextProperty="Name"
                               ValueProperty="Type" />
                
                <RadzenTextBox @bind-Value="rule.ErrorMessage" 
                              Placeholder="Mensaje de error personalizado" />
                
                <RadzenButton Icon="delete" 
                             ButtonStyle="ButtonStyle.Danger"
                             Size="ButtonSize.Small"
                             Click="() => RemoveCustomRule(rule)" />
            </div>
        }
        
        <RadzenButton Text="Agregar Validación" 
                     Icon="add" 
                     ButtonStyle="ButtonStyle.Light"
                     Click="@AddCustomRule" />
    </div>
</div>
```

### **4. ConditionBuilder - Constructor de Condiciones (La Parte Compleja)**
```razor
<!-- ConditionBuilder.razor -->
<div class="condition-builder">
    
    @if (!hasConditions)
    {
        <div class="no-conditions">
            <p>Este campo no tiene condiciones. Se mostrará siempre.</p>
            <RadzenButton Text="Agregar Condición" 
                         Icon="add" 
                         ButtonStyle="ButtonStyle.Primary"
                         Click="@AddFirstCondition" />
        </div>
    }
    else
    {
        <div class="conditions-list">
            @foreach (var condition in conditions)
            {
                <div class="condition-item">
                    
                    <!-- Tipo de condición -->
                    <div class="condition-type">
                        <RadzenDropDown @bind-Value="condition.Type"
                                       Data="@conditionTypes"
                                       TextProperty="Label"
                                       ValueProperty="Value"
                                       Placeholder="Tipo de condición..." />
                    </div>
                    
                    <!-- Campo fuente -->
                    <div class="condition-field">
                        <span class="condition-label">cuando</span>
                        <RadzenDropDown @bind-Value="condition.SourceField"
                                       Data="@AvailableFields"
                                       TextProperty="DisplayName"
                                       ValueProperty="FieldName"
                                       Placeholder="Seleccionar campo..." />
                    </div>
                    
                    <!-- Operador -->
                    <div class="condition-operator">
                        <RadzenDropDown @bind-Value="condition.Operator"
                                       Data="@GetOperatorsForField(condition.SourceField)"
                                       TextProperty="Label"
                                       ValueProperty="Value"
                                       Placeholder="es..." />
                    </div>
                    
                    <!-- Valor o valores -->
                    <div class="condition-value">
                        @if (IsMultiValueOperator(condition.Operator))
                        {
                            <!-- Para operadores IN/NOT_IN -->
                            <MultiValueSelector @bind-Values="condition.Values" 
                                              SourceField="@condition.SourceField" />
                        }
                        else if (IsSingleValueOperator(condition.Operator))
                        {
                            <!-- Para operadores simples -->
                            <SingleValueSelector @bind-Value="condition.Value" 
                                               SourceField="@condition.SourceField" />
                        }
                        else
                        {
                            <!-- Para operadores sin valor (IS_EMPTY, etc.) -->
                            <span class="no-value">Sin valor requerido</span>
                        }
                    </div>
                    
                    <!-- Acciones -->
                    <div class="condition-actions">
                        <RadzenButton Icon="delete" 
                                     ButtonStyle="ButtonStyle.Danger"
                                     Size="ButtonSize.Small"
                                     Click="() => RemoveCondition(condition)" />
                    </div>
                    
                    <!-- Preview de la condición -->
                    <div class="condition-preview">
                        <small>@GetConditionDescription(condition)</small>
                    </div>
                </div>
            }
        </div>
        
        <div class="condition-actions">
            <RadzenButton Text="Agregar Otra Condición" 
                         Icon="add" 
                         ButtonStyle="ButtonStyle.Light"
                         Click="@AddCondition" />
                         
            <RadzenButton Text="Probar Condiciones" 
                         Icon="play_arrow" 
                         ButtonStyle="ButtonStyle.Info"
                         Click="@TestConditions" />
        </div>
    }
</div>

@code {
    private List<ConditionType> conditionTypes = new()
    {
        new("show_if", "Mostrar si"),
        new("hide_if", "Ocultar si"),
        new("required_if", "Requerido si"),
        new("optional_if", "Opcional si"),
        new("readonly_if", "Solo lectura si"),
        new("editable_if", "Editable si")
    };
    
    private string GetConditionDescription(CustomFieldCondition condition)
    {
        var field = AvailableFields?.FirstOrDefault(f => f.FieldName == condition.SourceField);
        var fieldName = field?.DisplayName ?? condition.SourceField;
        var operatorText = GetOperatorText(condition.Operator);
        var valueText = GetValueText(condition);
        
        return $"{GetConditionTypeText(condition.Type)} {fieldName} {operatorText} {valueText}";
    }
}
```

### **5. FormPreview - Preview del Formulario Completo**
```razor
<!-- FormPreview.razor -->
<div class="form-preview">
    
    <!-- Simulador de datos -->
    <div class="preview-controls">
        <h5>Datos de Prueba</h5>
        
        <div class="sample-data-controls">
            @foreach (var field in AvailableFields.Where(f => f.EntityName == EntityName))
            {
                <div class="sample-field">
                    <label>@field.DisplayName:</label>
                    @switch (field.FieldType.ToLower())
                    {
                        case "text":
                            <RadzenTextBox @bind-Value="sampleData[field.FieldName]" />
                            break;
                        case "boolean":
                            <RadzenCheckBox @bind-Value="sampleData[field.FieldName]" />
                            break;
                        case "select":
                            <RadzenDropDown @bind-Value="sampleData[field.FieldName]" 
                                           Data="@GetFieldOptions(field)" />
                            break;
                    }
                </div>
            }
        </div>
    </div>
    
    <!-- Preview del formulario -->
    <div class="preview-form">
        <h5>Vista Previa del Formulario</h5>
        
        <div class="form-container">
            <CustomFieldsSection EntityName="@EntityName"
                                CustomFieldDefinitions="@CustomFields"
                                CustomFieldValues="@sampleData"
                                OnValuesChanged="@HandleSampleDataChanged"
                                IsPreviewMode="true" />
        </div>
    </div>
    
    <!-- Información de estado -->
    <div class="preview-state">
        <h5>Estado Actual</h5>
        
        @foreach (var field in CustomFields)
        {
            var state = GetFieldState(field);
            <div class="field-state">
                <strong>@field.DisplayName:</strong>
                @if (state.Visible)
                {
                    <span class="badge badge-success">Visible</span>
                }
                else
                {
                    <span class="badge badge-secondary">Oculto</span>
                }
                
                @if (state.Required)
                {
                    <span class="badge badge-warning">Requerido</span>
                }
                
                @if (state.ReadOnly)
                {
                    <span class="badge badge-info">Solo lectura</span>
                }
            </div>
        }
    </div>
</div>
```

---

## 🎯 Flujo de Usuario - UX Critical

### **1. Onboarding para Nuevos Usuarios**
```razor
<!-- WelcomeWizard.razor -->
@if (isFirstTime)
{
    <div class="welcome-wizard">
        <RadzenSteps>
            <Steps>
                <RadzenStepsItem Text="Bienvenido">
                    <div class="welcome-content">
                        <h3>¡Bienvenido al Diseñador de Campos!</h3>
                        <p>Aquí puedes crear campos personalizados para tus formularios.</p>
                        <RadzenButton Text="Empezar" Click="@StartWizard" />
                    </div>
                </RadzenStepsItem>
                
                <RadzenStepsItem Text="Crear Primer Campo">
                    <div class="tutorial-content">
                        <p>Vamos a crear tu primer campo personalizado paso a paso.</p>
                        <!-- Guía interactiva -->
                    </div>
                </RadzenStepsItem>
                
                <RadzenStepsItem Text="Vista Previa">
                    <div class="tutorial-content">
                        <p>Mira cómo se ve tu campo en el formulario real.</p>
                        <!-- Preview del campo creado -->
                    </div>
                </RadzenStepsItem>
            </Steps>
        </RadzenSteps>
    </div>
}
```

### **2. Validation y Error Handling**
```csharp
// Validaciones en tiempo real
public class FieldValidationService
{
    public ValidationResult ValidateField(CustomFieldDefinition field)
    {
        var errors = new List<string>();
        
        // Validar nombre único
        if (IsFieldNameDuplicate(field.FieldName, field.EntityName))
        {
            errors.Add("Ya existe un campo con este nombre");
        }
        
        // Validar formato de nombre
        if (!IsValidFieldName(field.FieldName))
        {
            errors.Add("El nombre debe contener solo letras, números y guiones bajos");
        }
        
        // Validar configuración específica por tipo
        var typeValidation = ValidateFieldTypeConfig(field);
        errors.AddRange(typeValidation);
        
        // Validar condiciones
        var conditionValidation = ValidateConditions(field);
        errors.AddRange(conditionValidation);
        
        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }
    
    private List<string> ValidateConditions(CustomFieldDefinition field)
    {
        var errors = new List<string>();
        
        if (string.IsNullOrEmpty(field.ConditionsConfig))
            return errors;
            
        var conditions = JsonSerializer.Deserialize<List<CustomFieldCondition>>(field.ConditionsConfig);
        
        foreach (var condition in conditions)
        {
            // Validar campo fuente existe
            if (!FieldExists(condition.SourceField, field.EntityName))
            {
                errors.Add($"Campo '{condition.SourceField}' no existe");
            }
            
            // Validar no hay dependencias circulares
            if (HasCircularDependency(field.FieldName, condition.SourceField))
            {
                errors.Add("Dependencia circular detectada");
            }
        }
        
        return errors;
    }
}
```

### **3. Auto-Save y Recovery**
```csharp
// Auto-guardar configuración
public class AutoSaveService
{
    private Timer _autoSaveTimer;
    private CustomFieldDefinition _currentField;
    
    public void StartAutoSave(CustomFieldDefinition field)
    {
        _currentField = field;
        _autoSaveTimer = new Timer(SaveDraft, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }
    
    private async void SaveDraft(object state)
    {
        if (_currentField != null)
        {
            await SaveFieldDraft(_currentField);
        }
    }
    
    public async Task<CustomFieldDefinition?> RecoverDraft(string entityName)
    {
        // Recuperar borrador guardado automáticamente
        return await LoadFieldDraft(entityName);
    }
}
```

---

## ✅ Criterios de Éxito - Fase 4

### **UX Requirements (CRÍTICOS):**
1. ✅ **Tiempo de aprendizaje**: Usuario nuevo puede crear primer campo en < 5 minutos
2. ✅ **Preview en tiempo real**: Cambios se reflejan inmediatamente
3. ✅ **Error handling**: Mensajes claros y orientación para corregir
4. ✅ **Auto-save**: No se pierde trabajo por errores o navegación
5. ✅ **Performance**: Interface responde en < 100ms a cambios
6. ✅ **Mobile friendly**: Funciona en tablets (responsive design)

### **Funcionalidad Required:**
1. ✅ **CRUD completo**: Crear, editar, eliminar campos
2. ✅ **Todos los tipos**: Soporte para todos los tipos implementados en Fase 2
3. ✅ **Validaciones visuales**: Constructor visual de validaciones
4. ✅ **Condiciones visuales**: Constructor visual de condiciones
5. ✅ **Preview multi-estado**: Ver campo en diferentes estados
6. ✅ **Export/Import**: Copiar configuración entre entidades/organizaciones

### **Testing UX:**
- **Usuario no técnico** puede crear campo completo sin ayuda
- **Power user** puede crear campo complejo en < 2 minutos
- **Error recovery**: Sistema se recupera graciosamente de errores
- **Cross-browser**: Funciona en Chrome, Firefox, Safari, Edge

---

## 🚨 Riesgos Críticos - Fase 4

### **⚠️ Complejidad de UI**
- **Problema**: Interface muy compleja, usuarios no la adoptan
- **Mitigación**: Wizard para casos comunes, modo avanzado opcional

### **⚠️ Performance Issues**
- **Problema**: Preview en tiempo real causa lag
- **Mitigación**: Debouncing, lazy loading, optimizaciones específicas

### **⚠️ User Adoption**
- **Problema**: Administradores prefieren solicitar desarrollo
- **Mitigación**: Templates preconfigurables, casos de uso documentados

### **⚠️ Data Consistency**
- **Problema**: Cambios en diseñador rompen datos existentes
- **Mitigación**: Validaciones de compatibilidad, migraciones automáticas

---

**🎯 Fase 4 es make-or-break. Si la UX no es excepcional, todo el proyecto fallará en adopción. Priorizar simplicidad y feedback inmediato sobre features avanzados.**