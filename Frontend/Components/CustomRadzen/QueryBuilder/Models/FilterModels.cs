using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace Frontend.Components.CustomRadzen.QueryBuilder.Models
{
    /// <summary>
    /// Representa un filtro compuesto que puede contener otros filtros o ser un filtro simple
    /// Compatible con CompositeFilterDescriptor de Radzen
    /// </summary>
    public class CustomCompositeFilterDescriptor
    {
        /// <summary>
        /// Nombre de la propiedad filtrada
        /// </summary>
        public string Property { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de la propiedad
        /// </summary>
        [JsonIgnore]
        public Type? Type { get; set; }

        /// <summary>
        /// Nombre de la propiedad de filtro (puede ser diferente de Property para navegación)
        /// </summary>
        public string FilterProperty { get; set; } = string.Empty;

        /// <summary>
        /// Valor por el cual filtrar
        /// </summary>
        public object? FilterValue { get; set; }

        /// <summary>
        /// Operador que comparará el valor de la propiedad con FilterValue
        /// </summary>
        public FilterOperator? FilterOperator { get; set; }

        /// <summary>
        /// Operador lógico utilizado para combinar el resultado de filtrado
        /// </summary>
        public LogicalFilterOperator LogicalFilterOperator { get; set; } = LogicalFilterOperator.And;

        /// <summary>
        /// Lista de filtros anidados (para grupos de filtros)
        /// </summary>
        public IEnumerable<CustomCompositeFilterDescriptor>? Filters { get; set; }

        /// <summary>
        /// Convierte este filtro custom a CompositeFilterDescriptor de Radzen
        /// </summary>
        public CompositeFilterDescriptor ToRadzenFilter()
        {
            var radzenFilter = new CompositeFilterDescriptor
            {
                Property = this.Property,
                Type = this.Type,
                FilterProperty = this.FilterProperty,
                FilterValue = this.FilterValue,
                FilterOperator = this.FilterOperator,
                LogicalFilterOperator = this.LogicalFilterOperator,
                Filters = this.Filters?.Select(f => f.ToRadzenFilter())
            };

            return radzenFilter;
        }

        /// <summary>
        /// Crea un CustomCompositeFilterDescriptor desde un CompositeFilterDescriptor de Radzen
        /// </summary>
        public static CustomCompositeFilterDescriptor FromRadzenFilter(CompositeFilterDescriptor radzenFilter)
        {
            return new CustomCompositeFilterDescriptor
            {
                Property = radzenFilter.Property ?? string.Empty,
                Type = radzenFilter.Type,
                FilterProperty = radzenFilter.FilterProperty ?? string.Empty,
                FilterValue = radzenFilter.FilterValue,
                FilterOperator = radzenFilter.FilterOperator,
                LogicalFilterOperator = radzenFilter.LogicalFilterOperator,
                Filters = radzenFilter.Filters?.Select(FromRadzenFilter)
            };
        }
    }

    /// <summary>
    /// Configuración personalizable para el CustomDataFilter
    /// </summary>
    public class CustomDataFilterConfiguration
    {
        /// <summary>
        /// Permite filtros únicos (solo un filtro por propiedad)
        /// </summary>
        public bool UniqueFilters { get; set; } = false;

        /// <summary>
        /// Permite filtrado automático al cambiar filtros
        /// </summary>
        public bool Auto { get; set; } = true;

        /// <summary>
        /// Sensibilidad de mayúsculas y minúsculas
        /// </summary>
        public FilterCaseSensitivity FilterCaseSensitivity { get; set; } = FilterCaseSensitivity.CaseInsensitive;

        /// <summary>
        /// Permite filtrado de columnas
        /// </summary>
        public bool AllowColumnFiltering { get; set; } = false;

        /// <summary>
        /// Formato de fecha para filtros
        /// </summary>
        public string FilterDateFormat { get; set; } = string.Empty;

        /// <summary>
        /// Textos personalizables
        /// </summary>
        public CustomDataFilterTexts Texts { get; set; } = new();

        /// <summary>
        /// Estilos CSS personalizables
        /// </summary>
        public CustomDataFilterStyles Styles { get; set; } = new();
    }

    /// <summary>
    /// Textos personalizables para el CustomDataFilter
    /// </summary>
    public class CustomDataFilterTexts
    {
        public string FilterText { get; set; } = "Filter";
        public string EnumFilterSelectText { get; set; } = "Select...";
        public string AndOperatorText { get; set; } = "And";
        public string OrOperatorText { get; set; } = "Or";
        public string ApplyFilterText { get; set; } = "Apply";
        public string ClearFilterText { get; set; } = "Clear all";
        public string AddFilterText { get; set; } = "Add filter";
        public string RemoveFilterText { get; set; } = "Remove filter";
        public string AddFilterGroupText { get; set; } = "Add filter group";
        public string EqualsText { get; set; } = "Equals";
        public string NotEqualsText { get; set; } = "Not equals";
        public string LessThanText { get; set; } = "Less than";
        public string LessThanOrEqualsText { get; set; } = "Less than or equals";
        public string GreaterThanText { get; set; } = "Greater than";
        public string GreaterThanOrEqualsText { get; set; } = "Greater than or equals";
        public string EndsWithText { get; set; } = "Ends with";
        public string ContainsText { get; set; } = "Contains";
        public string DoesNotContainText { get; set; } = "Does not contain";
        public string InText { get; set; } = "In";
        public string NotInText { get; set; } = "Not in";
        public string StartsWithText { get; set; } = "Starts with";
        public string IsNotNullText { get; set; } = "Is not null";
        public string IsNullText { get; set; } = "Is null";
        public string IsEmptyText { get; set; } = "Is empty";
        public string IsNotEmptyText { get; set; } = "Is not empty";
        public string CustomText { get; set; } = "Custom";
    }

    /// <summary>
    /// Estilos CSS personalizables para el CustomDataFilter
    /// </summary>
    public class CustomDataFilterStyles
    {
        public string ContainerClass { get; set; } = "custom-datafilter";
        public string OperatorBarClass { get; set; } = "custom-datafilter-operator-bar";
        public string ClearButtonClass { get; set; } = "custom-datafilter-clear";
        public string GroupClass { get; set; } = "custom-datafilter-group";
        public string ItemClass { get; set; } = "custom-datafilter-item";
        public string PropertyDropdownClass { get; set; } = "custom-datafilter-property";
        public string OperatorDropdownClass { get; set; } = "custom-datafilter-operator";
        public string EditorClass { get; set; } = "custom-datafilter-editor";
        public string RemoveButtonClass { get; set; } = "custom-datafilter-remove";
        public string AddButtonClass { get; set; } = "custom-datafilter-add";
    }

    /// <summary>
    /// Eventos personalizados para el CustomDataFilter
    /// </summary>
    public class CustomDataFilterEvents
    {
        public EventCallback<CustomCompositeFilterDescriptor> OnFilterAdded { get; set; }
        public EventCallback<CustomCompositeFilterDescriptor> OnFilterRemoved { get; set; }
        public EventCallback<CustomCompositeFilterDescriptor> OnFilterChanged { get; set; }
        public EventCallback OnFiltersCleared { get; set; }
        public EventCallback<IEnumerable<CustomCompositeFilterDescriptor>> OnFiltersChanged { get; set; }
    }

    /// <summary>
    /// Template personalizable para editores de filtros
    /// </summary>
    public class CustomFilterTemplate
    {
        public Type PropertyType { get; set; } = typeof(object);
        public RenderFragment<CustomCompositeFilterDescriptor>? Template { get; set; }
        public Func<object?, object?>? ValueConverter { get; set; }
        public IEnumerable<FilterOperator>? SupportedOperators { get; set; }
    }
}