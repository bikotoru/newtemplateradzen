using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using Shared.Models.Export;
using Frontend.Services;
using Shared.Models.QueryModels;
using Shared.Models.Requests;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Linq.Expressions;
using Frontend.Components.Base.Dialogs;

namespace Frontend.Components.Base.Tables;

public partial class EntityTable<T> : ComponentBase, IDisposable where T : class
{
    [Inject] private IServiceProvider ServiceProvider { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private FileDownloadService FileDownloadService { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;

    private RadzenDataGrid<T>? grid;
    
    private IEnumerable<T> entities = null; //SIEMPRE DEBE SER = NULL;
    private int totalCount;
    private bool isLoading = false;
    private string searchTerm = string.Empty;
    private LoadDataArgs? lastLoadDataArgs;
    private BaseApiService<T>? apiService;

    // Auto-refresh variables
    private Timer? autoRefreshTimer;
    private int currentAutoRefreshInterval = 0; // 0 = disabled
    private int autoRefreshCountdown = 0;
    private Timer? countdownTimer;

    // Column configuration variables
    private string currentPath = "";
    private bool hasLoadedColumnConfig = false;
    
    // Para detectar cambios en parámetros importantes
    private List<ColumnConfig<T>>? _previousColumnConfigs;
    private QueryBuilder<T>? _previousBaseQuery;

    // Search variables
    private List<string>? effectiveSearchFields;
    private string currentSearchFieldsInput = "";

    #region Parameters - Data Loading

    [Parameter] public BaseApiService<T>? ApiService { get; set; }
    [Parameter] public QueryBuilder<T>? BaseQuery { get; set; }
    [Parameter] public EventCallback<LoadDataArgs> OnLoadData { get; set; }
    [Parameter] public EventCallback<LoadDataArgs> OnAfterLoadData { get; set; }

    #endregion

    #region Parameters - Columns

    [Parameter] public RenderFragment? Columns { get; set; }
    [Parameter] public List<ColumnConfig<T>>? ColumnConfigs { get; set; }
    [Parameter] public List<string>? ExcludeProperties { get; set; } = new() { "Id", "UsuarioCreacionId", "UsuarioModificacionId" };
    [Parameter] public List<string>? IncludeProperties { get; set; }
    
    [Parameter] public bool ForceOrderByIndex { get; set; } = true;
    [Parameter] public bool EnableSelectOptimization { get; set; } = false;
    [Parameter] public bool SelectOnlyVisibleColumns { get; set; } = false;
    [Parameter] public List<string>? AlwaysSelectFields { get; set; } = new() { "Id" };

    #endregion

    #region Parameters - View Management

    [Parameter] public List<IViewConfiguration<T>>? ViewConfigurations { get; set; }
    [Parameter] public IViewConfiguration<T>? CurrentView { get; set; }
    [Parameter] public EventCallback<IViewConfiguration<T>> OnViewChanged { get; set; }
    [Parameter] public string ViewDisplayNameProperty { get; set; } = "DisplayName";

    #endregion

    #region Parameters - Actions

    [Parameter] public bool ShowActions { get; set; } = true;
    [Parameter] public bool ShowEditButton { get; set; } = true;
    [Parameter] public bool ShowDeleteButton { get; set; } = true;
    [Parameter] public EventCallback<T> OnEdit { get; set; }
    [Parameter] public EventCallback<T> OnDelete { get; set; }
    [Parameter] public RenderFragment<T>? CustomActions { get; set; }
    [Parameter] public string ActionsColumnTitle { get; set; } = "Acciones";
    [Parameter] public string ActionsColumnWidth { get; set; } = "120px";
    [Parameter] public bool FreezeActionsColumn { get; set; } = false;
    [Parameter] public string EditIcon { get; set; } = "edit";
    [Parameter] public string DeleteIcon { get; set; } = "delete";
    [Parameter] public ButtonStyle EditButtonStyle { get; set; } = ButtonStyle.Light;
    [Parameter] public ButtonStyle DeleteButtonStyle { get; set; } = ButtonStyle.Danger;

    #endregion

    #region Parameters - Search

    [Parameter] public bool ShowSearchBar { get; set; } = false;
    [Parameter] public EventCallback<string> OnSearch { get; set; }
    [Parameter] public string SearchPlaceholder { get; set; } = "Buscar...";
    [Parameter] public List<string>? SearchFields { get; set; }
    [Parameter] public List<string>? CustomSearchFields { get; set; }
    [Parameter] public SearchOperator SearchOperator { get; set; } = SearchOperator.Contains;
    [Parameter] public bool AutoDetectSearchFields { get; set; } = true;
    [Parameter] public bool SearchOnlyStringAndNumeric { get; set; } = true;
    [Parameter] public bool ShowSearchFieldsInput { get; set; } = false;

    #endregion

    #region Parameters - Excel Export

    [Parameter] public bool ShowExcelExport { get; set; } = false;
    [Parameter] public EventCallback OnExcelExport { get; set; }
    [Parameter] public EventCallback<ExcelExportContext<T>> OnCustomExcelExport { get; set; }
    [Parameter] public string ExcelButtonText { get; set; } = "Exportar Excel";
    [Parameter] public string ExcelFileName { get; set; } = "";
    [Parameter] public List<ExcelColumnConfig>? ExcelColumns { get; set; }

    #endregion

    #region Parameters - Refresh

    [Parameter] public bool ShowRefreshButton { get; set; } = false;
    [Parameter] public bool ShowAutoRefresh { get; set; } = false;
    [Parameter] public EventCallback OnRefresh { get; set; }
    [Parameter] public string RefreshButtonText { get; set; } = "Actualizar";
    [Parameter] public List<int> AutoRefreshIntervals { get; set; } = new() { 5, 15, 30, 60, 300, 900 };

    #endregion

    #region Parameters - Column Configuration

    [Parameter] public bool ShowColumnConfig { get; set; } = false;
    [Parameter] public string ColumnConfigButtonText { get; set; } = "Configurar Columnas";

    #endregion

    #region Parameters - Grid Configuration

    [Parameter] public bool AllowPaging { get; set; } = true;
    [Parameter] public bool AllowSorting { get; set; } = true;
    [Parameter] public bool AllowFiltering { get; set; } = true;
    [Parameter] public bool AllowColumnResize { get; set; } = false;
    [Parameter] public int PageSize { get; set; } = 10;
    [Parameter] public int[] PageSizeOptions { get; set; } = { 10, 20, 50, 100, 1000 };
    [Parameter] public bool ShowPagingSummary { get; set; } = true;
    [Parameter] public HorizontalAlign PagerHorizontalAlign { get; set; } = HorizontalAlign.Center;
    [Parameter] public string EmptyText { get; set; } = "No se encontraron registros";
    [Parameter] public string Style { get; set; } = "min-height: 440px";
    [Parameter] public string? ColumnWidth { get; set; }

    #endregion

    protected override async Task OnInitializedAsync()
    {
        if (ApiService != null)
        {
            apiService = ApiService;
        }
        else
        {
            var serviceName = $"{typeof(T).Name}Service";
            var serviceType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == serviceName && typeof(BaseApiService<T>).IsAssignableFrom(t));

            if (serviceType != null)
            {
                apiService = ServiceProvider.GetService(serviceType) as BaseApiService<T>;
            }

            if (apiService == null)
            {
                apiService = ServiceProvider.GetService<BaseApiService<T>>();
            }
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        var columnConfigsChanged = !AreColumnConfigsEqual(_previousColumnConfigs, ColumnConfigs);
        var baseQueryChanged = _previousBaseQuery != BaseQuery;
        
        Console.WriteLine($"[EntityTable] OnParametersSetAsync ejecutado:");
        Console.WriteLine($"[EntityTable]   hasLoadedColumnConfig: {hasLoadedColumnConfig}");
        Console.WriteLine($"[EntityTable]   grid != null: {grid != null}");
        Console.WriteLine($"[EntityTable]   columnConfigsChanged: {columnConfigsChanged}");
        Console.WriteLine($"[EntityTable]   baseQueryChanged: {baseQueryChanged}");
        Console.WriteLine($"[EntityTable]   ColumnConfigs count: {ColumnConfigs?.Count ?? 0}");
        Console.WriteLine($"[EntityTable]   _previousColumnConfigs count: {_previousColumnConfigs?.Count ?? 0}");
        
        if (ColumnConfigs != null)
        {
            Console.WriteLine("[EntityTable]   ColumnConfigs actuales:");
            foreach (var col in ColumnConfigs)
            {
                Console.WriteLine($"[EntityTable]     - {col.Property} ({col.Title}) - Visible: {col.Visible}, Order: {col.Order}");
            }
        }
        
        if (hasLoadedColumnConfig && grid != null && (columnConfigsChanged || baseQueryChanged))
        {
            Console.WriteLine("[EntityTable] ¡Cambios detectados! Ejecutando Reload()");
            await Reload();
        }
        else
        {
            Console.WriteLine("[EntityTable] No hay cambios o condiciones no cumplen para reload");
        }
        
        _previousColumnConfigs = ColumnConfigs?.ToList();
        _previousBaseQuery = BaseQuery;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            ConfigureSpanishTexts();
            
            if (!hasLoadedColumnConfig)
            {
                await ApplyStoredColumnConfiguration();
                hasLoadedColumnConfig = true;
            }
        }
    }

    private void ConfigureSpanishTexts()
    {
        if (grid != null)
        {
            grid.AndOperatorText = "Y";
            grid.OrOperatorText = "O";
            grid.EqualsText = "Igual a";
            grid.NotEqualsText = "No es igual a";
            grid.LessThanText = "Menor que";
            grid.LessThanOrEqualsText = "Menor que o igual";
            grid.GreaterThanText = "Mayor que";
            grid.GreaterThanOrEqualsText = "Mayor que o igual";
            grid.IsNullText = "Es nulo";
            grid.IsNotNullText = "No es nulo";
            grid.ContainsText = "Contiene";
            grid.DoesNotContainText = "No contiene";
            grid.StartsWithText = "Inicia con";
            grid.EndsWithText = "Termina con";
            grid.ClearFilterText = "Limpiar";
            grid.ApplyFilterText = "Aplicar";
            grid.FilterText = "Filtrar";
            grid.PageSizeText = "Items por página";
            grid.PagingSummaryFormat = "Páginas {0} de {1} ({2} items)";
            grid.EmptyText = "No se encontraron registros";
        }
    }

    #region Public Methods

    public async Task Reload()
    {
        if (grid != null)
        {
            await grid.Reload();
        }
    }

    public async Task FirstPage()
    {
        if (grid != null)
        {
            await grid.FirstPage();
            await grid.Reload();
        }
    }

    public List<ExcelColumnConfig> GetVisibleColumns()
    {
        var columns = new List<ExcelColumnConfig>();
        
        if (grid?.ColumnsCollection != null)
        {
            foreach (var column in grid.ColumnsCollection.Where(c => c.Visible && !string.IsNullOrEmpty(c.Property)))
            {
                var excelColumn = new ExcelColumnConfig
                {
                    PropertyPath = column.Property!,
                    DisplayName = column.Title ?? column.Property!,
                    Width = column.Width?.Length > 0 ? ConvertToExcelWidth(column.Width) : 15,
                    Visible = column.Visible
                };

                ApplyColumnSpecificFormatting(excelColumn);
                columns.Add(excelColumn);
            }
        }
        
        return columns;
    }

    #endregion

    private bool AreColumnConfigsEqual(List<ColumnConfig<T>>? list1, List<ColumnConfig<T>>? list2)
    {
        if (list1 == null && list2 == null) return true;
        if (list1 == null || list2 == null) return false;
        if (list1.Count != list2.Count) return false;
        
        for (int i = 0; i < list1.Count; i++)
        {
            var col1 = list1[i];
            var col2 = list2[i];
            
            if (col1.Property != col2.Property ||
                col1.Title != col2.Title ||
                col1.Visible != col2.Visible ||
                col1.Order != col2.Order)
            {
                return false;
            }
        }
        
        return true;
    }

    public void Dispose()
    {
        StopAutoRefresh();
    }
}