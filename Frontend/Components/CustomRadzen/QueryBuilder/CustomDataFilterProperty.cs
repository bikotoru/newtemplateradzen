using Microsoft.AspNetCore.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Radzen;
using Frontend.Components.CustomRadzen.QueryBuilder.Models;

namespace Frontend.Components.CustomRadzen.QueryBuilder
{
    /// <summary>
    /// CustomDataFilterProperty component - propiedad personalizable para CustomDataFilter
    /// Debe ser colocado dentro de un CustomDataFilter
    /// </summary>
    /// <typeparam name="TItem">El tipo del item del DataFilter</typeparam>
    public partial class CustomDataFilterProperty<TItem> : ComponentBase, IDisposable
    {
        internal event Action<object>? FilterValueChange;

        /// <summary>
        /// DataFilter que contiene esta propiedad
        /// </summary>
        [CascadingParameter]
        public CustomDataFilter<TItem> DataFilter { get; set; } = null!;

        /// <summary>
        /// Cadena de formato
        /// </summary>
        [Parameter]
        public string FormatString { get; set; } = string.Empty;

        /// <summary>
        /// Visibilidad de la propiedad
        /// </summary>
        [Parameter]
        public bool Visible { get; set; } = true;

        bool? _visible;

        /// <summary>
        /// Obtiene si la propiedad es visible o no
        /// </summary>
        public bool GetVisible()
        {
            return _visible ?? Visible;
        }

        internal void SetVisible(bool? value)
        {
            _visible = value;
        }

        /// <summary>
        /// Título de la propiedad
        /// </summary>
        [Parameter]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Nombre de la propiedad
        /// </summary>
        [Parameter]
        public string Property { get; set; } = string.Empty;

        /// <summary>
        /// Nombre de la propiedad de filtro
        /// </summary>
        [Parameter]
        public string FilterProperty { get; set; } = string.Empty;

        /// <summary>
        /// Indica si esta propiedad está seleccionada en el filtro
        /// </summary>
        [Parameter]
        public bool IsSelected { get; set; }

        /// <summary>
        /// Valor del filtro
        /// </summary>
        [Parameter]
        public object? FilterValue { get; set; }

        /// <summary>
        /// Template personalizado para el filtro
        /// </summary>
        [Parameter]
        public RenderFragment<CustomCompositeFilterDescriptor>? FilterTemplate { get; set; }

        /// <summary>
        /// Tipo de datos
        /// </summary>
        [Parameter]
        public Type? Type { get; set; }

        /// <summary>
        /// Operador de filtro
        /// </summary>
        [Parameter]
        public FilterOperator FilterOperator { get; set; }

        /// <summary>
        /// Operadores de filtro disponibles
        /// </summary>
        [Parameter]
        public IEnumerable<FilterOperator>? FilterOperators { get; set; }

        Func<TItem, object>? propertyValueGetter;
        Type? _filterPropertyType;
        object? filterValue;
        FilterOperator? filterOperator;

        /// <summary>
        /// Tipo de la propiedad de filtro
        /// </summary>
        public Type? FilterPropertyType => _filterPropertyType;

        protected override void OnInitialized()
        {
            if (DataFilter != null)
            {
                DataFilter.AddProperty(this);

                var property = GetFilterProperty();

                if (!string.IsNullOrEmpty(property) && Type == null)
                {
                    if (!string.IsNullOrEmpty(property))
                    {
                        _filterPropertyType = PropertyAccess.GetPropertyType(typeof(TItem), property);
                    }
                }

                if (_filterPropertyType == null)
                {
                    _filterPropertyType = Type;
                }
                else
                {
                    propertyValueGetter = PropertyAccess.Getter<TItem, object>(Property);
                }

                if (_filterPropertyType == typeof(string))
                {
                    FilterOperator = FilterOperator.Contains;
                }
            }
        }

        internal object GetHeader()
        {
            if (!string.IsNullOrEmpty(Title))
            {
                return Title;
            }
            else
            {
                return Property;
            }
        }

        /// <summary>
        /// Obtiene la propiedad de filtro
        /// </summary>
        public string GetFilterProperty()
        {
            return Property;
        }

        /// <summary>
        /// Establecer parámetros de forma asíncrona
        /// </summary>
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.DidParameterChange(nameof(FilterValue), FilterValue))
            {
                var value = parameters.GetValueOrDefault<object>(nameof(FilterValue));

                if (filterValue != value)
                {
                    filterValue = value;

                    if (FilterTemplate != null)
                    {
                        FilterValueChange?.Invoke(filterValue!);
                        await DataFilter.Filter();
                        return;
                    }
                }
            }

            await base.SetParametersAsync(parameters);
        }

        /// <summary>
        /// Obtener valor del filtro de la propiedad
        /// </summary>
        public object? GetFilterValue()
        {
            return filterValue ?? FilterValue;
        }

        /// <summary>
        /// Obtener operador del filtro de la propiedad
        /// </summary>
        public FilterOperator GetFilterOperator()
        {
            return filterOperator ?? FilterOperator;
        }

        /// <summary>
        /// Establecer valor del filtro de la propiedad
        /// </summary>
        public void SetFilterValue(object? value)
        {
            if ((FilterPropertyType == typeof(DateTimeOffset) || FilterPropertyType == typeof(DateTimeOffset?)) &&
                value != null && value is DateTime?)
            {
                DateTimeOffset? offset = DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc);
                value = offset;
            }

            filterValue = value;
        }

        internal bool CanSetFilterValue()
        {
            return GetFilterOperator() == FilterOperator.IsNull
                   || GetFilterOperator() == FilterOperator.IsNotNull
                   || GetFilterOperator() == FilterOperator.IsEmpty
                   || GetFilterOperator() == FilterOperator.IsNotEmpty;
        }

        /// <summary>
        /// Establecer valores y operadores por defecto del filtro de la propiedad
        /// </summary>
        public void ClearFilters()
        {
            SetFilterValue(null);
            SetFilterOperator(null);

            FilterValue = null;
            var defaultOperator = typeof(System.Collections.IEnumerable).IsAssignableFrom(FilterPropertyType)
                ? FilterOperator.Contains
                : default(FilterOperator);
            FilterOperator = GetFilterOperators().Contains(defaultOperator)
                ? defaultOperator
                : GetFilterOperators().FirstOrDefault();
        }

        /// <summary>
        /// Establecer operador del filtro de la propiedad
        /// </summary>
        public void SetFilterOperator(FilterOperator? value)
        {
            if (value == FilterOperator.IsEmpty || value == FilterOperator.IsNotEmpty ||
                value == FilterOperator.IsNull || value == FilterOperator.IsNotNull)
            {
                filterValue = value == FilterOperator.IsEmpty || value == FilterOperator.IsNotEmpty
                    ? string.Empty
                    : null;
            }

            filterOperator = value;
        }

        /// <summary>
        /// Obtener operadores de filtro posibles para la propiedad
        /// </summary>
        public virtual IEnumerable<FilterOperator> GetFilterOperators()
        {
            if (FilterOperators != null) return FilterOperators;

            if (FilterPropertyType == null) return Enum.GetValues(typeof(FilterOperator)).Cast<FilterOperator>();

            if (PropertyAccess.IsEnum(FilterPropertyType))
                return new FilterOperator[] { FilterOperator.Equals, FilterOperator.NotEquals };

            if (PropertyAccess.IsNullableEnum(FilterPropertyType))
                return new FilterOperator[] { FilterOperator.Equals, FilterOperator.NotEquals, FilterOperator.IsNull, FilterOperator.IsNotNull };

            if ((typeof(IEnumerable).IsAssignableFrom(FilterPropertyType) ||
                 typeof(IEnumerable<>).IsAssignableFrom(FilterPropertyType)) &&
                FilterPropertyType != typeof(string))
            {
                var operators = new FilterOperator[]
                {
                    FilterOperator.Contains,
                    FilterOperator.DoesNotContain,
                    FilterOperator.Equals,
                    FilterOperator.NotEquals,
                    FilterOperator.IsNull,
                    FilterOperator.IsNotNull,
                    FilterOperator.IsEmpty,
                    FilterOperator.IsNotEmpty
                };

                if (!string.IsNullOrEmpty(Property))
                {
                    var type = PropertyAccess.GetPropertyType(typeof(TItem), Property);
                    if ((typeof(IEnumerable).IsAssignableFrom(type) ||
                         typeof(IEnumerable<>).IsAssignableFrom(type)) &&
                        type != typeof(string))
                    {
                        operators = operators.Concat(new FilterOperator[] { FilterOperator.In, FilterOperator.NotIn }).ToArray();
                    }
                }

                return operators;
            }

            return Enum.GetValues(typeof(FilterOperator)).Cast<FilterOperator>()
                .Where(o => o != FilterOperator.In && o != FilterOperator.NotIn)
                .Where(o => {
                    var isStringOperator = o == FilterOperator.Contains || o == FilterOperator.DoesNotContain
                                          || o == FilterOperator.StartsWith || o == FilterOperator.EndsWith
                                          || o == FilterOperator.IsEmpty || o == FilterOperator.IsNotEmpty;
                    return FilterPropertyType == typeof(string)
                        ? isStringOperator || o == FilterOperator.Equals || o == FilterOperator.NotEquals
                          || o == FilterOperator.IsNull || o == FilterOperator.IsNotNull
                        : !isStringOperator;
                });
        }

        internal string GetFilterOperatorText(FilterOperator filterOperator)
        {
            return filterOperator switch
            {
                FilterOperator.Custom => DataFilter?.Configuration.Texts.CustomText ?? "Custom",
                FilterOperator.Contains => DataFilter?.Configuration.Texts.ContainsText ?? "Contains",
                FilterOperator.DoesNotContain => DataFilter?.Configuration.Texts.DoesNotContainText ?? "Does not contain",
                FilterOperator.In => DataFilter?.Configuration.Texts.InText ?? "In",
                FilterOperator.NotIn => DataFilter?.Configuration.Texts.NotInText ?? "Not in",
                FilterOperator.EndsWith => DataFilter?.Configuration.Texts.EndsWithText ?? "Ends with",
                FilterOperator.Equals => DataFilter?.Configuration.Texts.EqualsText ?? "Equals",
                FilterOperator.GreaterThan => DataFilter?.Configuration.Texts.GreaterThanText ?? "Greater than",
                FilterOperator.GreaterThanOrEquals => DataFilter?.Configuration.Texts.GreaterThanOrEqualsText ?? "Greater than or equals",
                FilterOperator.LessThan => DataFilter?.Configuration.Texts.LessThanText ?? "Less than",
                FilterOperator.LessThanOrEquals => DataFilter?.Configuration.Texts.LessThanOrEqualsText ?? "Less than or equals",
                FilterOperator.StartsWith => DataFilter?.Configuration.Texts.StartsWithText ?? "Starts with",
                FilterOperator.NotEquals => DataFilter?.Configuration.Texts.NotEqualsText ?? "Not equals",
                FilterOperator.IsNull => DataFilter?.Configuration.Texts.IsNullText ?? "Is null",
                FilterOperator.IsEmpty => DataFilter?.Configuration.Texts.IsEmptyText ?? "Is empty",
                FilterOperator.IsNotNull => DataFilter?.Configuration.Texts.IsNotNullText ?? "Is not null",
                FilterOperator.IsNotEmpty => DataFilter?.Configuration.Texts.IsNotEmptyText ?? "Is not empty",
                _ => $"{filterOperator}"
            };
        }

        internal string GetFilterOperatorSymbol(FilterOperator filterOperator)
        {
            var symbol = DataFilter.Configuration.FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ? "a" : "A";
            return filterOperator switch
            {
                FilterOperator.Contains => $"*{symbol}*",
                FilterOperator.DoesNotContain => $"*{symbol}*",
                FilterOperator.StartsWith => $"{symbol}**",
                FilterOperator.EndsWith => $"**{symbol}",
                FilterOperator.Equals => "=",
                FilterOperator.GreaterThan => ">",
                FilterOperator.GreaterThanOrEquals => "≥",
                FilterOperator.LessThan => "<",
                FilterOperator.LessThanOrEquals => "≤",
                FilterOperator.NotEquals => "≠",
                FilterOperator.IsNull => "∅",
                FilterOperator.IsNotNull => "!∅",
                FilterOperator.IsEmpty => "= ''",
                FilterOperator.IsNotEmpty => "≠ ''",
                _ => $"{filterOperator}"
            };
        }

        /// <summary>
        /// Dispose del componente
        /// </summary>
        public void Dispose()
        {
            DataFilter?.RemoveProperty(this);
        }
    }
}