# CustomDataFilter - Versión Personalizable de RadzenDataFilter

## Descripción

Este conjunto de componentes proporciona una versión completamente personalizable del RadzenDataFilter de Radzen Blazor, permitiendo control total sobre la apariencia y funcionalidad mientras mantiene la compatibilidad completa con el código existente.

## Componentes Incluidos

### 1. CustomDataFilter&lt;TItem&gt;
Componente principal que reemplaza RadzenDataFilter con funcionalidades adicionales.

### 2. CustomDataFilterItem&lt;TItem&gt;
Representa elementos individuales del filtro (tanto filtros simples como grupos).

### 3. CustomDataFilterProperty&lt;TItem&gt;
Define las propiedades que pueden ser filtradas.

### 4. FilterModels.cs
Contiene todas las clases de modelo y configuración necesarias.

## Uso Básico

### Reemplazo Drop-in

```razor
@using Frontend.Components.CustomRadzen.QueryBuilder

<!-- En lugar de RadzenDataFilter -->
<CustomDataFilter @ref="customDataFilterRef"
                 TItem="object"
                 LogicalFilterOperator="@LogicalFilterOperator.And"
                 Auto="false"
                 ContainsText="Contiene"
                 StartsWithText="Comienza con"
                 EqualsText="Igual a">
    <Properties>
        @foreach (var field in EntityFields)
        {
            <CustomDataFilterProperty Property="@field.PropertyName"
                                    Title="@field.DisplayName"
                                    Type="@field.PropertyType" />
        }
    </Properties>
</CustomDataFilter>
```

### Configuración Avanzada

```razor
<CustomDataFilter @ref="customDataFilterRef"
                 TItem="MyEntity"
                 Configuration="@customConfig"
                 Events="@customEvents"
                 ShowDebugInfo="true"
                 CustomTemplates="@customTemplates">
    <Properties>
        <!-- Propiedades aquí -->
    </Properties>
</CustomDataFilter>
```

```csharp
// Configuración personalizada
private CustomDataFilterConfiguration customConfig = new()
{
    Auto = true,
    UniqueFilters = false,
    FilterCaseSensitivity = FilterCaseSensitivity.CaseInsensitive,
    Texts = new CustomDataFilterTexts
    {
        AndOperatorText = "Y",
        OrOperatorText = "O",
        AddFilterText = "Agregar filtro personalizado"
    },
    Styles = new CustomDataFilterStyles
    {
        ContainerClass = "mi-filtro-personalizado",
        EditorClass = "mi-editor-personalizado"
    }
};

// Eventos personalizados
private CustomDataFilterEvents customEvents = new()
{
    OnFilterAdded = EventCallback.Factory.Create<CustomCompositeFilterDescriptor>(this, OnFilterAdded),
    OnFilterRemoved = EventCallback.Factory.Create<CustomCompositeFilterDescriptor>(this, OnFilterRemoved)
};

private async Task OnFilterAdded(CustomCompositeFilterDescriptor filter)
{
    Console.WriteLine($"Filtro agregado: {filter.Property}");
}
```

## Personalización

### 1. Estilos CSS Personalizados

```csharp
var customStyles = new CustomDataFilterStyles
{
    ContainerClass = "my-custom-datafilter",
    OperatorBarClass = "my-operator-bar",
    GroupClass = "my-filter-group",
    ItemClass = "my-filter-item",
    PropertyDropdownClass = "my-property-selector",
    OperatorDropdownClass = "my-operator-selector",
    EditorClass = "my-value-editor",
    RemoveButtonClass = "my-remove-button",
    AddButtonClass = "my-add-button",
    ClearButtonClass = "my-clear-button"
};
```

### 2. Templates Personalizados

```csharp
// Registrar template personalizado para fechas
customDataFilter.RegisterCustomTemplate(typeof(DateTime), new CustomFilterTemplate
{
    PropertyType = typeof(DateTime),
    Template = filter => @<div>
        <RadzenDatePicker @bind-Value="filter.FilterValue"
                         ShowTime="true"
                         DateFormat="dd/MM/yyyy HH:mm" />
    </div>,
    SupportedOperators = new[] { FilterOperator.Equals, FilterOperator.GreaterThan, FilterOperator.LessThan }
});
```

### 3. Textos Completamente Personalizables

```csharp
var customTexts = new CustomDataFilterTexts
{
    FilterText = "Filtrar datos",
    AndOperatorText = "Y",
    OrOperatorText = "O",
    EqualsText = "Es igual a",
    ContainsText = "Contiene texto",
    AddFilterText = "➕ Nuevo filtro",
    AddFilterGroupText = "📁 Nuevo grupo",
    RemoveFilterText = "🗑️ Eliminar",
    ClearFilterText = "🧹 Limpiar todo"
};
```

## Compatibilidad

### Con RadzenDataFilter Existente

El CustomDataFilter es completamente compatible con el código existente:

```csharp
// Funciona con ambos
public RadzenDataFilter<object>? DataFilter => dataFilterRef;
public CustomDataFilter<object>? CustomDataFilter => customDataFilterRef;

// Los métodos helper soportan ambos
private bool HasDataFilter()
{
    return filterConfigurationRef?.DataFilter != null ||
           filterConfigurationRef?.CustomDataFilter != null;
}
```

### Serialización Compatible

```csharp
// Los filtros se pueden convertir entre formatos
var radzenFilter = customFilter.ToRadzenFilter();
var customFilter = CustomCompositeFilterDescriptor.FromRadzenFilter(radzenFilter);
```

## Eventos Disponibles

```csharp
public class CustomDataFilterEvents
{
    public EventCallback<CustomCompositeFilterDescriptor> OnFilterAdded { get; set; }
    public EventCallback<CustomCompositeFilterDescriptor> OnFilterRemoved { get; set; }
    public EventCallback<CustomCompositeFilterDescriptor> OnFilterChanged { get; set; }
    public EventCallback OnFiltersCleared { get; set; }
    public EventCallback<IEnumerable<CustomCompositeFilterDescriptor>> OnFiltersChanged { get; set; }
}
```

## Debug y Desarrollo

### Modo Debug

```razor
<CustomDataFilter ShowDebugInfo="true" ... />
```

Esto mostrará información útil como:
- Número de filtros activos
- Operador lógico actual
- Estado del modo automático
- Número de propiedades disponibles
- Información detallada por cada filtro

### Logs de Consola

El componente genera logs detallados en la consola del navegador para facilitar el debugging:

```
🎯 FilterConfiguration: CustomDataFilter is ready
✓ Added filter to CustomDataFilter: Name Equals John
Current filter count: 1
```

## Mejoras sobre RadzenDataFilter

1. **Control Total**: Código fuente completo en tu proyecto
2. **Personalización Completa**: Estilos, textos, comportamientos
3. **Templates Personalizados**: Editores específicos por tipo
4. **Eventos Extendidos**: Más hooks para personalización
5. **Debug Mejorado**: Información detallada del estado
6. **Compatibilidad**: Drop-in replacement del original
7. **Extensibilidad**: Fácil agregar nuevas funcionalidades

## Estructura de Archivos

```
Frontend/Components/CustomRadzen/QueryBuilder/
├── CustomDataFilter.razor              # Componente principal UI
├── CustomDataFilter.razor.cs           # Lógica del componente principal
├── CustomDataFilterItem.razor          # UI de elementos individuales
├── CustomDataFilterItem.razor.cs       # Lógica de elementos individuales
├── CustomDataFilterProperty.cs         # Manejo de propiedades
├── Models/
│   └── FilterModels.cs                  # Modelos y configuraciones
└── README.md                           # Esta documentación
```

## Próximos Pasos

Con esta implementación tienes una base sólida para:

1. **Personalizaciones Visuales**: Cambiar completamente el look & feel
2. **Nuevos Tipos de Editores**: Agregar editores especializados
3. **Validaciones Personalizadas**: Implementar validaciones específicas
4. **Integración con APIs**: Conectar con servicios externos
5. **Mejoras de UX**: Agregar tooltips, ayuda contextual, etc.

El componente está diseñado para crecer con tus necesidades sin perder funcionalidad.