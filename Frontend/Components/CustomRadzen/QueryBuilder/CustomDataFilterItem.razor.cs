using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Radzen;
using Frontend.Components.CustomRadzen.QueryBuilder.Models;

namespace Frontend.Components.CustomRadzen.QueryBuilder
{
    /// <summary>
    /// CustomDataFilterItem - representa un elemento individual del filtro
    /// </summary>
    /// <typeparam name="TItem">Tipo del item del filtro</typeparam>
    public partial class CustomDataFilterItem<TItem> : ComponentBase
    {
        [Parameter]
        public CustomDataFilter<TItem> DataFilter { get; set; } = null!;

        [Parameter]
        public CustomDataFilterItem<TItem>? Parent { get; set; }

        [Parameter]
        public CustomDataFilterConfiguration Configuration { get; set; } = new();

        CustomCompositeFilterDescriptor _filter = new();
        [Parameter]
        public CustomCompositeFilterDescriptor Filter
        {
            get => _filter;
            set
            {
                _filter = value;

                if (property == null && Filter.Filters == null)
                {
                    if (!string.IsNullOrEmpty(Filter.Property))
                    {
                        property = DataFilter?.properties.FirstOrDefault(f => object.Equals(f.Property, Filter.Property));
                    }
                    else if (property == null && DataFilter?.Configuration.UniqueFilters == true)
                    {
                        property = DataFilter?.properties.FirstOrDefault(f => f.IsSelected == false);
                    }
                    else
                    {
                        property = DataFilter?.properties.FirstOrDefault();
                    }

                    if (property != null)
                    {
                        property.FilterValueChange -= OnFilterValueChange;
                        property.FilterValueChange += OnFilterValueChange;

                        if (DataFilter?.Configuration.UniqueFilters == true)
                        {
                            property.IsSelected = true;
                        }

                        Filter.Property = property.Property;
                        Filter.FilterProperty = property.FilterProperty;
                        Filter.Type = property.FilterPropertyType;

                        if (Filter.FilterOperator == null)
                        {
                            Filter.FilterOperator = property.GetFilterOperator();
                        }
                        else if (!property.GetFilterOperators().Contains(Filter.FilterOperator.Value))
                        {
                            Filter.FilterOperator = property.GetFilterOperators().FirstOrDefault();
                        }

                        var v = property.GetFilterValue();
                        if (v != null)
                        {
                            Filter.FilterValue = v;
                        }
                    }
                }
            }
        }

        void OnFilterValueChange(object value)
        {
            if (property != null)
            {
                Filter.FilterValue = property.GetFilterValue();
            }
        }

        CustomDataFilterProperty<TItem>? property;

        async Task ApplyFilter()
        {
            if (DataFilter.Configuration.Auto)
            {
                await DataFilter.Filter();
            }
        }

        async Task OnPropertyChange(object p)
        {
            if (property != null)
            {
                property.FilterValueChange -= OnFilterValueChange;
                property.IsSelected = false;
            }

            property = DataFilter.properties.Where(c => object.Equals(c.Property, p)).FirstOrDefault();

            if (property != null)
            {
                property.FilterValueChange += OnFilterValueChange;
                if (DataFilter?.Configuration.UniqueFilters == true)
                {
                    property.IsSelected = true;
                }
            }

            Filter.FilterValue = null;

            var defaultOperator = typeof(System.Collections.IEnumerable).IsAssignableFrom(property?.FilterPropertyType)
                ? FilterOperator.Contains
                : default(FilterOperator);

            if (property != null)
            {
                if (property.GetFilterOperators().Any(o => o == property.FilterOperator))
                {
                    Filter.FilterOperator = property.FilterOperator;
                }
                else if (property.GetFilterOperators().Contains(defaultOperator))
                {
                    Filter.FilterOperator = defaultOperator;
                }
                else
                {
                    Filter.FilterOperator = property.GetFilterOperators().FirstOrDefault();
                }
            }

            await ApplyFilter();
        }

        bool IsOperatorNullOrEmpty()
        {
            if (Filter != null)
            {
                return Filter.FilterOperator == FilterOperator.IsEmpty ||
                       Filter.FilterOperator == FilterOperator.IsNotEmpty ||
                       Filter.FilterOperator == FilterOperator.IsNull ||
                       Filter.FilterOperator == FilterOperator.IsNotNull;
            }

            return false;
        }

        async Task OnOperatorChange(object p)
        {
            Console.WriteLine($"🔄 OnOperatorChange: Old={Filter.FilterOperator}, New={p}, Property={Filter.Property}");

            var newOperator = (FilterOperator)p;
            Filter.FilterOperator = newOperator;

            // Solo limpiar el valor para operadores que no requieren valor
            if (IsOperatorNullOrEmpty())
            {
                Console.WriteLine($"🧹 Clearing FilterValue for operator {newOperator}");
                Filter.FilterValue = null;
            }
            else
            {
                Console.WriteLine($"✅ Keeping FilterValue for operator {newOperator}, current value: {Filter.FilterValue}");
            }

            // Forzar actualización del estado sin disparar el Auto filter
            await ChangeState();

            // Solo aplicar filtro si Auto está habilitado
            if (DataFilter.Configuration.Auto)
            {
                await ApplyFilter();
            }
        }

        async Task AddFilter(bool isGroup)
        {
            if (DataFilter?.Configuration.UniqueFilters == true && DataFilter.properties.All(f => f.IsSelected))
            {
                return;
            }

            var newFilter = new CustomCompositeFilterDescriptor();

            if (isGroup)
            {
                newFilter.Filters = new List<CustomCompositeFilterDescriptor>();
                if (Filter.Filters == null)
                    Filter.Filters = new List<CustomCompositeFilterDescriptor>();

                Filter.Filters = Filter.Filters.Concat(new[] { newFilter });
            }
            else
            {
                if (Filter.Filters == null)
                    Filter.Filters = new List<CustomCompositeFilterDescriptor>();

                Filter.Filters = Filter.Filters.Concat(new[] { newFilter });
            }

            if (DataFilter.Configuration.Auto)
            {
                await DataFilter.Filter();
            }
        }

        async Task RemoveFilter()
        {
            Console.WriteLine($"🗑️ RemoveFilter called for Property={Filter.Property}, Operator={Filter.FilterOperator}");

            if (property != null)
            {
                property.IsSelected = false;
            }
            property = null;

            if (Parent != null)
            {
                Console.WriteLine($"📁 Removing from parent group");
                Parent.Filter.Filters = Parent.Filter.Filters?.Where(f => f != Filter).ToList();
                await Parent.ChangeState();
            }
            else
            {
                Console.WriteLine($"🏠 Removing from main DataFilter");
                // Convertir el filtro custom a Radzen para hacer la comparación correcta
                var radzenFilter = Filter.ToRadzenFilter();
                DataFilter.Filters = DataFilter.Filters.Where(f => f != radzenFilter).ToList();
                await DataFilter.ChangeState();
            }

            await ApplyFilter();
        }

        internal async Task ChangeState()
        {
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Obtiene el template personalizado para el tipo de propiedad actual
        /// </summary>
        CustomFilterTemplate? GetCustomTemplate()
        {
            if (property?.FilterPropertyType != null)
            {
                return DataFilter?.GetCustomTemplate(property.FilterPropertyType);
            }
            return null;
        }

        RenderFragment DrawNumericFilter()
        {
            return new RenderFragment(builder =>
            {
                if (property?.FilterPropertyType == null) return;

                var type = Nullable.GetUnderlyingType(property.FilterPropertyType) != null ?
                    property.FilterPropertyType : typeof(Nullable<>).MakeGenericType(property.FilterPropertyType);

                var numericType = typeof(RadzenNumeric<>).MakeGenericType(type);

                builder.OpenComponent(0, numericType);

                builder.AddAttribute(1, "Value", Filter.FilterValue);
                builder.AddAttribute(2, "class", $"{Configuration.Styles.EditorClass} rz-datafilter-editor");
                builder.AddAttribute(3, "Disabled", IsOperatorNullOrEmpty());

                Action<object> action = args =>
                {
                    Filter.FilterValue = args;
                    InvokeAsync(ApplyFilter);
                };

                var eventCallbackGenericCreate = typeof(NumericFilterEventCallback).GetMethod("Create")?.MakeGenericMethod(type);
                var eventCallbackGenericAction = typeof(NumericFilterEventCallback).GetMethod("Action")?.MakeGenericMethod(type);

                if (eventCallbackGenericCreate != null && eventCallbackGenericAction != null)
                {
                    builder.AddAttribute(4, "ValueChanged", eventCallbackGenericCreate.Invoke(this,
                        new object[] { this, eventCallbackGenericAction.Invoke(this, new object[] { action })! })!);
                }

                builder.CloseComponent();
            });
        }

        internal class NumericFilterEventCallback
        {
            public static EventCallback<T> Create<T>(object receiver, Action<T> action)
            {
                return EventCallback.Factory.Create<T>(receiver, action);
            }

            public static Action<T> Action<T>(Action<object> action)
            {
                return args => action(args!);
            }
        }

        internal string getFilterDateFormat()
        {
            if (property != null && !string.IsNullOrEmpty(property.FormatString))
            {
                return property.FormatString.Replace("{0:", "").Replace("}", "");
            }

            return DataFilter.Configuration.FilterDateFormat;
        }
    }
}