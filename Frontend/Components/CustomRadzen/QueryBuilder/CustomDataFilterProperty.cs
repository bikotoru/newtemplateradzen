using Microsoft.AspNetCore.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Frontend.Components.CustomRadzen.QueryBuilder.Models;
using FilterOperator = Frontend.Components.CustomRadzen.QueryBuilder.Models.FilterOperator;
using CompositeFilterDescriptor = Frontend.Components.CustomRadzen.QueryBuilder.Models.CompositeFilterDescriptor;
using PropertyAccess = Frontend.Components.CustomRadzen.QueryBuilder.Models.PropertyAccess;

namespace Frontend.Components.CustomRadzen.QueryBuilder
{
    /// <summary>
    /// CustomDataFilterProperty component.
    /// Must be placed inside a <see cref="CustomDataFilter{TItem}" />
    /// </summary>
    /// <typeparam name="TItem">The type of the DataFilter item.</typeparam>
    public partial class CustomDataFilterProperty<TItem> : ComponentBase, IDisposable
    {
        internal event Action<object> FilterValueChange;

        /// <summary>
        /// Gets or sets the DataFilter.
        /// </summary>
        /// <value>The DataFilter.</value>
        [CascadingParameter]
        public CustomDataFilter<TItem> DataFilter { get; set; }

        /// <summary>
        /// Gets or sets the format string.
        /// </summary>
        /// <value>The format string.</value>
        [Parameter]
        public string FormatString { get; set; }

        internal void RemoveColumn(CustomDataFilterProperty<TItem> property)
        {
            if (DataFilter.properties.Contains(property))
            {
                DataFilter.properties.Remove(property);
                if (!DataFilter.disposed)
                {
                    try { InvokeAsync(StateHasChanged); } catch { }
                }
            }
        }

        /// <summary>
        /// Called when initialized.
        /// </summary>
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

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="CustomDataFilterProperty{TItem}"/> is visible.
        /// </summary>
        /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Visible { get; set; } = true;

        bool? _visible;

        /// <summary>
        /// Gets if the property is visible or not.
        /// </summary>
        /// <returns>System.Boolean.</returns>
        public bool GetVisible()
        {
            return _visible ?? Visible;
        }

        internal void SetVisible(bool? value)
        {
            _visible = value;
        }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        [Parameter]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the property name.
        /// </summary>
        /// <value>The property name.</value>
        [Parameter]
        public string Property { get; set; }

        /// <summary>
        /// Gets or sets the filter property name.
        /// </summary>
        /// <value>The filter property name.</value>
        [Parameter]
        public string FilterProperty { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this property is selected in the filter.
        /// </summary>
        /// <value><c>true</c>, if already selected; otherwise <c>false</c>.</value>
        [Parameter]
        public bool IsSelected { get; set; }

        /// <summary>
        /// Gets or sets the filter value.
        /// </summary>
        /// <value>The filter value.</value>
        [Parameter]
        public object FilterValue { get; set; }

        /// <summary>
        /// Gets or sets the filter template.
        /// </summary>
        /// <value>The filter template.</value>
        [Parameter]
        public RenderFragment<CompositeFilterDescriptor> FilterTemplate { get; set; }

        /// <summary>
        /// Gets or sets the data type.
        /// </summary>
        /// <value>The data type.</value>
        [Parameter]
        public Type Type { get; set; }

        Func<TItem, object> propertyValueGetter;

        internal object GetHeader()
        {
            if (!string.IsNullOrEmpty(Title))
            {
                return Title;
            }
            else
            {
                // Use display name logic to show "Region" instead of "RegionId"
                return PropertyAccess.GetDisplayName(typeof(TItem), Property);
            }
        }

        /// <summary>
        /// Gets the filter property.
        /// </summary>
        /// <returns>System.String.</returns>
        public string GetFilterProperty()
        {
            return Property;
        }

        Type _filterPropertyType;

        /// <summary>
        /// Gets the filter property type.
        /// </summary>
        public Type FilterPropertyType
        {
            get
            {
                return _filterPropertyType;
            }
        }

        object filterValue;
        FilterOperator? filterOperator;

        /// <summary>
        /// Set parameters as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
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
                        if (FilterValueChange != null)
                        {
                            FilterValueChange(filterValue);
                        }

                        await DataFilter.Filter();

                        return;
                    }
                }
            }

            await base.SetParametersAsync(parameters);
        }

        /// <summary>
        /// Get property filter value.
        /// </summary>
        public object GetFilterValue()
        {
            return filterValue ?? FilterValue;
        }

        /// <summary>
        /// Get property filter operator.
        /// </summary>
        public FilterOperator GetFilterOperator()
        {
            return filterOperator ?? FilterOperator;
        }

        /// <summary>
        /// Set property filter value.
        /// </summary>
        public void SetFilterValue(object value)
        {
            if ((FilterPropertyType == typeof(DateTimeOffset) || FilterPropertyType == typeof(DateTimeOffset?)) && value != null && value is DateTime?)
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
        /// Sets to default property filter values and operators.
        /// </summary>
        public void ClearFilters()
        {
            SetFilterValue(null);
            SetFilterOperator(null);

            FilterValue = null;
            var defaultOperator = typeof(System.Collections.IEnumerable).IsAssignableFrom(FilterPropertyType) ? FilterOperator.Contains : default(FilterOperator);
            FilterOperator = GetFilterOperators().Contains(defaultOperator) ? defaultOperator : GetFilterOperators().FirstOrDefault();
        }

        /// <summary>
        /// Gets or sets the filter operator.
        /// </summary>
        /// <value>The filter operator.</value>
        [Parameter]
        public FilterOperator FilterOperator { get; set; }

        IEnumerable<FilterOperator> _filterOperators;
        /// <summary>
        /// Gets or sets the filter operators.
        /// </summary>
        /// <value>The filter operators.</value>
        [Parameter]
        public IEnumerable<FilterOperator> FilterOperators
        {
            get
            {
                return _filterOperators;
            }
            set
            {
                _filterOperators = value;
            }
        }

        /// <summary>
        /// Set property filter operator.
        /// </summary>
        public void SetFilterOperator(FilterOperator? value)
        {
            if (value == FilterOperator.IsEmpty || value == FilterOperator.IsNotEmpty || value == FilterOperator.IsNull || value == FilterOperator.IsNotNull)
            {
                filterValue = value == FilterOperator.IsEmpty || value == FilterOperator.IsNotEmpty ? string.Empty : null;
            }

            filterOperator = value;
        }

        /// <summary>
        /// Get possible property filter operators based on field type.
        /// </summary>
        public virtual IEnumerable<FilterOperator> GetFilterOperators()
        {
            if (FilterOperators != null) return FilterOperators;

            // 🆕 Operadores específicos para campos Guid de relación (RegionId, PaisId, etc.)
            if (PropertyAccess.IsGuidRelation(typeof(TItem), Property))
            {
                return new FilterOperator[] {
                    FilterOperator.Equals,
                    FilterOperator.NotEquals,
                    FilterOperator.IsNull,
                    FilterOperator.IsNotNull,
                    FilterOperator.Related
                };
            }

            // 🆕 Operadores específicos para campos Guid que NO son relación (Id puro)
            if (IsGuidField())
            {
                return new FilterOperator[] {
                    FilterOperator.Equals,
                    FilterOperator.NotEquals,
                    FilterOperator.IsNull,
                    FilterOperator.IsNotNull
                };
            }

            // 🆕 Operadores específicos para entidades/clases complejas (Region, Pais, etc.)
            if (IsComplexEntityField())
            {
                return new FilterOperator[] {
                    FilterOperator.Equals,
                    FilterOperator.NotEquals,
                    FilterOperator.IsNull,
                    FilterOperator.IsNotNull,
                    FilterOperator.Related
                };
            }

            // Operadores para Enums
            if (PropertyAccess.IsEnum(FilterPropertyType))
            {
                return new FilterOperator[] {
                    FilterOperator.Equals,
                    FilterOperator.NotEquals
                };
            }

            // Operadores para Enums nullable
            if (PropertyAccess.IsNullableEnum(FilterPropertyType))
            {
                return new FilterOperator[] {
                    FilterOperator.Equals,
                    FilterOperator.NotEquals,
                    FilterOperator.IsNull,
                    FilterOperator.IsNotNull
                };
            }

            // Operadores para tipos numéricos
            if (PropertyAccess.IsNumeric(FilterPropertyType))
            {
                return new FilterOperator[] {
                    FilterOperator.Equals,
                    FilterOperator.NotEquals,
                    FilterOperator.LessThan,
                    FilterOperator.LessThanOrEquals,
                    FilterOperator.GreaterThan,
                    FilterOperator.GreaterThanOrEquals,
                    FilterOperator.IsNull,
                    FilterOperator.IsNotNull
                };
            }

            // Operadores para fechas
            if (PropertyAccess.IsDate(FilterPropertyType))
            {
                return new FilterOperator[] {
                    FilterOperator.Equals,
                    FilterOperator.NotEquals,
                    FilterOperator.LessThan,
                    FilterOperator.LessThanOrEquals,
                    FilterOperator.GreaterThan,
                    FilterOperator.GreaterThanOrEquals,
                    FilterOperator.IsNull,
                    FilterOperator.IsNotNull
                };
            }

            // Operadores para booleanos
            if (FilterPropertyType == typeof(bool) || FilterPropertyType == typeof(bool?))
            {
                return new FilterOperator[] {
                    FilterOperator.Equals,
                    FilterOperator.NotEquals,
                    FilterOperator.IsNull,
                    FilterOperator.IsNotNull
                };
            }

            // Operadores para strings
            if (FilterPropertyType == typeof(string))
            {
                return new FilterOperator[] {
                    FilterOperator.Equals,
                    FilterOperator.NotEquals,
                    FilterOperator.Contains,
                    FilterOperator.DoesNotContain,
                    FilterOperator.StartsWith,
                    FilterOperator.EndsWith,
                    FilterOperator.IsNull,
                    FilterOperator.IsNotNull,
                    FilterOperator.IsEmpty,
                    FilterOperator.IsNotEmpty
                };
            }

            // Operadores para colecciones
            if ((typeof(IEnumerable).IsAssignableFrom(FilterPropertyType) || typeof(IEnumerable<>).IsAssignableFrom(FilterPropertyType))
                && FilterPropertyType != typeof(string))
            {
                return new FilterOperator[] {
                    FilterOperator.Contains,
                    FilterOperator.DoesNotContain,
                    FilterOperator.Equals,
                    FilterOperator.NotEquals,
                    FilterOperator.IsNull,
                    FilterOperator.IsNotNull,
                    FilterOperator.IsEmpty,
                    FilterOperator.IsNotEmpty
                };
            }

            // Default para otros tipos (sin Custom)
            return new FilterOperator[] {
                FilterOperator.Equals,
                FilterOperator.NotEquals,
                FilterOperator.IsNull,
                FilterOperator.IsNotNull
            };
        }

        /// <summary>
        /// Determina si el campo es un Guid que NO es una relación
        /// </summary>
        private bool IsGuidField()
        {
            var underlyingType = Nullable.GetUnderlyingType(FilterPropertyType) ?? FilterPropertyType;
            return underlyingType == typeof(Guid) && !PropertyAccess.IsGuidRelation(typeof(TItem), Property);
        }

        /// <summary>
        /// Determina si el campo es una entidad compleja/clase
        /// </summary>
        private bool IsComplexEntityField()
        {
            return FilterPropertyType.IsClass &&
                   FilterPropertyType != typeof(string) &&
                   !FilterPropertyType.IsArray &&
                   !FilterPropertyType.IsPrimitive;
        }

        internal string GetFilterOperatorText(FilterOperator filterOperator)
        {
            switch (filterOperator)
            {
                case FilterOperator.Related:
                    return DataFilter?.RelatedText ?? "Relacionado";
                case FilterOperator.Contains:
                    return DataFilter?.ContainsText;
                case FilterOperator.DoesNotContain:
                    return DataFilter?.DoesNotContainText;
                case FilterOperator.In:
                    return DataFilter?.InText;
                case FilterOperator.NotIn:
                    return DataFilter?.NotInText;
                case FilterOperator.EndsWith:
                    return DataFilter?.EndsWithText;
                case FilterOperator.Equals:
                    return DataFilter?.EqualsText;
                case FilterOperator.GreaterThan:
                    return DataFilter?.GreaterThanText;
                case FilterOperator.GreaterThanOrEquals:
                    return DataFilter?.GreaterThanOrEqualsText;
                case FilterOperator.LessThan:
                    return DataFilter?.LessThanText;
                case FilterOperator.LessThanOrEquals:
                    return DataFilter?.LessThanOrEqualsText;
                case FilterOperator.StartsWith:
                    return DataFilter?.StartsWithText;
                case FilterOperator.NotEquals:
                    return DataFilter?.NotEqualsText;
                case FilterOperator.IsNull:
                    return DataFilter?.IsNullText;
                case FilterOperator.IsEmpty:
                    return DataFilter?.IsEmptyText;
                case FilterOperator.IsNotNull:
                    return DataFilter?.IsNotNullText;
                case FilterOperator.IsNotEmpty:
                    return DataFilter?.IsNotEmptyText;
                default:
                    return $"{filterOperator}";
            }
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            DataFilter?.RemoveProperty(this);
        }
    }
}