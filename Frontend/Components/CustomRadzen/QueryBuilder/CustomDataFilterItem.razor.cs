using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Frontend.Components.CustomRadzen.QueryBuilder.Models;
using CompositeFilterDescriptor = Frontend.Components.CustomRadzen.QueryBuilder.Models.CompositeFilterDescriptor;
using FilterOperator = Frontend.Components.CustomRadzen.QueryBuilder.Models.FilterOperator;
using PropertyAccess = Frontend.Components.CustomRadzen.QueryBuilder.Models.PropertyAccess;

namespace Frontend.Components.CustomRadzen.QueryBuilder
{
    public partial class CustomDataFilterItem<TItem> : ComponentBase
    {
        [Parameter]
        public CustomDataFilter<TItem> DataFilter { get; set; }

        [Parameter]
        public CustomDataFilterItem<TItem> Parent { get; set; }

        CompositeFilterDescriptor _filter;
        [Parameter]
        public CompositeFilterDescriptor Filter
        {
            get
            {
                return _filter;
            }
            set
            {
                _filter = value;

                if (property == null && Filter.Filters == null)
                {
                    if (Filter.Property != null)
                    {
                        property = DataFilter?.properties.FirstOrDefault(f => object.Equals(f.Property, Filter.Property));
                    }
                    else if (property == null && DataFilter?.UniqueFilters == true)
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

                        if (DataFilter?.UniqueFilters == true)
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

        CustomDataFilterProperty<TItem> property;

        async Task ApplyFilter()
        {
            if (DataFilter.Auto)
            {
                await DataFilter.Filter();
            }
        }

        async Task OnPropertyChange(object p)
        {
            property.FilterValueChange -= OnFilterValueChange;
            property.IsSelected = false;

            property = DataFilter.properties.Where(c => object.Equals(c.Property, p)).FirstOrDefault();

            property.FilterValueChange += OnFilterValueChange;
            if (DataFilter?.UniqueFilters == true)
            {
                property.IsSelected = true;
            }
            Filter.FilterValue = null;

            var defaultOperator = typeof(System.Collections.IEnumerable).IsAssignableFrom(property.FilterPropertyType) ? FilterOperator.Contains : default(FilterOperator);

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

            await ApplyFilter();
        }

        bool IsOperatorNullOrEmpty()
        {
            if (Filter != null)
            {
                return Filter.FilterOperator == FilterOperator.IsEmpty || Filter.FilterOperator == FilterOperator.IsNotEmpty ||
                        Filter.FilterOperator == FilterOperator.IsNull || Filter.FilterOperator == FilterOperator.IsNotNull;
            }

            return false;
        }

        async Task OnOperatorChange(object p)
        {
            if (IsOperatorNullOrEmpty())
            {
                Filter.FilterValue = null;
            }

            // 🆕 Inicializar propiedades para operador Related
            if (Filter.FilterOperator == FilterOperator.Related)
            {
                await InitializeRelatedFilter();
            }

            await ApplyFilter();
        }

        // 🆕 Métodos para manejar filtros anidados

        /// <summary>
        /// Inicializa las propiedades necesarias para un filtro Related
        /// </summary>
        private async Task InitializeRelatedFilter()
        {
            if (property == null)
            {
                Console.WriteLine("⚠️ InitializeRelatedFilter: property is null");
                return;
            }

            Console.WriteLine($"🔧 InitializeRelatedFilter: property={property.Property}, RelatedEntityType={Filter.RelatedEntityType}");

            try
            {
                // Detectar el tipo de entidad relacionada y el path de navegación
                var entityType = GetActualEntityType();
                Console.WriteLine($"🔍 Checking if {entityType.Name}.{property.Property} is Guid relation");

                if (PropertyAccess.IsGuidRelation(entityType, property.Property))
                {
                    Console.WriteLine($"✅ {property.Property} is a Guid relation!");

                    Filter.NavigationPath = PropertyAccess.GetDisplayName(entityType, property.Property);
                    Filter.RelatedEntityType = PropertyAccess.GetRelatedEntityType(entityType, property.Property);
                    Filter.NestingLevel = 0; // Será incrementado por el componente anidado
                    Filter.MaxNestingLevel = 3;
                    Filter.NestedFilters = new List<CompositeFilterDescriptor>();
                    Filter.IsExpanded = false;

                    Console.WriteLine($"🎯 RelatedFilter initialized: NavigationPath={Filter.NavigationPath}, RelatedEntityType={Filter.RelatedEntityType?.Name}");

                    // Forzar re-render del componente
                    await InvokeAsync(StateHasChanged);
                }
                else
                {
                    Console.WriteLine($"❌ {property.Property} is NOT a Guid relation");
                }
            }
            catch (Exception ex)
            {
                // Log error pero no fallar
                Console.WriteLine($"💥 Error initializing related filter: {ex.Message}");
                Console.WriteLine($"💥 Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Determina si debe mostrar el filtro anidado
        /// </summary>
        private bool ShouldShowNestedFilter()
        {
            var hasRelatedEntityType = Filter.RelatedEntityType != null;
            var hasNavigationPath = !string.IsNullOrEmpty(Filter.NavigationPath);
            var withinNestingLimit = Filter.NestingLevel < Filter.MaxNestingLevel;

            var shouldShow = hasRelatedEntityType && hasNavigationPath && withinNestingLimit;

            Console.WriteLine($"🔍 ShouldShowNestedFilter: RelatedEntityType={Filter.RelatedEntityType?.Name}, NavigationPath='{Filter.NavigationPath}', NestingLevel={Filter.NestingLevel}, MaxNesting={Filter.MaxNestingLevel} → {shouldShow}");

            return shouldShow;
        }

        /// <summary>
        /// Alterna la expansión del filtro anidado
        /// </summary>
        private async Task ToggleNestedFilter()
        {
            Filter.IsExpanded = !Filter.IsExpanded;
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Maneja la actualización de filtros anidados
        /// </summary>
        private async Task OnNestedFiltersUpdated()
        {
            // Notificar al componente padre que los filtros han cambiado
            await ApplyFilter();
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Obtiene el tipo real de la entidad basándose en las propiedades disponibles
        /// </summary>
        private Type GetActualEntityType()
        {
            // Si TItem es object, necesitamos inferir el tipo real de la entidad
            if (typeof(TItem) == typeof(object))
            {
                // Intentar obtener el tipo de la primera propiedad disponible
                var firstProperty = DataFilter?.properties?.FirstOrDefault();
                if (firstProperty?.FilterPropertyType != null)
                {
                    // Buscar tipos que contengan esta propiedad
                    var entityType = FindEntityTypeByProperty(firstProperty.Property, firstProperty.FilterPropertyType);
                    if (entityType != null)
                    {
                        Console.WriteLine($"🎯 Inferred entity type: {entityType.Name}");
                        return entityType;
                    }
                }

                Console.WriteLine($"⚠️ Could not infer entity type, using default: {typeof(TItem).Name}");
                return typeof(TItem);
            }

            return typeof(TItem);
        }

        /// <summary>
        /// Busca un tipo de entidad que contenga la propiedad especificada
        /// </summary>
        private Type FindEntityTypeByProperty(string propertyName, Type propertyType)
        {
            try
            {
                // Buscar en el assembly de las entidades compartidas
                var entitiesAssembly = typeof(Shared.Models.Entities.Comuna).Assembly;
                var entityTypes = entitiesAssembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && t.Namespace?.Contains("Entities") == true);

                foreach (var entityType in entityTypes)
                {
                    var prop = entityType.GetProperty(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (prop != null)
                    {
                        var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                        var expectedType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

                        if (propType == expectedType)
                        {
                            Console.WriteLine($"🔍 Found matching entity type: {entityType.Name} for property {propertyName}");
                            return entityType;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Error finding entity type: {ex.Message}");
            }

            return null;
        }

        async Task AddFilter(bool isGroup)
        {
            if (DataFilter?.UniqueFilters == true && DataFilter.properties.All(f => f.IsSelected))
            {
                return;
            }
            if (isGroup)
            {
                Filter.Filters = Filter.Filters.Concat(new CompositeFilterDescriptor[]
                    {
                        new CompositeFilterDescriptor()
                        {
                            Filters = Enumerable.Empty<CompositeFilterDescriptor>()
                        }
                    }
                );
            }
            else
            {
                Filter.Filters = Filter.Filters.Concat(new CompositeFilterDescriptor[] { new CompositeFilterDescriptor() });
            }

            if (DataFilter.Auto)
            {
                await DataFilter.Filter();
            }
        }

        async Task RemoveFilter()
        {
            if (property != null)
            {
                property.IsSelected = false;
            }
            property = null;

            if (Parent != null)
            {
                Parent.Filter.Filters = Parent.Filter.Filters.Where(f => f != Filter).ToList();
                await Parent.ChangeState();
            }
            else
            {
                DataFilter.Filters = DataFilter.Filters.Where(f => f != Filter).ToList();
                await DataFilter.ChangeState();
            }

            await ApplyFilter();
        }

        internal async Task ChangeState()
        {
            await InvokeAsync(StateHasChanged);
        }

        RenderFragment DrawNumericFilter()
        {
            return new RenderFragment(builder =>
            {
                var type = Nullable.GetUnderlyingType(property.FilterPropertyType) != null ?
                    property.FilterPropertyType : typeof(Nullable<>).MakeGenericType(property.FilterPropertyType);

                var numericType = typeof(RadzenNumeric<>).MakeGenericType(type);

                builder.OpenComponent(0, numericType);

                builder.AddAttribute(1, "Value", Filter.FilterValue);
                builder.AddAttribute(2, "class", "rz-datafilter-editor");
                builder.AddAttribute(3, "Disabled", IsOperatorNullOrEmpty());

                Action<object> action = args =>
                {
                    Filter.FilterValue = args; InvokeAsync(ApplyFilter);
                };

                var eventCallbackGenericCreate = typeof(NumericFilterEventCallback).GetMethod("Create").MakeGenericMethod(type);
                var eventCallbackGenericAction = typeof(NumericFilterEventCallback).GetMethod("Action").MakeGenericMethod(type);

                builder.AddAttribute(4, "ValueChanged", eventCallbackGenericCreate.Invoke(this,
                    new object[] { this, eventCallbackGenericAction.Invoke(this, new object[] { action }) }));

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
                return args => action(args);
            }
        }

        internal string getFilterDateFormat()
        {
            if (property != null && !string.IsNullOrEmpty(property.FormatString))
            {
                return property.FormatString.Replace("{0:", "").Replace("}", "");
            }

            return DataFilter.FilterDateFormat;
        }
    }
}