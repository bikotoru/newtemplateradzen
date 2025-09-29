using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Radzen;
using Radzen.Blazor;
using Frontend.Components.CustomRadzen.QueryBuilder.Models;

namespace Frontend.Components.CustomRadzen.QueryBuilder
{
    /// <summary>
    /// CustomDataFilter component - versión personalizable del RadzenDataFilter
    /// </summary>
    /// <typeparam name="TItem">El tipo del item del filtro</typeparam>
#if NET6_0_OR_GREATER
    [CascadingTypeParameter(nameof(TItem))]
#endif
    public partial class CustomDataFilter<TItem> : RadzenComponent
    {
        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return $"{Configuration.Styles.ContainerClass} rz-datafilter";
        }

        #region Properties and Parameters

        /// <summary>
        /// Las propiedades del filtro
        /// </summary>
        [Parameter]
        public RenderFragment Properties { get; set; } = null!;

        /// <summary>
        /// Configuración personalizable del componente
        /// </summary>
        [Parameter]
        public CustomDataFilterConfiguration Configuration { get; set; } = new();

        /// <summary>
        /// Eventos personalizables del componente
        /// </summary>
        [Parameter]
        public CustomDataFilterEvents Events { get; set; } = new();

        /// <summary>
        /// Muestra información de debug
        /// </summary>
        [Parameter]
        public bool ShowDebugInfo { get; set; } = false;

        /// <summary>
        /// Templates personalizados para tipos específicos
        /// </summary>
        [Parameter]
        public Dictionary<Type, CustomFilterTemplate> CustomTemplates { get; set; } = new();

        /// <summary>
        /// Los datos
        /// </summary>
        IEnumerable<TItem>? _data;

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        [Parameter]
        public IEnumerable<TItem>? Data
        {
            get => _data;
            set
            {
                if (_data != value)
                {
                    _data = value;
                    StateHasChanged();
                }
            }
        }

        IQueryable<TItem>? _view = null;

        /// <summary>
        /// Gets the view.
        /// </summary>
        public virtual IQueryable<TItem> View
        {
            get
            {
                if (_view == null)
                {
                    _view = Data != null ? Data.AsQueryable() : Enumerable.Empty<TItem>().AsQueryable();
                }
                return _view;
            }
        }

        /// <summary>
        /// Indica si el filtro es automático
        /// </summary>
        [Parameter]
        public bool Auto
        {
            get => Configuration.Auto;
            set => Configuration.Auto = value;
        }

        /// <summary>
        /// Los filtros actuales
        /// </summary>
        public IEnumerable<CompositeFilterDescriptor> Filters { get; set; } = Enumerable.Empty<CompositeFilterDescriptor>();

        /// <summary>
        /// Operador lógico del filtro
        /// </summary>
        [Parameter]
        public LogicalFilterOperator LogicalFilterOperator { get; set; } = LogicalFilterOperator.And;

        /// <summary>
        /// Sensibilidad a mayúsculas
        /// </summary>
        [Parameter]
        public FilterCaseSensitivity FilterCaseSensitivity
        {
            get => Configuration.FilterCaseSensitivity;
            set => Configuration.FilterCaseSensitivity = value;
        }

        /// <summary>
        /// Permite filtros únicos
        /// </summary>
        [Parameter]
        public bool UniqueFilters
        {
            get => Configuration.UniqueFilters;
            set => Configuration.UniqueFilters = value;
        }

        /// <summary>
        /// Permite filtrado de columnas
        /// </summary>
        [Parameter]
        public bool AllowColumnFiltering
        {
            get => Configuration.AllowColumnFiltering;
            set => Configuration.AllowColumnFiltering = value;
        }

        /// <summary>
        /// Formato de fecha para filtros
        /// </summary>
        [Parameter]
        public string FilterDateFormat
        {
            get => Configuration.FilterDateFormat;
            set => Configuration.FilterDateFormat = value;
        }

        /// <summary>
        /// Callback cuando la vista cambia
        /// </summary>
        [Parameter]
        public EventCallback<IQueryable<TItem>> ViewChanged { get; set; }

        // Todos los textos configurables como parámetros para compatibilidad
        [Parameter] public string FilterText { get; set; } = "Filter";
        [Parameter] public string EnumFilterSelectText { get; set; } = "Select...";
        [Parameter] public string AndOperatorText { get; set; } = "And";
        [Parameter] public string OrOperatorText { get; set; } = "Or";
        [Parameter] public string ApplyFilterText { get; set; } = "Apply";
        [Parameter] public string ClearFilterText { get; set; } = "Clear all";
        [Parameter] public string AddFilterText { get; set; } = "Add filter";
        [Parameter] public string RemoveFilterText { get; set; } = "Remove filter";
        [Parameter] public string AddFilterGroupText { get; set; } = "Add filter group";
        [Parameter] public string EqualsText { get; set; } = "Equals";
        [Parameter] public string NotEqualsText { get; set; } = "Not equals";
        [Parameter] public string LessThanText { get; set; } = "Less than";
        [Parameter] public string LessThanOrEqualsText { get; set; } = "Less than or equals";
        [Parameter] public string GreaterThanText { get; set; } = "Greater than";
        [Parameter] public string GreaterThanOrEqualsText { get; set; } = "Greater than or equals";
        [Parameter] public string EndsWithText { get; set; } = "Ends with";
        [Parameter] public string ContainsText { get; set; } = "Contains";
        [Parameter] public string DoesNotContainText { get; set; } = "Does not contain";
        [Parameter] public string InText { get; set; } = "In";
        [Parameter] public string NotInText { get; set; } = "Not in";
        [Parameter] public string StartsWithText { get; set; } = "Starts with";
        [Parameter] public string IsNotNullText { get; set; } = "Is not null";
        [Parameter] public string IsNullText { get; set; } = "Is null";
        [Parameter] public string IsEmptyText { get; set; } = "Is empty";
        [Parameter] public string IsNotEmptyText { get; set; } = "Is not empty";
        [Parameter] public string CustomText { get; set; } = "Custom";

        #endregion

        #region Properties Management

        /// <summary>
        /// Colección de propiedades
        /// </summary>
        public IList<CustomDataFilterProperty<TItem>> PropertiesCollection => properties;

        internal List<CustomDataFilterProperty<TItem>> properties = new();
        protected bool disposed = false;

        internal void AddProperty(CustomDataFilterProperty<TItem> property)
        {
            if (!properties.Contains(property))
            {
                properties.Add(property);
            }
            StateHasChanged();
        }

        internal void RemoveProperty(CustomDataFilterProperty<TItem> property)
        {
            if (properties.Contains(property))
            {
                properties.Remove(property);
            }

            if (!disposed)
            {
                try { InvokeAsync(StateHasChanged); } catch { }
            }
        }

        #endregion

        #region Lifecycle

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            // Sincronizar textos de parámetros con configuración
            SyncTextsFromParameters();
        }

        private void SyncTextsFromParameters()
        {
            if (!string.IsNullOrEmpty(FilterText)) Configuration.Texts.FilterText = FilterText;
            if (!string.IsNullOrEmpty(EnumFilterSelectText)) Configuration.Texts.EnumFilterSelectText = EnumFilterSelectText;
            if (!string.IsNullOrEmpty(AndOperatorText)) Configuration.Texts.AndOperatorText = AndOperatorText;
            if (!string.IsNullOrEmpty(OrOperatorText)) Configuration.Texts.OrOperatorText = OrOperatorText;
            if (!string.IsNullOrEmpty(ApplyFilterText)) Configuration.Texts.ApplyFilterText = ApplyFilterText;
            if (!string.IsNullOrEmpty(ClearFilterText)) Configuration.Texts.ClearFilterText = ClearFilterText;
            if (!string.IsNullOrEmpty(AddFilterText)) Configuration.Texts.AddFilterText = AddFilterText;
            if (!string.IsNullOrEmpty(RemoveFilterText)) Configuration.Texts.RemoveFilterText = RemoveFilterText;
            if (!string.IsNullOrEmpty(AddFilterGroupText)) Configuration.Texts.AddFilterGroupText = AddFilterGroupText;
            if (!string.IsNullOrEmpty(EqualsText)) Configuration.Texts.EqualsText = EqualsText;
            if (!string.IsNullOrEmpty(NotEqualsText)) Configuration.Texts.NotEqualsText = NotEqualsText;
            if (!string.IsNullOrEmpty(LessThanText)) Configuration.Texts.LessThanText = LessThanText;
            if (!string.IsNullOrEmpty(LessThanOrEqualsText)) Configuration.Texts.LessThanOrEqualsText = LessThanOrEqualsText;
            if (!string.IsNullOrEmpty(GreaterThanText)) Configuration.Texts.GreaterThanText = GreaterThanText;
            if (!string.IsNullOrEmpty(GreaterThanOrEqualsText)) Configuration.Texts.GreaterThanOrEqualsText = GreaterThanOrEqualsText;
            if (!string.IsNullOrEmpty(EndsWithText)) Configuration.Texts.EndsWithText = EndsWithText;
            if (!string.IsNullOrEmpty(ContainsText)) Configuration.Texts.ContainsText = ContainsText;
            if (!string.IsNullOrEmpty(DoesNotContainText)) Configuration.Texts.DoesNotContainText = DoesNotContainText;
            if (!string.IsNullOrEmpty(InText)) Configuration.Texts.InText = InText;
            if (!string.IsNullOrEmpty(NotInText)) Configuration.Texts.NotInText = NotInText;
            if (!string.IsNullOrEmpty(StartsWithText)) Configuration.Texts.StartsWithText = StartsWithText;
            if (!string.IsNullOrEmpty(IsNotNullText)) Configuration.Texts.IsNotNullText = IsNotNullText;
            if (!string.IsNullOrEmpty(IsNullText)) Configuration.Texts.IsNullText = IsNullText;
            if (!string.IsNullOrEmpty(IsEmptyText)) Configuration.Texts.IsEmptyText = IsEmptyText;
            if (!string.IsNullOrEmpty(IsNotEmptyText)) Configuration.Texts.IsNotEmptyText = IsNotEmptyText;
            if (!string.IsNullOrEmpty(CustomText)) Configuration.Texts.CustomText = CustomText;
        }

        #endregion

        #region Filter Operations

        /// <summary>
        /// Recrea la vista usando los filtros actuales
        /// </summary>
        public async Task Filter()
        {
            _view = null;
            await ViewChanged.InvokeAsync(View);
        }

        internal async Task ChangeState()
        {
            await InvokeAsync(StateHasChanged);
        }

        internal async Task AddFilter(bool isGroup)
        {
            if (Configuration.UniqueFilters && properties.All(f => f.IsSelected))
            {
                return;
            }

            var newFilter = new CompositeFilterDescriptor();

            if (isGroup)
            {
                newFilter.Filters = Enumerable.Empty<CompositeFilterDescriptor>();
                Filters = Filters.Concat(new[] { newFilter });
            }
            else
            {
                Filters = Filters.Concat(new[] { newFilter });
            }

            // Disparar evento personalizado
            await Events.OnFilterAdded.InvokeAsync(CustomCompositeFilterDescriptor.FromRadzenFilter(newFilter));
            await Events.OnFiltersChanged.InvokeAsync(Filters.Select(CustomCompositeFilterDescriptor.FromRadzenFilter));

            if (Configuration.Auto)
            {
                await Filter();
            }
        }

        /// <summary>
        /// Limpiar todos los filtros
        /// </summary>
        public async Task ClearFilters()
        {
            Filters = Enumerable.Empty<CompositeFilterDescriptor>();

            properties.ForEach(p => p.IsSelected = false);

            // Disparar evento personalizado
            await Events.OnFiltersCleared.InvokeAsync();
            await Events.OnFiltersChanged.InvokeAsync(Enumerable.Empty<CustomCompositeFilterDescriptor>());

            if (Configuration.Auto)
            {
                await Filter();
            }
        }

        /// <summary>
        /// Agregar filtro específico
        /// </summary>
        public async Task AddFilter(CompositeFilterDescriptor filter)
        {
            Filters = Filters.Concat(new[] { filter });

            // Disparar evento personalizado
            await Events.OnFilterAdded.InvokeAsync(CustomCompositeFilterDescriptor.FromRadzenFilter(filter));
            await Events.OnFiltersChanged.InvokeAsync(Filters.Select(CustomCompositeFilterDescriptor.FromRadzenFilter));

            if (Configuration.Auto)
            {
                await Filter();
            }
        }

        /// <summary>
        /// Remover filtro específico
        /// </summary>
        public async Task RemoveFilter(CompositeFilterDescriptor filter)
        {
            Filters = Filters.Where(f => f != filter);

            // Disparar evento personalizado
            await Events.OnFilterRemoved.InvokeAsync(CustomCompositeFilterDescriptor.FromRadzenFilter(filter));
            await Events.OnFiltersChanged.InvokeAsync(Filters.Select(CustomCompositeFilterDescriptor.FromRadzenFilter));

            if (Configuration.Auto)
            {
                await Filter();
            }
        }

        #endregion

        #region Template Management

        /// <summary>
        /// Obtiene el template personalizado para un tipo específico
        /// </summary>
        public CustomFilterTemplate? GetCustomTemplate(Type propertyType)
        {
            return CustomTemplates.TryGetValue(propertyType, out var template) ? template : null;
        }

        /// <summary>
        /// Registra un template personalizado para un tipo
        /// </summary>
        public void RegisterCustomTemplate(Type propertyType, CustomFilterTemplate template)
        {
            CustomTemplates[propertyType] = template;
        }

        #endregion

        #region Dispose

        /// <summary>
        /// Dispose del componente
        /// </summary>
        public override void Dispose()
        {
            disposed = true;
            base.Dispose();
        }

        #endregion
    }
}