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
using Frontend.Services;
using Radzen.Blazor;

namespace Frontend.Components.CustomRadzen.QueryBuilder
{
    public partial class CustomDataFilterItem<TItem> : ComponentBase
    {
        [Parameter]
        public CustomDataFilter<TItem> DataFilter { get; set; }

        [Parameter]
        public CustomDataFilterItem<TItem> Parent { get; set; }

        [Inject] private EntityRegistrationService EntityRegistrationService { get; set; }

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
                // Buscar el assembly de Shared.Models de manera genérica
                System.Reflection.Assembly? entitiesAssembly = null;
                
                try
                {
                    // Opción 1: Buscar por nombre de assembly
                    entitiesAssembly = AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(a => a.GetName().Name == "Shared.Models");
                    
                    // Opción 2: Si no encontramos por nombre, buscar cualquier assembly que contenga Shared.Models.Entities
                    if (entitiesAssembly == null)
                    {
                        entitiesAssembly = AppDomain.CurrentDomain.GetAssemblies()
                            .FirstOrDefault(a => a.GetTypes().Any(t => t.Namespace?.StartsWith("Shared.Models.Entities") == true));
                    }
                    
                    // Opción 3: Como último recurso, cargar el assembly por referencia
                    if (entitiesAssembly == null)
                    {
                        entitiesAssembly = System.Reflection.Assembly.Load("Shared.Models");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"💥 Error loading Shared.Models assembly: {ex.Message}");
                    return null;
                }

                if (entitiesAssembly == null)
                {
                    Console.WriteLine("⚠️ Could not find Shared.Models assembly");
                    return null;
                }

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

        /// <summary>
        /// Determina si debe mostrar el Lookup para campos relacionados
        /// </summary>
        private bool ShouldShowLookupForRelatedField()
        {
            if (property == null || Filter == null) return false;

            // Solo para operadores Equals y NotEquals
            var isEqualsOperator = Filter.FilterOperator == FilterOperator.Equals || Filter.FilterOperator == FilterOperator.NotEquals;
            if (!isEqualsOperator) return false;

            // Solo para campos Guid de relación o entidades complejas
            var entityType = GetActualEntityType();
            var isGuidRelation = PropertyAccess.IsGuidRelation(entityType, property.Property);
            var isComplexEntity = IsComplexEntityField();

            var shouldShow = isGuidRelation || isComplexEntity;

            Console.WriteLine($"🔍 ShouldShowLookupForRelatedField: Property={property.Property}, Operator={Filter.FilterOperator}, IsGuidRelation={isGuidRelation}, IsComplexEntity={isComplexEntity} → {shouldShow}");

            return shouldShow;
        }

        /// <summary>
        /// Renderiza el componente Lookup para campos relacionados
        /// </summary>
        private RenderFragment RenderLookupForRelatedField()
        {
            return new RenderFragment(builder =>
            {
                try
                {
                    if (property == null || Filter == null)
                    {
                        Console.WriteLine("⚠️ RenderLookupForRelatedField: property or Filter is null");
                        return;
                    }

                    var entityType = GetActualEntityType();

                    // Determinar la entidad relacionada
                    Type relatedEntityType = null;
                    string entityKey = null;

                    if (PropertyAccess.IsGuidRelation(entityType, property.Property))
                    {
                        // Para campos Guid de relación (RegionId, PaisId, etc.)
                        relatedEntityType = PropertyAccess.GetRelatedEntityType(entityType, property.Property);
                        entityKey = relatedEntityType?.Name?.ToLowerInvariant();
                    }
                    else if (IsComplexEntityField())
                    {
                        // Para entidades complejas (Region, Pais, etc.)
                        relatedEntityType = property.FilterPropertyType;
                        entityKey = relatedEntityType?.Name?.ToLowerInvariant();
                    }

                    if (relatedEntityType == null || string.IsNullOrEmpty(entityKey))
                    {
                        Console.WriteLine($"⚠️ Could not determine related entity type for property: {property.Property}");
                        RenderFallbackTextBox(builder);
                        return;
                    }

                    Console.WriteLine($"🎯 Rendering Lookup for entity: {relatedEntityType.Name}, key: {entityKey}");

                    // Obtener configuración de la entidad
                    var entityConfig = EntityRegistrationService?.GetEntityConfiguration(entityKey);
                    if (entityConfig == null)
                    {
                        Console.WriteLine($"⚠️ No entity configuration found for: {entityKey}");
                        RenderFallbackTextBox(builder);
                        return;
                    }

                    // Crear el tipo de Lookup genérico
                    var lookupType = typeof(Frontend.Components.Base.Lookup<,>).MakeGenericType(relatedEntityType, typeof(Guid?));

                    builder.OpenComponent(100, lookupType);

                    // Configurar propiedades del Lookup
                    builder.AddAttribute(101, "Value", GetLookupValue());
                    builder.AddAttribute(102, "ValueChanged", CreateLookupValueChangedCallback());
                    builder.AddAttribute(103, "DisplayProperty", entityConfig.DisplayProperty ?? "Name");
                    builder.AddAttribute(104, "ValueProperty", entityConfig.ValueProperty ?? "Id");
                    builder.AddAttribute(105, "Placeholder", $"Seleccione {GetDisplayNameForProperty()}");
                    builder.AddAttribute(106, "AllowClear", true);
                    builder.AddAttribute(107, "Disabled", IsOperatorNullOrEmpty());
                    builder.AddAttribute(108, "ShowAdd", false); // Sin crear en filtros
                    builder.AddAttribute(109, "EnableCache", false);
                    builder.AddAttribute(110, "ShowSearch", true);

                    // Obtener y configurar el servicio
                    var service = GetEntityService(entityKey, entityConfig);
                    if (service != null)
                    {
                        builder.AddAttribute(112, "Service", service);
                        Console.WriteLine($"✅ Service configured for entity: {entityKey}");
                    }
                    else
                    {
                        Console.WriteLine($"⚠️ Could not obtain service for entity: {entityKey}");
                    }

                    builder.CloseComponent();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"💥 Error rendering Lookup: {ex.Message}");
                    RenderFallbackTextBox(builder);
                }
            });
        }

        /// <summary>
        /// Obtiene el valor actual para el Lookup
        /// </summary>
        private Guid? GetLookupValue()
        {
            if (Filter.FilterValue == null) return null;

            if (Filter.FilterValue is Guid guidValue)
                return guidValue;

            if (Guid.TryParse(Filter.FilterValue.ToString(), out var parsedGuid))
                return parsedGuid;

            return null;
        }

        /// <summary>
        /// Crea el callback para cuando cambia el valor del Lookup
        /// </summary>
        private EventCallback<Guid?> CreateLookupValueChangedCallback()
        {
            return EventCallback.Factory.Create<Guid?>(this, async newValue =>
            {
                Filter.FilterValue = newValue;
                await ApplyFilter();
                Console.WriteLine($"🔄 Lookup value changed to: {newValue}");
            });
        }

        /// <summary>
        /// Obtiene un nombre de display amigable para la propiedad
        /// </summary>
        private string GetDisplayNameForProperty()
        {
            if (property == null) return "elemento";

            var entityType = GetActualEntityType();
            return PropertyAccess.GetDisplayName(entityType, property.Property);
        }

        /// <summary>
        /// Obtiene el servicio para una entidad usando EntityRegistrationService
        /// </summary>
        private object GetEntityService(string entityKey, EntityRegistrationService.EntityConfiguration entityConfig)
        {
            try
            {
                // Usar el método del EntityRegistrationService
                return EntityRegistrationService?.GetEntityService(entityKey);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Error getting service for {entityKey}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Renderiza un TextBox de fallback cuando no se puede usar Lookup
        /// </summary>
        private void RenderFallbackTextBox(RenderTreeBuilder builder)
        {
            builder.OpenComponent<RadzenTextBox>(200);
            builder.AddAttribute(201, "Disabled", IsOperatorNullOrEmpty());
            builder.AddAttribute(202, "class", "rz-datafilter-editor");
            builder.AddAttribute(203, "Value", Filter.FilterValue?.ToString() ?? "");
            builder.AddAttribute(204, "Change", EventCallback.Factory.Create<string>(this, async args =>
            {
                Filter.FilterValue = args;
                await ApplyFilter();
            }));
            builder.CloseComponent();
        }

        /// <summary>
        /// Determina si el campo es una entidad compleja/clase
        /// </summary>
        private bool IsComplexEntityField()
        {
            if (property?.FilterPropertyType == null) return false;

            return property.FilterPropertyType.IsClass &&
                   property.FilterPropertyType != typeof(string) &&
                   !property.FilterPropertyType.IsArray &&
                   !property.FilterPropertyType.IsPrimitive;
        }
    }
}