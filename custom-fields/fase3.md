# 📋 Fase 3: Permisos y Condiciones Simples

## 🎯 Objetivo
Implementar el sistema de permisos granular por campo personalizado y las condiciones simples (show/hide, required/optional, readonly/editable) que hacen los campos verdaderamente dinámicos.

## ⏱️ Duración Estimada: 1-2 semanas

---

## 🔐 Sistema de Permisos para Campos Personalizados

### **Integración con FieldPermissionAttribute Existente**

El sistema actual ya maneja permisos granulares por campo. Los campos personalizados usarán la misma infraestructura:

```csharp
// Extensión de la tabla custom_field_definitions
ALTER TABLE custom_field_definitions ADD 
    PermissionCreate NVARCHAR(255) NULL,    -- "EMPLEADO.TELEFONO_EMERGENCIA.CREATE"
    PermissionUpdate NVARCHAR(255) NULL,    -- "EMPLEADO.TELEFONO_EMERGENCIA.UPDATE"  
    PermissionView NVARCHAR(255) NULL;      -- "EMPLEADO.TELEFONO_EMERGENCIA.VIEW"
```

### **Nomenclatura de Permisos**
```
ENTIDAD.CAMPO_PERSONALIZADO.ACCION

Ejemplos:
- EMPLEADO.TELEFONO_EMERGENCIA.CREATE
- EMPLEADO.TELEFONO_EMERGENCIA.UPDATE
- EMPLEADO.TELEFONO_EMERGENCIA.VIEW
- EMPRESA.CODIGO_CLIENTE.CREATE
- EMPRESA.CODIGO_CLIENTE.UPDATE
```

### **Generación Automática de Permisos**
```csharp
public class CustomFieldPermissionService
{
    public string GeneratePermissionCode(string entityName, string fieldName, string action)
    {
        // Convertir a UPPER_CASE para consistencia
        var entity = entityName.ToUpperInvariant();
        var field = ConvertToSnakeCase(fieldName).ToUpperInvariant();
        var actionUpper = action.ToUpperInvariant();
        
        return $"{entity}.{field}.{actionUpper}";
    }
    
    public async Task CreatePermissionsForFieldAsync(CustomFieldDefinition field)
    {
        var permissions = new[]
        {
            GeneratePermissionCode(field.EntityName, field.FieldName, "CREATE"),
            GeneratePermissionCode(field.EntityName, field.FieldName, "UPDATE"),
            GeneratePermissionCode(field.EntityName, field.FieldName, "VIEW")
        };
        
        foreach (var permissionCode in permissions)
        {
            await _permissionService.CreatePermissionIfNotExistsAsync(new SystemPermission
            {
                Code = permissionCode,
                Name = GetPermissionName(permissionCode),
                Description = GetPermissionDescription(field, permissionCode),
                OrganizationId = field.OrganizationId
            });
        }
    }
}
```

---

## 🔄 Sistema de Condiciones Simples

### **Estructura de Condiciones**
```csharp
public class CustomFieldCondition
{
    public string Type { get; set; } = null!;        // "show_if", "required_if", "readonly_if"
    public string SourceField { get; set; } = null!; // Campo que dispara la condición
    public string Operator { get; set; } = null!;    // "equals", "not_equals", "in", "not_in", etc.
    public object? Value { get; set; }               // Valor a comparar
    public List<object>? Values { get; set; }        // Para operadores "in"/"not_in"
}

// En CustomFieldDefinition
public class CustomFieldDefinition
{
    // ... campos existentes ...
    public string? ConditionsConfig { get; set; }    // JSON array de condiciones
}
```

### **Tipos de Condiciones Soportadas**

#### **1. Visibilidad (show_if / hide_if)**
```json
{
  "conditionsConfig": [
    {
      "type": "show_if",
      "sourceField": "tipo_contrato",
      "operator": "equals",
      "value": "temporal"
    }
  ]
}
```

#### **2. Requerimiento (required_if / optional_if)**
```json
{
  "conditionsConfig": [
    {
      "type": "required_if",
      "sourceField": "tiene_carga_familiar",
      "operator": "equals",
      "value": true
    }
  ]
}
```

#### **3. Solo Lectura (readonly_if / editable_if)**
```json
{
  "conditionsConfig": [
    {
      "type": "readonly_if",
      "sourceField": "estado_empleado",
      "operator": "in",
      "values": ["inactivo", "suspendido"]
    }
  ]
}
```

### **Operadores Soportados**
```csharp
public enum ConditionOperator
{
    Equals,           // field == value
    NotEquals,        // field != value
    In,               // field in [values]
    NotIn,            // field not in [values]
    GreaterThan,      // field > value (números/fechas)
    LessThan,         // field < value (números/fechas)
    GreaterOrEqual,   // field >= value
    LessOrEqual,      // field <= value
    IsEmpty,          // field == null || field == ""
    IsNotEmpty,       // field != null && field != ""
    Contains,         // field.contains(value) (strings)
    NotContains,      // !field.contains(value)
    StartsWith,       // field.startsWith(value)
    EndsWith          // field.endsWith(value)
}
```

---

## 🎨 Implementación Frontend

### **Evaluador de Condiciones en JavaScript**
```javascript
// CustomFieldConditionEvaluator.js
class CustomFieldConditionEvaluator {
    
    evaluateCondition(condition, formData, customFieldValues) {
        const sourceValue = this.getFieldValue(condition.sourceField, formData, customFieldValues);
        
        switch (condition.operator.toLowerCase()) {
            case 'equals':
                return sourceValue == condition.value;
                
            case 'not_equals':
                return sourceValue != condition.value;
                
            case 'in':
                return condition.values && condition.values.includes(sourceValue);
                
            case 'not_in':
                return condition.values && !condition.values.includes(sourceValue);
                
            case 'greater_than':
                return this.compareValues(sourceValue, condition.value) > 0;
                
            case 'less_than':
                return this.compareValues(sourceValue, condition.value) < 0;
                
            case 'is_empty':
                return !sourceValue || sourceValue === '';
                
            case 'is_not_empty':
                return sourceValue && sourceValue !== '';
                
            case 'contains':
                return sourceValue && sourceValue.toString().includes(condition.value);
                
            default:
                console.warn(`Operador no soportado: ${condition.operator}`);
                return true;
        }
    }
    
    getFieldValue(fieldName, formData, customFieldValues) {
        // Primero buscar en campos nativos
        if (formData.hasOwnProperty(fieldName)) {
            return formData[fieldName];
        }
        
        // Luego buscar en campos personalizados
        if (customFieldValues.hasOwnProperty(fieldName)) {
            return customFieldValues[fieldName];
        }
        
        return null;
    }
    
    evaluateFieldConditions(field, formData, customFieldValues) {
        if (!field.conditionsConfig) {
            return {
                visible: true,
                required: field.isRequired || false,
                readonly: false
            };
        }
        
        const conditions = JSON.parse(field.conditionsConfig);
        let visible = true;
        let required = field.isRequired || false;
        let readonly = false;
        
        for (const condition of conditions) {
            const result = this.evaluateCondition(condition, formData, customFieldValues);
            
            switch (condition.type) {
                case 'show_if':
                    visible = visible && result;
                    break;
                case 'hide_if':
                    visible = visible && !result;
                    break;
                case 'required_if':
                    if (result) required = true;
                    break;
                case 'optional_if':
                    if (result) required = false;
                    break;
                case 'readonly_if':
                    if (result) readonly = true;
                    break;
                case 'editable_if':
                    if (result) readonly = false;
                    break;
            }
        }
        
        return { visible, required, readonly };
    }
}
```

### **Componente CustomFieldsSection Actualizado**
```razor
<!-- CustomFieldsSection.razor con condiciones -->
@foreach (var field in GetVisibleFields())
{
    <div class="custom-field-container" style="@GetFieldContainerStyle(field)">
        @{
            var fieldState = GetFieldState(field);
            var editorProps = new Dictionary<string, object>
            {
                ["Field"] = field,
                ["Value"] = GetFieldValue(field.FieldName),
                ["OnValueChanged"] = EventCallback.Factory.Create<object?>(this, v => SetFieldValue(field.FieldName, v)),
                ["IsRequired"] = fieldState.Required,
                ["IsReadOnly"] = fieldState.ReadOnly || !HasPermission(field, "UPDATE"),
                ["IsVisible"] = fieldState.Visible && HasPermission(field, "VIEW")
            };
        }
        
        @switch (field.FieldType.ToLower())
        {
            case "text":
                <TextFieldEditor @attributes="editorProps" />
                break;
            case "number":
                <NumberEditor @attributes="editorProps" />
                break;
            // ... otros tipos
        }
    </div>
}

@code {
    private CustomFieldConditionEvaluator conditionEvaluator = new();
    private Dictionary<string, FieldState> fieldStates = new();
    
    public class FieldState
    {
        public bool Visible { get; set; } = true;
        public bool Required { get; set; }
        public bool ReadOnly { get; set; }
    }
    
    protected override void OnParametersSet()
    {
        EvaluateAllConditions();
        base.OnParametersSet();
    }
    
    private void EvaluateAllConditions()
    {
        if (CustomFieldDefinitions == null) return;
        
        fieldStates.Clear();
        
        foreach (var field in CustomFieldDefinitions)
        {
            var state = conditionEvaluator.EvaluateFieldConditions(
                field, 
                GetFormData(), 
                CustomFieldValues ?? new Dictionary<string, object>()
            );
            
            fieldStates[field.FieldName] = new FieldState
            {
                Visible = state.Visible,
                Required = state.Required,
                ReadOnly = state.ReadOnly
            };
        }
    }
    
    private List<CustomFieldDefinition> GetVisibleFields()
    {
        return CustomFieldDefinitions?.Where(f => GetFieldState(f).Visible).ToList() ?? new List<CustomFieldDefinition>();
    }
    
    private FieldState GetFieldState(CustomFieldDefinition field)
    {
        return fieldStates.TryGetValue(field.FieldName, out var state) ? state : new FieldState { Required = field.IsRequired };
    }
    
    private bool HasPermission(CustomFieldDefinition field, string action)
    {
        var permissionCode = GeneratePermissionCode(field, action);
        return UserPermissions?.Contains(permissionCode) ?? false;
    }
    
    private string GeneratePermissionCode(CustomFieldDefinition field, string action)
    {
        return $"{field.EntityName.ToUpper()}.{ConvertToSnakeCase(field.FieldName).ToUpper()}.{action}";
    }
}
```

### **Integración con FormValidator**
```csharp
// Servicio para generar reglas dinámicas
public class DynamicCustomFieldValidationService
{
    public ValidationRules GenerateValidationRules(
        List<CustomFieldDefinition> fields, 
        Dictionary<string, object> formData,
        Dictionary<string, object> customValues)
    {
        var rules = new ValidationRules();
        var evaluator = new CustomFieldConditionEvaluator();
        
        foreach (var field in fields)
        {
            var fieldState = evaluator.EvaluateFieldConditions(field, formData, customValues);
            
            // Solo agregar validaciones si el campo es visible
            if (!fieldState.Visible) continue;
            
            var fieldRules = rules.ForField(field.FieldName);
            
            // Required dinámico
            if (fieldState.Required)
            {
                fieldRules.Required($"{field.DisplayName} es requerido");
            }
            
            // Validaciones específicas del tipo si no es readonly
            if (!fieldState.ReadOnly)
            {
                AddTypeSpecificValidations(fieldRules, field);
            }
        }
        
        return rules;
    }
}
```

---

## 🗄️ Extensiones de Base de Datos

### **Tabla de Permisos Personalizados**
```sql
-- Los permisos se crean automáticamente en system_permissions existente
-- No se requieren nuevas tablas, solo lógica para generar códigos de permisos

-- Ejemplo de permisos generados automáticamente:
INSERT INTO system_permissions (Code, Name, Description, OrganizationId) VALUES
('EMPLEADO.TELEFONO_EMERGENCIA.CREATE', 'Crear Teléfono de Emergencia', 'Permite crear el campo personalizado Teléfono de Emergencia', @orgId),
('EMPLEADO.TELEFONO_EMERGENCIA.UPDATE', 'Editar Teléfono de Emergencia', 'Permite editar el campo personalizado Teléfono de Emergencia', @orgId),
('EMPLEADO.TELEFONO_EMERGENCIA.VIEW', 'Ver Teléfono de Emergencia', 'Permite ver el campo personalizado Teléfono de Emergencia', @orgId);
```

### **Índices para Performance**
```sql
-- Índice para búsquedas de condiciones por entidad
CREATE INDEX IX_custom_field_definitions_conditions 
    ON custom_field_definitions(EntityName, OrganizationId) 
    WHERE ConditionsConfig IS NOT NULL;

-- Índice para permisos
CREATE INDEX IX_custom_field_definitions_permissions 
    ON custom_field_definitions(EntityName, PermissionView, PermissionCreate, PermissionUpdate)
    WHERE PermissionView IS NOT NULL OR PermissionCreate IS NOT NULL OR PermissionUpdate IS NOT NULL;
```

---

## 🧪 Testing y Validación

### **Casos de Prueba - Permisos**
- ✅ Usuario sin permiso VIEW no ve el campo
- ✅ Usuario sin permiso CREATE no puede llenar campo en nueva entidad
- ✅ Usuario sin permiso UPDATE ve campo readonly en edición
- ✅ Permisos se evalúan correctamente por organización
- ✅ Super admin ve todos los campos
- ✅ Permisos se respetan en API calls

### **Casos de Prueba - Condiciones**
- ✅ show_if funciona con todos los operadores
- ✅ required_if hace campo obligatorio dinámicamente
- ✅ readonly_if bloquea edición correctamente
- ✅ Condiciones complejas (múltiples condiciones) funcionan
- ✅ Condiciones se evalúan en tiempo real al cambiar valores
- ✅ Condiciones basadas en campos nativos funcionan
- ✅ Condiciones basadas en otros campos personalizados funcionan

### **Casos Edge**
- ✅ Campo con condición que nunca se cumple (siempre oculto)
- ✅ Campo que cambia de visible a oculto y viceversa
- ✅ Campo que cambia de required a optional dinámicamente
- ✅ Condiciones con valores null/undefined
- ✅ Condiciones con arrays vacíos
- ✅ Performance con 20+ campos con condiciones

---

## 🎨 UI/UX Improvements

### **Indicadores Visuales**
```css
/* Campos con condiciones */
.custom-field-container[data-has-conditions="true"] {
    border-left: 3px solid #ffa500;
    padding-left: 12px;
}

/* Campos readonly por condición */
.custom-field-container[data-readonly="true"] {
    opacity: 0.7;
    pointer-events: none;
}

/* Campos requeridos dinámicamente */
.custom-field-container[data-dynamic-required="true"] .field-label::after {
    content: " *";
    color: #ff4444;
    animation: pulse 2s infinite;
}

/* Animaciones de show/hide */
.custom-field-container {
    transition: all 0.3s ease-in-out;
}

.custom-field-container.hidden {
    max-height: 0;
    overflow: hidden;
    opacity: 0;
    margin: 0;
    padding: 0;
}
```

### **Debugging en Desarrollo**
```razor
@if (IsDevelopment)
{
    <div class="debug-panel">
        <h5>Debug - Condiciones de Campos</h5>
        @foreach (var field in CustomFieldDefinitions ?? new List<CustomFieldDefinition>())
        {
            var state = GetFieldState(field);
            <div class="debug-field">
                <strong>@field.FieldName:</strong>
                Visible: @state.Visible, 
                Required: @state.Required, 
                ReadOnly: @state.ReadOnly
                
                @if (!string.IsNullOrEmpty(field.ConditionsConfig))
                {
                    <br><small>Condiciones: @field.ConditionsConfig</small>
                }
            </div>
        }
    </div>
}
```

---

## ✅ Criterios de Éxito - Fase 3

### **Funcionalidad Requerida:**
1. ✅ **Permisos granulares**: CREATE/UPDATE/VIEW por campo personalizado
2. ✅ **Condiciones simples**: show_if, required_if, readonly_if funcionan
3. ✅ **Operadores completos**: Todos los operadores básicos implementados
4. ✅ **Tiempo real**: Condiciones se evalúan al cambiar valores
5. ✅ **Performance**: No hay lag perceptible al evaluar condiciones
6. ✅ **Integración**: Funciona con FormValidator y sistema de permisos existente

### **Pruebas de Aceptación:**
1. **Permisos**: Admin puede configurar permisos específicos por campo
2. **Visibilidad**: Campo se oculta/muestra según condición configurada
3. **Requerimiento**: Campo se vuelve obligatorio según condición
4. **Solo lectura**: Campo se bloquea según estado de otros campos
5. **Performance**: Formulario con 15 campos y condiciones carga en < 400ms
6. **UX**: Transiciones suaves y feedback visual claro

---

## 🚨 Consideraciones Importantes

### **⚠️ Complejidad de Condiciones**
- **Límite**: Una condición por tipo (show_if, required_if, readonly_if)
- **No AND/OR**: Evitar lógica compleja de múltiples condiciones
- **Performance**: Evaluar impacto con muchas condiciones

### **⚠️ Circular Dependencies**
- **Problema**: Campo A depende de Campo B que depende de Campo A
- **Solución**: Validación en configurador para detectar dependencias circulares

### **⚠️ Debugging Complex Conditions**
- **Herramientas**: Panel de debug en desarrollo
- **Logging**: Registrar evaluaciones de condiciones para troubleshooting

---

**🎯 Al completar Fase 3, tendremos campos personalizados verdaderamente dinámicos con permisos granulares y comportamiento condicional, manteniendo la simplicidad y performance.**