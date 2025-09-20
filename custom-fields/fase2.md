# 📋 Fase 2: Tipos Básicos y Validaciones

## 🎯 Objetivo
Implementar todos los tipos de campos básicos (text, number, date, boolean, select) y el sistema completo de validaciones integrado con FormValidationRulesBuilder existente.

## ⏱️ Duración Estimada: 1 semana

---

## 🔧 Tipos de Campo a Implementar

### **1. TextArea (text-area)**
```json
{
  "fieldType": "textarea",
  "validationConfig": {
    "minLength": 10,
    "maxLength": 500,
    "required": true
  },
  "uiConfig": {
    "rows": 4,
    "placeholder": "Escriba su comentario aquí..."
  }
}
```

### **2. Number (number)**
```json
{
  "fieldType": "number",
  "validationConfig": {
    "min": 0,
    "max": 999999,
    "step": 0.01,
    "required": true
  },
  "uiConfig": {
    "placeholder": "0.00",
    "prefix": "$",
    "suffix": "CLP"
  }
}
```

### **3. Date (date)**
```json
{
  "fieldType": "date",
  "validationConfig": {
    "minDate": "2020-01-01",
    "maxDate": "2030-12-31",
    "required": true
  },
  "uiConfig": {
    "format": "dd/MM/yyyy",
    "showCalendar": true
  }
}
```

### **4. Boolean (boolean)**
```json
{
  "fieldType": "boolean",
  "validationConfig": {
    "required": false
  },
  "uiConfig": {
    "style": "checkbox", // "checkbox", "switch", "radio"
    "trueLabel": "Sí",
    "falseLabel": "No"
  }
}
```

### **5. Select (select)**
```json
{
  "fieldType": "select",
  "validationConfig": {
    "required": true,
    "allowEmpty": false
  },
  "uiConfig": {
    "options": [
      {"value": "basico", "label": "Básico"},
      {"value": "intermedio", "label": "Intermedio"},
      {"value": "avanzado", "label": "Avanzado"}
    ],
    "placeholder": "Seleccione una opción..."
  }
}
```

### **6. MultiSelect (multiselect)**
```json
{
  "fieldType": "multiselect",
  "validationConfig": {
    "minSelections": 1,
    "maxSelections": 3,
    "required": true
  },
  "uiConfig": {
    "options": [
      {"value": "excel", "label": "Microsoft Excel"},
      {"value": "word", "label": "Microsoft Word"},
      {"value": "powerpoint", "label": "PowerPoint"}
    ],
    "placeholder": "Seleccione habilidades...",
    "showSelectAll": true
  }
}
```

---

## 🔍 Sistema de Validaciones

### **Extensión de ValidationConfig**
```csharp
public class CustomFieldValidationConfig
{
    // Validaciones comunes
    public bool Required { get; set; }
    public string? RequiredMessage { get; set; }
    
    // Text/TextArea
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public string? Pattern { get; set; } // Regex
    public string? PatternMessage { get; set; }
    
    // Number
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public decimal? Step { get; set; }
    
    // Date
    public DateTime? MinDate { get; set; }
    public DateTime? MaxDate { get; set; }
    
    // Select/MultiSelect
    public bool AllowEmpty { get; set; } = true;
    public int? MinSelections { get; set; }
    public int? MaxSelections { get; set; }
    
    // Validaciones personalizadas
    public List<CustomValidationRule>? CustomRules { get; set; }
}

public class CustomValidationRule
{
    public string RuleType { get; set; } = null!; // "email", "phone", "rut", etc.
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? Parameters { get; set; }
}
```

### **Integración con FormValidationRulesBuilder**
```csharp
// Servicio para generar reglas de validación
public class CustomFieldValidationService
{
    public ValidationRules GenerateValidationRules(
        List<CustomFieldDefinition> fields, 
        Dictionary<string, object> values)
    {
        var rules = new ValidationRules();
        
        foreach (var field in fields)
        {
            var config = JsonSerializer.Deserialize<CustomFieldValidationConfig>(
                field.ValidationConfig ?? "{}");
            
            var fieldRules = rules.ForField(field.FieldName);
            
            // Required
            if (config.Required)
            {
                fieldRules.Required(config.RequiredMessage ?? $"{field.DisplayName} es requerido");
            }
            
            // Validaciones específicas por tipo
            switch (field.FieldType.ToLower())
            {
                case "text":
                case "textarea":
                    AddTextValidations(fieldRules, config);
                    break;
                    
                case "number":
                    AddNumberValidations(fieldRules, config);
                    break;
                    
                case "date":
                    AddDateValidations(fieldRules, config);
                    break;
                    
                case "select":
                case "multiselect":
                    AddSelectValidations(fieldRules, config);
                    break;
            }
            
            // Validaciones personalizadas
            AddCustomValidations(fieldRules, config.CustomRules);
        }
        
        return rules;
    }
}
```

---

## 🎨 Componentes de UI

### **1. TextAreaEditor.razor**
```razor
<ValidatedInput FieldName="@Field.FieldName" Value="@Value?.ToString()">
    <RadzenFormField Text="@Field.DisplayName" Style="width: 100%">
        <RadzenTextArea Value="@(Value?.ToString())" 
                       Rows="@GetRows()"
                       MaxLength="@GetMaxLength()"
                       Placeholder="@GetPlaceholder()" 
                       @oninput="@(args => HandleValueChange(args.Value?.ToString()))" />
    </RadzenFormField>
</ValidatedInput>

@code {
    [Parameter] public CustomFieldDefinition Field { get; set; } = null!;
    [Parameter] public object? Value { get; set; }
    [Parameter] public EventCallback<object?> OnValueChanged { get; set; }
    
    private int GetRows()
    {
        var uiConfig = GetUIConfig();
        return uiConfig.TryGetValue("rows", out var rows) && int.TryParse(rows.ToString(), out var r) ? r : 3;
    }
    
    private int? GetMaxLength()
    {
        var validationConfig = GetValidationConfig();
        return validationConfig.TryGetValue("maxLength", out var max) && int.TryParse(max.ToString(), out var m) ? m : null;
    }
}
```

### **2. NumberEditor.razor**
```razor
<ValidatedInput FieldName="@Field.FieldName" Value="@Value?.ToString()">
    <RadzenFormField Text="@Field.DisplayName" Style="width: 100%">
        <RadzenNumeric TValue="decimal?" 
                      Value="@GetDecimalValue()" 
                      Min="@GetMinValue()"
                      Max="@GetMaxValue()"
                      Step="@GetStep()"
                      Placeholder="@GetPlaceholder()"
                      @oninput="@(args => HandleValueChange(args.Value))" />
    </RadzenFormField>
</ValidatedInput>

@code {
    [Parameter] public CustomFieldDefinition Field { get; set; } = null!;
    [Parameter] public object? Value { get; set; }
    [Parameter] public EventCallback<object?> OnValueChanged { get; set; }
    
    private decimal? GetDecimalValue()
    {
        if (Value == null) return null;
        return decimal.TryParse(Value.ToString(), out var result) ? result : null;
    }
    
    private decimal? GetMinValue()
    {
        var config = GetValidationConfig();
        return config.TryGetValue("min", out var min) && decimal.TryParse(min.ToString(), out var m) ? m : null;
    }
}
```

### **3. DateEditor.razor**
```razor
<ValidatedInput FieldName="@Field.FieldName" Value="@Value?.ToString()">
    <RadzenFormField Text="@Field.DisplayName" Style="width: 100%">
        <RadzenDatePicker TValue="DateTime?" 
                         Value="@GetDateValue()" 
                         Min="@GetMinDate()"
                         Max="@GetMaxDate()"
                         DateFormat="@GetDateFormat()"
                         Placeholder="@GetPlaceholder()"
                         @oninput="@(args => HandleValueChange(args.Value))" />
    </RadzenFormField>
</ValidatedInput>

@code {
    private DateTime? GetDateValue()
    {
        if (Value == null) return null;
        return DateTime.TryParse(Value.ToString(), out var result) ? result : null;
    }
    
    private DateTime? GetMinDate()
    {
        var config = GetValidationConfig();
        return config.TryGetValue("minDate", out var min) && DateTime.TryParse(min.ToString(), out var d) ? d : null;
    }
}
```

### **4. BooleanEditor.razor**
```razor
<ValidatedInput FieldName="@Field.FieldName" Value="@Value?.ToString()">
    <RadzenFormField Text="@Field.DisplayName" Style="width: 100%">
        @{
            var style = GetUIStyle();
        }
        
        @if (style == "checkbox")
        {
            <RadzenCheckBox TValue="bool?" 
                           Value="@GetBoolValue()" 
                           Text="@GetCheckboxText()"
                           @oninput="@(args => HandleValueChange(args.Value))" />
        }
        else if (style == "switch")
        {
            <RadzenSwitch TValue="bool?" 
                         Value="@GetBoolValue()" 
                         @oninput="@(args => HandleValueChange(args.Value))" />
        }
        else // radio
        {
            <RadzenRadioButtonList TValue="bool?" 
                                  Value="@GetBoolValue()" 
                                  Data="@GetRadioOptions()"
                                  TextProperty="Label"
                                  ValueProperty="Value"
                                  @oninput="@(args => HandleValueChange(args.Value))" />
        }
    </RadzenFormField>
</ValidatedInput>
```

### **5. SelectEditor.razor**
```razor
<ValidatedInput FieldName="@Field.FieldName" Value="@Value?.ToString()">
    <RadzenFormField Text="@Field.DisplayName" Style="width: 100%">
        <RadzenDropDown TValue="string" 
                       Value="@(Value?.ToString())" 
                       Data="@GetOptions()"
                       TextProperty="Label"
                       ValueProperty="Value"
                       AllowClear="@GetAllowClear()"
                       Placeholder="@GetPlaceholder()"
                       @oninput="@(args => HandleValueChange(args.Value))" />
    </RadzenFormField>
</ValidatedInput>

@code {
    private List<SelectOption> GetOptions()
    {
        var uiConfig = GetUIConfig();
        if (uiConfig.TryGetValue("options", out var optionsObj))
        {
            var optionsJson = JsonSerializer.Serialize(optionsObj);
            return JsonSerializer.Deserialize<List<SelectOption>>(optionsJson) ?? new List<SelectOption>();
        }
        return new List<SelectOption>();
    }
}

public class SelectOption
{
    public string Value { get; set; } = null!;
    public string Label { get; set; } = null!;
}
```

### **6. MultiSelectEditor.razor**
```razor
<ValidatedInput FieldName="@Field.FieldName" Value="@GetSelectedValuesString()">
    <RadzenFormField Text="@Field.DisplayName" Style="width: 100%">
        <RadzenListBox TValue="IEnumerable<string>" 
                      Value="@GetSelectedValues()" 
                      Data="@GetOptions()"
                      TextProperty="Label"
                      ValueProperty="Value"
                      Multiple="true"
                      AllowSelectAll="@GetShowSelectAll()"
                      Placeholder="@GetPlaceholder()"
                      @oninput="@(args => HandleValueChange(args.Value))" />
    </RadzenFormField>
</ValidatedInput>

@code {
    private IEnumerable<string>? GetSelectedValues()
    {
        if (Value is string stringValue && !string.IsNullOrEmpty(stringValue))
        {
            return JsonSerializer.Deserialize<List<string>>(stringValue);
        }
        return new List<string>();
    }
    
    private string GetSelectedValuesString()
    {
        var selected = GetSelectedValues();
        return JsonSerializer.Serialize(selected ?? new List<string>());
    }
}
```

---

## 🔄 Actualización del CustomFieldsSection

```razor
<!-- CustomFieldsSection.razor actualizado -->
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
                
            case "textarea":
                <TextAreaEditor Field="@field" 
                              Value="@GetFieldValue(field.FieldName)" 
                              OnValueChanged="@(v => SetFieldValue(field.FieldName, v))" />
                break;
                
            case "number":
                <NumberEditor Field="@field" 
                            Value="@GetFieldValue(field.FieldName)" 
                            OnValueChanged="@(v => SetFieldValue(field.FieldName, v))" />
                break;
                
            case "date":
                <DateEditor Field="@field" 
                          Value="@GetFieldValue(field.FieldName)" 
                          OnValueChanged="@(v => SetFieldValue(field.FieldName, v))" />
                break;
                
            case "boolean":
                <BooleanEditor Field="@field" 
                             Value="@GetFieldValue(field.FieldName)" 
                             OnValueChanged="@(v => SetFieldValue(field.FieldName, v))" />
                break;
                
            case "select":
                <SelectEditor Field="@field" 
                            Value="@GetFieldValue(field.FieldName)" 
                            OnValueChanged="@(v => SetFieldValue(field.FieldName, v))" />
                break;
                
            case "multiselect":
                <MultiSelectEditor Field="@field" 
                                 Value="@GetFieldValue(field.FieldName)" 
                                 OnValueChanged="@(v => SetFieldValue(field.FieldName, v))" />
                break;
                
            default:
                <div class="alert alert-warning">
                    <p>⚠️ Tipo de campo no soportado: <strong>@field.FieldType</strong></p>
                    <p>Tipos soportados: text, textarea, number, date, boolean, select, multiselect</p>
                </div>
                break;
        }
    </div>
}
```

---

## 🧪 Testing y Validación

### **Casos de Prueba por Tipo de Campo**

#### **Text/TextArea**
- ✅ MinLength/MaxLength funcionan
- ✅ Pattern regex funciona
- ✅ Required/Optional funciona
- ✅ Placeholder se muestra
- ✅ Caracteres especiales se guardan correctamente

#### **Number**
- ✅ Min/Max values funcionan
- ✅ Step increment funciona
- ✅ Decimales se manejan correctamente
- ✅ Valores negativos (si se permiten)
- ✅ Formato de display (prefix/suffix)

#### **Date**
- ✅ MinDate/MaxDate funcionan
- ✅ Formato de fecha se respeta
- ✅ Timezone handling
- ✅ Invalid dates se rechazan

#### **Boolean**
- ✅ Todos los estilos (checkbox, switch, radio) funcionan
- ✅ True/False labels se muestran
- ✅ Null values se manejan
- ✅ Default values funcionan

#### **Select/MultiSelect**
- ✅ Options se cargan correctamente
- ✅ Placeholder funciona
- ✅ AllowClear funciona
- ✅ MinSelections/MaxSelections (multiselect)
- ✅ SelectAll funciona (multiselect)

---

## ✅ Criterios de Éxito - Fase 2

### **Funcionalidad Requerida:**
1. ✅ **Todos los tipos básicos**: Los 6 tipos de campo funcionan correctamente
2. ✅ **Validaciones completas**: Todas las validaciones definidas funcionan
3. ✅ **UI consistente**: Todos los campos siguen el mismo patrón visual
4. ✅ **Performance**: No hay degradación notable con múltiples campos
5. ✅ **Serialización**: Todos los tipos se serializan/deserializan correctamente
6. ✅ **Validación en tiempo real**: FormValidator funciona con campos personalizados

### **Pruebas de Aceptación:**
1. **Crear todos los tipos**: Admin puede crear cada tipo de campo sin errores
2. **Validaciones funcionan**: Cada tipo respeta sus validaciones específicas
3. **UI responsive**: Campos se ven bien en móvil y desktop
4. **Performance**: Formulario carga en < 300ms con 10 campos personalizados
5. **Datos complejos**: MultiSelect con arrays se maneja correctamente
6. **Integración**: ValidatedInput funciona igual que campos nativos

---

## 🚨 Riesgos y Mitigaciones

### **⚠️ Serialización JSON Compleja**
- **Riesgo**: Arrays y objetos complejos pueden causar errores
- **Mitigación**: Validación estricta de tipos antes de serializar

### **⚠️ Performance con Muchos Campos**
- **Riesgo**: Formularios lentos con 20+ campos personalizados
- **Mitigación**: Lazy loading y virtualización si es necesario

### **⚠️ Validaciones Conflictivas**
- **Riesgo**: Validaciones custom interfieren con sistema existente
- **Mitigación**: Namespace separado para validaciones custom

### **⚠️ UI Inconsistencies**
- **Riesgo**: Campos personalizados se ven diferentes a nativos
- **Mitigación**: Uso estricto de componentes Radzen existentes

---

**🎯 Al completar Fase 2, tendremos un sistema completo de campos personalizados con todos los tipos básicos y validaciones robustas.**