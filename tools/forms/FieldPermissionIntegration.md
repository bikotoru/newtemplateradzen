# 🔒 Integración de FieldPermission en Formularios

## 📋 Consideraciones para Generadores de Formularios

Al generar formularios automáticamente, es importante considerar los campos protegidos por `FieldPermission`.

## 🎯 Detección de Campos Protegidos

### **En Tiempo de Generación**
```python
# Detectar campos con FieldPermission en metadata
def has_field_permission(entity_metadata, field_name):
    """
    Verifica si un campo tiene atributo FieldPermission
    """
    # Buscar en archivos .Metadata.cs
    metadata_file = f"{entity_name}.Metadata.cs"
    
    # Parsear y buscar [FieldPermission(...)]
    # Retornar True si encontrado
    
def get_field_permissions(entity_metadata, field_name):
    """
    Obtiene los permisos específicos (CREATE/UPDATE/VIEW)
    """
    # Extraer: CREATE = "...", UPDATE = "...", VIEW = "..."
    # Retornar dict con permisos
```

### **En Formularios Frontend**
```javascript
// En Blazor/JavaScript
const fieldPermissions = {
    'SueldoBase': {
        CREATE: 'EMPLEADO.SUELDOBASE.CREATE',
        UPDATE: 'EMPLEADO.SUELDOBASE.EDIT',
        VIEW: 'EMPLEADO.SUELDOBASE.VIEW'
    }
};
```

## 🎨 Generación Inteligente de Formularios

### **Formularios de Creación**
```csharp
@if (HasPermission("EMPLEADO.SUELDOBASE.CREATE"))
{
    <InputNumber @bind-Value="model.SueldoBase" DisplayName="Sueldo Base" />
}
else
{
    <!-- Campo oculto o deshabilitado -->
}
```

### **Formularios de Edición**
```csharp
@if (HasPermission("EMPLEADO.SUELDOBASE.UPDATE"))
{
    <InputNumber @bind-Value="model.SueldoBase" DisplayName="Sueldo Base" />
}
else if (HasPermission("EMPLEADO.SUELDOBASE.VIEW"))
{
    <InputNumber @bind-Value="model.SueldoBase" DisplayName="Sueldo Base" readonly />
}
else
{
    <!-- Campo completamente oculto -->
}
```

### **Formularios de Solo Lectura/Lista**
```csharp
@if (HasPermission("EMPLEADO.SUELDOBASE.VIEW"))
{
    <DisplayField Value="@model.SueldoBase" Label="Sueldo Base" />
}
else
{
    <DisplayField Value="***" Label="Sueldo Base" Title="Sin permisos de visualización" />
}
```

## 🛠️ Templates Recomendados

### **Template para Campo Protegido**
```html
<!-- Template: ProtectedField.razor -->
@if (CanView)
{
    @if (IsEditable && CanEdit)
    {
        <!-- Campo editable -->
        @ChildContent
    }
    else if (CanView)
    {
        <!-- Campo solo lectura -->
        @ReadOnlyContent
    }
}
else
{
    <!-- Campo oculto -->
    <div class="field-hidden" title="Campo protegido">
        <span class="text-muted">***</span>
    </div>
}

@code {
    [Parameter] public string FieldName { get; set; }
    [Parameter] public string EntityName { get; set; }
    [Parameter] public bool IsEditable { get; set; } = false;
    [Parameter] public RenderFragment ChildContent { get; set; }
    [Parameter] public RenderFragment ReadOnlyContent { get; set; }
    
    private bool CanView => HasPermission($"{EntityName}.{FieldName}.VIEW");
    private bool CanEdit => HasPermission($"{EntityName}.{FieldName}.{(IsEditable ? "UPDATE" : "CREATE")}");
}
```

### **Uso del Template**
```html
<ProtectedField FieldName="SUELDOBASE" EntityName="EMPLEADO" IsEditable="@IsEditMode">
    <ChildContent>
        <InputNumber @bind-Value="model.SueldoBase" DisplayName="Sueldo Base" />
    </ChildContent>
    <ReadOnlyContent>
        <DisplayField Value="@model.SueldoBase" Label="Sueldo Base" />
    </ReadOnlyContent>
</ProtectedField>
```

## 🎭 Ejemplos por Tipo de Campo

### **Campos Monetarios**
```csharp
[FieldPermission(VIEW = "EMPLEADO.SUELDOBASE.VIEW", UPDATE = "EMPLEADO.SUELDOBASE.EDIT")]
public decimal? SueldoBase { get; set; }

// Frontend
@if (HasPermission("EMPLEADO.SUELDOBASE.VIEW"))
{
    <CurrencyInput @bind-Value="model.SueldoBase" 
                   ReadOnly="@(!HasPermission("EMPLEADO.SUELDOBASE.EDIT"))" />
}
```

### **Campos de Texto Sensibles**
```csharp
[FieldPermission(VIEW = "EMPLEADO.DOCUMENTO.VIEW")]
public string NumeroDocumento { get; set; }

// Frontend - Solo visualización
@if (HasPermission("EMPLEADO.DOCUMENTO.VIEW"))
{
    <MaskedInput Value="@model.NumeroDocumento" Mask="**-***-***" />
}
else
{
    <span class="text-muted">***-***-***</span>
}
```

### **Campos de Selección**
```csharp
[FieldPermission(CREATE = "EMPLEADO.CATEGORIA.CREATE", UPDATE = "EMPLEADO.CATEGORIA.EDIT")]
public string CategoriaId { get; set; }

// Frontend
@if (IsCreateMode && HasPermission("EMPLEADO.CATEGORIA.CREATE") || 
     IsEditMode && HasPermission("EMPLEADO.CATEGORIA.EDIT"))
{
    <SelectInput @bind-Value="model.CategoriaId" 
                 Options="@categorias" 
                 DisplayField="Nombre" />
}
else
{
    <DisplayField Value="@GetCategoriaName(model.CategoriaId)" Label="Categoría" />
}
```

## 📊 Indicadores Visuales

### **CSS para Campos Protegidos**
```css
.field-protected {
    position: relative;
}

.field-protected::after {
    content: "🔒";
    position: absolute;
    right: 5px;
    top: 50%;
    transform: translateY(-50%);
    font-size: 0.8em;
    color: #6c757d;
}

.field-hidden {
    background-color: #f8f9fa;
    border: 1px dashed #dee2e6;
    padding: 8px;
    border-radius: 4px;
    text-align: center;
    color: #6c757d;
}

.field-readonly {
    background-color: #e9ecef;
    border-color: #ced4da;
}
```

## 🔧 Integración con Entity Generator

### **Modificaciones Sugeridas**
```python
# En entity-generator.py
def generate_field_html(field_info):
    if has_field_permission(field_info):
        permissions = get_field_permissions(field_info)
        return generate_protected_field_html(field_info, permissions)
    else:
        return generate_normal_field_html(field_info)

def generate_protected_field_html(field_info, permissions):
    return f"""
<ProtectedField FieldName="{field_info.name.upper()}" 
                EntityName="{field_info.entity.upper()}" 
                IsEditable="@IsEditMode">
    <ChildContent>
        {generate_input_component(field_info)}
    </ChildContent>
    <ReadOnlyContent>
        {generate_display_component(field_info)}
    </ReadOnlyContent>
</ProtectedField>
    """
```

## 🎯 Mejores Prácticas

1. **Generar formularios adaptativos** que se ajusten automáticamente a permisos
2. **Proporcionar feedback visual** cuando un campo está protegido
3. **Mantener UX consistente** entre campos protegidos y normales
4. **Documentar dependencias** de permisos en formularios generados
5. **Probar todos los estados** (sin permisos, solo view, full access)

## 🚨 Consideraciones Importantes

- **Backend siempre valida**: Frontend es solo UX, backend es seguridad
- **Campos ocultos**: No incluir en HTML si no tiene permiso VIEW
- **Estados intermedios**: Manejar loading/error states apropiadamente
- **Responsive**: Asegurar que formularios funcionen en mobile

---

**💡 Recuerda**: El sistema de permisos funciona automáticamente en backend. Frontend es solo para mejorar UX.