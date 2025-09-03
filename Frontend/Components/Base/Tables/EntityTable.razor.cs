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
    
    /// <summary>
    /// Propiedades definidas en RenderFragment Columns que deben excluirse de ColumnConfigs
    /// </summary>
    [Parameter] public List<string>? RenderFragmentProperties { get; set; }
    
    /// <summary>
    /// Fuerza el ordenamiento por índice Order SI O SI (true por defecto)
    /// </summary>
    [Parameter] public bool ForceOrderByIndex { get; set; } = true;
    
    /// <summary>
    /// Combina RenderFragment Columns con ColumnConfigs (false = solo usa uno u otro)
    /// </summary>
    [Parameter] public bool CombineColumnsAndConfigs { get; set; } = false;
    
    /// <summary>
    /// Habilita la optimización de Select para traer solo campos necesarios
    /// </summary>
    [Parameter] public bool EnableSelectOptimization { get; set; } = false;
    
    /// <summary>
    /// Solo trae las columnas visibles cuando EnableSelectOptimization es true
    /// </summary>
    [Parameter] public bool SelectOnlyVisibleColumns { get; set; } = false;
    
    /// <summary>
    /// Campos que siempre deben incluirse en el Select (ej: Id para operaciones)
    /// </summary>
    [Parameter] public List<string>? AlwaysSelectFields { get; set; } = new() { "Id" };

    #endregion

    #region Parameters - View Management

    /// <summary>
    /// Lista de configuraciones de vista disponibles
    /// </summary>
    [Parameter] public List<object>? ViewConfigurations { get; set; }
    
    /// <summary>
    /// Vista actualmente seleccionada
    /// </summary>
    [Parameter] public object? CurrentView { get; set; }
    
    /// <summary>
    /// Callback cuando cambia la vista seleccionada
    /// </summary>
    [Parameter] public EventCallback<object> OnViewChanged { get; set; }
    
    /// <summary>
    /// Propiedad para obtener el DisplayName de la vista
    /// </summary>
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
    [Parameter] public List<int> AutoRefreshIntervals { get; set; } = new() { 5, 15, 30, 60, 300, 900 }; // en segundos

    #endregion

    #region Parameters - Column Configuration

    [Parameter] public bool ShowColumnConfig { get; set; } = false;
    [Parameter] public string ColumnConfigButtonText { get; set; } = "Configurar Columnas";

    #endregion


    #region Parameters - Grid Configuration

    [Parameter] public bool AllowPaging { get; set; } = true;
    [Parameter] public bool AllowSorting { get; set; } = true;
    [Parameter] public bool AllowFiltering { get; set; } = true;
    [Parameter] public bool AllowColumnResize { get; set; } = true;
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
        // Si se pasó un servicio como parámetro, usarlo directamente
        if (ApiService != null)
        {
            apiService = ApiService;
        }
        else
        {
            // Si no se pasó servicio, intentar encontrarlo por convención
            // Buscar servicio por convención: [EntityName]Service
            var serviceName = $"{typeof(T).Name}Service";
            var serviceType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == serviceName && typeof(BaseApiService<T>).IsAssignableFrom(t));

            if (serviceType != null)
            {
                apiService = ServiceProvider.GetService(serviceType) as BaseApiService<T>;
            }

            // Si no encontramos un servicio específico, intentar con el genérico
            if (apiService == null)
            {
                apiService = ServiceProvider.GetService<BaseApiService<T>>();
            }
        }

    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !hasLoadedColumnConfig)
        {
            // Cargar configuración de columnas desde localStorage después del primer render
            await ApplyStoredColumnConfiguration();
            hasLoadedColumnConfig = true;
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

    #region Private Methods - Data Loading

    private async Task LoadData(LoadDataArgs args)
    {
        var callId = Guid.NewGuid().ToString()[..8];
        Console.WriteLine($"[DEBUG] ==> LoadData INICIO #{callId} con searchTerm: '{searchTerm}'");
        Console.WriteLine($"[DEBUG] ==> #{callId} ColumnConfigs Count: {ColumnConfigs?.Count ?? 0}");
        Console.WriteLine($"[DEBUG] ==> #{callId} isLoading: {isLoading}");
        
        if (isLoading)
        {
            Console.WriteLine($"[DEBUG] ==> #{callId} YA ESTÁ CARGANDO, SALTANDO...");
            return;
        }
        
        try
        {
            isLoading = true;
            lastLoadDataArgs = args;

            // Si hay callback custom, usarlo
            if (OnLoadData.HasDelegate)
            {
                await OnLoadData.InvokeAsync(args);
            }
            else if (apiService != null)
            {
                // Verificar si hay búsqueda activa
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    await LoadDataWithSearch(args);
                }
                else
                {
                    Console.WriteLine($"[DEBUG] ==> Sin búsqueda, decidiendo ruta de optimización...");
                    
                    // Decidir si usar optimización de Select
                    if (ShouldUseSelectOptimization())
                    {
                        Console.WriteLine($"[DEBUG] ==> Usando optimización de Select");
                        await LoadDataWithSelectOptimization(args);
                    }
                    else
                    {
                        Console.WriteLine($"[DEBUG] ==> Usando API normal sin optimización");
                        // Usar el servicio API normal
                        var response = BaseQuery != null 
                            ? await apiService.LoadDataAsync(args, BaseQuery)
                            : await apiService.LoadDataAsync(args);
                        
                        if (response.Success && response.Data != null)
                        {
                            entities = response.Data.Data;
                            totalCount = response.Data.TotalCount;
                        }
                        else
                        {
                            entities = new List<T>();
                            totalCount = 0;
                        }
                    }
                }
            }
            else
            {
                // Sin servicio ni callback, datos vacíos
                entities = new List<T>();
                totalCount = 0;
            }

            // Notificar después de cargar
            if (OnAfterLoadData.HasDelegate)
            {
                await OnAfterLoadData.InvokeAsync(args);
            }
        }
        catch (Exception ex)
        {
            entities = new List<T>();
            totalCount = 0;
            await DialogService.Alert($"Error al cargar datos: {ex.Message}", "Error");
        }
        finally
        {
            Console.WriteLine($"[DEBUG] ==> LoadData TERMINADO #{callId}");
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task LoadDataWithSelectOptimization(LoadDataArgs args)
    {
        try
        {
            // Construir expresión de Select usando QueryBuilder y Select string
            var selectString = BuildSelectString();
            
            // Usar QueryBuilder con Select manual a través de QueryRequest
            var query = BaseQuery != null
                ? apiService!.Query().And(BaseQuery)
                : apiService!.Query();

            // Convertir LoadDataArgs a QueryRequest y agregar Select
            var queryRequest = ConvertLoadDataArgsToQueryRequestWithSelect(args, selectString);
            
            // Ejecutar query usando el endpoint select-paged
            var response = await apiService.QuerySelectPagedAsync(queryRequest);
            
            if (response.Success && response.Data != null)
            {
                // Los datos vienen como List<object> del endpoint select
                // Necesitamos convertirlos de vuelta a T usando reflection
                entities = ConvertSelectResultsToEntityType(response.Data.Data);
                totalCount = response.Data.TotalCount;
            }
            else
            {
                entities = new List<T>();
                totalCount = 0;
            }
        }
        catch (Exception)
        {
            // Si falla la optimización, fallback al método normal
            var response = BaseQuery != null 
                ? await apiService!.LoadDataAsync(args, BaseQuery)
                : await apiService!.LoadDataAsync(args);
            
            if (response.Success && response.Data != null)
            {
                entities = response.Data.Data;
                totalCount = response.Data.TotalCount;
            }
            else
            {
                entities = new List<T>();
                totalCount = 0;
            }
        }
    }

    private async Task LoadDataWithSearch(LoadDataArgs args)
    {
        try
        {
            // Obtener campos de búsqueda efectivos
            var searchFields = GetEffectiveSearchFields();
            
            // Crear SearchRequest
            var searchRequest = BuildSearchRequest(args, searchFields);
            
            // Decidir si usar optimización de Select con búsqueda
            if (ShouldUseSelectOptimization())
            {
                await LoadDataWithSearchAndSelectOptimization(args, searchRequest);
            }
            else
            {
                // Ejecutar búsqueda usando endpoint search-paged normal
                var response = await apiService!.SearchPagedAsync(searchRequest);
                
                if (response.Success && response.Data != null)
                {
                    entities = response.Data.Data;
                    totalCount = response.Data.TotalCount;
                }
                else
                {
                    entities = new List<T>();
                    totalCount = 0;
                }
            }
        }
        catch (Exception)
        {
            // Si falla la búsqueda, fallback al método normal
            var response = BaseQuery != null 
                ? await apiService!.LoadDataAsync(args, BaseQuery)
                : await apiService!.LoadDataAsync(args);
            
            if (response.Success && response.Data != null)
            {
                entities = response.Data.Data;
                totalCount = response.Data.TotalCount;
            }
            else
            {
                entities = new List<T>();
                totalCount = 0;
            }
        }
    }

    private async Task LoadDataWithSearchAndSelectOptimization(LoadDataArgs args, SearchRequest searchRequest)
    {
        try
        {
            // Agregar Select string al SearchRequest
            var selectString = BuildSelectString();
            
            // Asegurarse de que BaseQuery tenga el Select
            if (searchRequest.BaseQuery != null)
            {
                searchRequest.BaseQuery.Select = selectString;
            }
            else
            {
                searchRequest.BaseQuery = new QueryRequest { Select = selectString };
            }
            
            // Ejecutar búsqueda con select usando endpoint search-select-paged
            var response = await apiService!.SearchSelectPagedAsync(searchRequest);
            
            if (response.Success && response.Data != null)
            {
                // Los datos vienen como List<object> del endpoint search-select
                // Necesitamos convertirlos de vuelta a T usando reflection
                entities = ConvertSelectResultsToEntityType(response.Data.Data);
                totalCount = response.Data.TotalCount;
            }
            else
            {
                entities = new List<T>();
                totalCount = 0;
            }
        }
        catch (Exception)
        {
            // Si falla la búsqueda con select, fallback a búsqueda normal
            var response = await apiService!.SearchPagedAsync(searchRequest);
            
            if (response.Success && response.Data != null)
            {
                entities = response.Data.Data;
                totalCount = response.Data.TotalCount;
            }
            else
            {
                entities = new List<T>();
                totalCount = 0;
            }
        }
    }

    private QueryRequest ConvertLoadDataArgsToQueryRequestWithSelect(LoadDataArgs args, string selectString)
    {
        var queryRequest = new QueryRequest
        {
            Select = selectString,
            Skip = args.Skip,
            Take = args.Top
        };

        // Aplicar filtros
        if (args.Filters != null && args.Filters.Any())
        {
            var filters = args.Filters.Select(ConvertRadzenFilterToString).Where(f => !string.IsNullOrEmpty(f));
            if (filters.Any())
            {
                queryRequest.Filter = string.Join(" && ", filters);
            }
        }

        // Aplicar ordenamiento
        if (args.Sorts != null && args.Sorts.Any())
        {
            var sorts = args.Sorts.Select(ConvertRadzenSortToString).Where(s => !string.IsNullOrEmpty(s));
            if (sorts.Any())
            {
                queryRequest.OrderBy = string.Join(", ", sorts);
            }
        }

        return queryRequest;
    }

    private List<T> ConvertSelectResultsToEntityType(List<object> selectResults)
    {
        var result = new List<T>();
        
        Console.WriteLine($"[DEBUG] Convirtiendo {selectResults.Count} elementos de select results");
        
        foreach (var item in selectResults)
        {
            try
            {
                T? entity = default;
                
                if (item is System.Text.Json.JsonElement jsonElement)
                {
                    entity = CreatePartialEntity(jsonElement);
                }
                else if (item is string jsonString)
                {
                    var jsonElem = JsonSerializer.Deserialize<JsonElement>(jsonString);
                    entity = CreatePartialEntity(jsonElem);
                }
                else
                {
                    // Intentar serializar y deserializar el objeto
                    var json = JsonSerializer.Serialize(item);
                    var jsonElem = JsonSerializer.Deserialize<JsonElement>(json);
                    entity = CreatePartialEntity(jsonElem);
                }

                if (entity != null)
                {
                    result.Add(entity);
                    Console.WriteLine($"[DEBUG] Entidad convertida exitosamente");
                }
                else
                {
                    Console.WriteLine($"[DEBUG] No se pudo convertir entidad");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Error convirtiendo elemento: {ex.Message}");
            }
        }
        
        Console.WriteLine($"[DEBUG] Resultado final: {result.Count} entidades convertidas");
        return result;
    }

    private T? CreatePartialEntity(JsonElement jsonElement)
    {
        try
        {
            var entity = Activator.CreateInstance<T>();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            Console.WriteLine($"[DEBUG] Creando entidad parcial para tipo {typeof(T).Name}");

            foreach (var property in properties)
            {
                // Buscar la propiedad tanto por nombre exacto como por nombre en minúsculas (JSON camelCase)
                var propertyFound = false;
                JsonElement propertyValue = default;

                if (jsonElement.TryGetProperty(property.Name, out propertyValue))
                {
                    propertyFound = true;
                }
                else if (jsonElement.TryGetProperty(char.ToLowerInvariant(property.Name[0]) + property.Name.Substring(1), out propertyValue))
                {
                    propertyFound = true;
                }

                if (propertyFound)
                {
                    try
                    {
                        object? value = null;

                        // Manejar diferentes tipos de datos
                        if (property.PropertyType == typeof(string))
                        {
                            value = propertyValue.GetString();
                        }
                        else if (property.PropertyType == typeof(Guid) || property.PropertyType == typeof(Guid?))
                        {
                            if (propertyValue.ValueKind == JsonValueKind.String && Guid.TryParse(propertyValue.GetString(), out var guid))
                            {
                                value = guid;
                            }
                        }
                        else if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
                        {
                            if (propertyValue.ValueKind == JsonValueKind.True || propertyValue.ValueKind == JsonValueKind.False)
                            {
                                value = propertyValue.GetBoolean();
                            }
                        }
                        else if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
                        {
                            if (propertyValue.ValueKind == JsonValueKind.String && DateTime.TryParse(propertyValue.GetString(), out var date))
                            {
                                value = date;
                            }
                        }
                        else
                        {
                            // Para otros tipos, usar deserialización JSON
                            value = JsonSerializer.Deserialize(propertyValue.GetRawText(), property.PropertyType);
                        }

                        if (value != null)
                        {
                            property.SetValue(entity, value);
                            Console.WriteLine($"[DEBUG] Propiedad {property.Name} establecida con valor {value}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DEBUG] Error estableciendo propiedad {property.Name}: {ex.Message}");
                    }
                }
            }

            return entity;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Error creando entidad parcial: {ex.Message}");
            return default(T);
        }
    }

    private string ConvertRadzenFilterToString(FilterDescriptor filter)
    {
        if (filter == null || string.IsNullOrEmpty(filter.Property)) 
            return string.Empty;

        var property = filter.Property;
        var value = filter.FilterValue?.ToString();
        
        return filter.FilterOperator switch
        {
            FilterOperator.Equals => $"{property} == \"{value}\"",
            FilterOperator.NotEquals => $"{property} != \"{value}\"",
            FilterOperator.Contains => $"{property}.Contains(\"{value}\")",
            FilterOperator.DoesNotContain => $"!{property}.Contains(\"{value}\")",
            FilterOperator.StartsWith => $"{property}.StartsWith(\"{value}\")",
            FilterOperator.EndsWith => $"{property}.EndsWith(\"{value}\")",
            FilterOperator.GreaterThan => $"{property} > {value}",
            FilterOperator.GreaterThanOrEquals => $"{property} >= {value}",
            FilterOperator.LessThan => $"{property} < {value}",
            FilterOperator.LessThanOrEquals => $"{property} <= {value}",
            FilterOperator.IsNull => $"{property} == null",
            FilterOperator.IsNotNull => $"{property} != null",
            FilterOperator.IsEmpty => $"string.IsNullOrEmpty({property})",
            FilterOperator.IsNotEmpty => $"!string.IsNullOrEmpty({property})",
            _ => string.Empty
        };
    }

    private string ConvertRadzenSortToString(SortDescriptor sort)
    {
        if (sort == null || string.IsNullOrEmpty(sort.Property))
            return string.Empty;

        var direction = sort.SortOrder == SortOrder.Descending ? " desc" : "";
        return $"{sort.Property}{direction}";
    }

    #endregion

    #region Private Methods - Actions

    private async Task HandleEdit(T item)
    {
        if (OnEdit.HasDelegate)
        {
            await OnEdit.InvokeAsync(item);
        }
    }

    private async Task HandleDelete(T item)
    {
        if (OnDelete.HasDelegate)
        {
            var confirm = await DialogService.Confirm(
                "¿Está seguro que desea eliminar este registro?", 
                "Confirmar eliminación", 
                new ConfirmOptions { OkButtonText = "Sí", CancelButtonText = "No" }
            );

            if (confirm == true)
            {
                await OnDelete.InvokeAsync(item);
                await Reload();
            }
        }
    }

    #endregion

    #region Private Methods - Search

    private List<string> GetEffectiveSearchFields()
    {
        // 1. Si hay campos custom especificados, usarlos
        if (CustomSearchFields?.Any() == true)
        {
            return CustomSearchFields;
        }

        // 2. Si hay SearchFields legacy, usarlos
        if (SearchFields?.Any() == true)
        {
            return SearchFields;
        }

        // 3. Si hay input manual de campos, usarlo
        if (!string.IsNullOrWhiteSpace(currentSearchFieldsInput))
        {
            return currentSearchFieldsInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        // 4. Auto-detectar campos si está habilitado
        if (AutoDetectSearchFields)
        {
            return GetAutoDetectedSearchFields();
        }

        // 5. Por defecto, usar todos los campos visibles
        return GetVisibleFieldNames();
    }

    private List<string> GetAutoDetectedSearchFields()
    {
        var fields = new List<string>();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            // Excluir campos que no son apropiados para búsqueda
            if (ExcludeProperties?.Contains(prop.Name) == true)
                continue;

            // Si SearchOnlyStringAndNumeric está habilitado, filtrar por tipo
            if (SearchOnlyStringAndNumeric)
            {
                if (IsStringOrNumericType(prop.PropertyType))
                {
                    fields.Add(prop.Name);
                }
            }
            else
            {
                // Incluir todos los tipos simples
                if (IsSimpleProperty(prop.Name))
                {
                    fields.Add(prop.Name);
                }
            }
        }

        return fields;
    }

    private bool IsStringOrNumericType(Type type)
    {
        // Manejar tipos nullable
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        
        return underlyingType == typeof(string) ||
               underlyingType == typeof(int) ||
               underlyingType == typeof(long) ||
               underlyingType == typeof(decimal) ||
               underlyingType == typeof(double) ||
               underlyingType == typeof(float) ||
               underlyingType == typeof(short) ||
               underlyingType == typeof(byte);
    }

    private SearchRequest BuildSearchRequest(LoadDataArgs args, List<string> searchFields)
    {
        // Crear base query a partir de LoadDataArgs
        var baseQuery = new QueryRequest
        {
            Skip = args.Skip,
            Take = args.Top
        };

        // Aplicar filtros de columna (si existen)
        if (args.Filters != null && args.Filters.Any())
        {
            var filters = args.Filters.Select(ConvertRadzenFilterToString).Where(f => !string.IsNullOrEmpty(f));
            if (filters.Any())
            {
                baseQuery.Filter = string.Join(" && ", filters);
            }
        }

        // Aplicar ordenamiento
        if (args.Sorts != null && args.Sorts.Any())
        {
            var sorts = args.Sorts.Select(ConvertRadzenSortToString).Where(s => !string.IsNullOrEmpty(s));
            if (sorts.Any())
            {
                baseQuery.OrderBy = string.Join(", ", sorts);
            }
        }

        // Si hay BaseQuery personalizado, integrarlo
        if (BaseQuery != null)
        {
            // TODO: Integrar BaseQuery con baseQuery
            // Por ahora usamos baseQuery tal como está
        }

        // Crear SearchRequest
        var searchRequest = new SearchRequest
        {
            SearchTerm = searchTerm,
            SearchFields = searchFields.ToArray(),
            BaseQuery = baseQuery,
            Skip = args.Skip,
            Take = args.Top
        };

        return searchRequest;
    }

    private async Task OnSearchSubmit()
    {
        if (OnSearch.HasDelegate)
        {
            await OnSearch.InvokeAsync(searchTerm);
        }
        else
        {
            // Búsqueda automática: recargar grid usando el sistema de búsqueda inteligente
            // Esto activará LoadDataWithSearch en lugar de LoadData normal
            await FirstPage();
        }
    }

    private async Task OnSearchKeyPress(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await OnSearchSubmit();
        }
    }

    #endregion

    #region Private Methods - Excel Export

    private async Task ExportToExcel()
    {
        try
        {
            // Primero verificar si hay callback personalizado
            if (OnCustomExcelExport.HasDelegate)
            {
                var context = new ExcelExportContext<T>
                {
                    LastLoadDataArgs = lastLoadDataArgs,
                    VisibleColumns = GetVisibleColumns(),
                    CurrentEntities = entities?.ToList() ?? new List<T>(),
                    TotalCount = totalCount,
                    SearchTerm = searchTerm,
                    ApiService = apiService,
                    FileDownloadService = FileDownloadService,
                    DefaultFileName = string.IsNullOrEmpty(ExcelFileName) 
                        ? $"{typeof(T).Name}_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                        : ExcelFileName
                };
                
                await OnCustomExcelExport.InvokeAsync(context);
            }
            else if (OnExcelExport.HasDelegate)
            {
                await OnExcelExport.InvokeAsync();
            }
            else if (apiService != null && lastLoadDataArgs != null)
            {
                // Exportación automática
                var columns = ExcelColumns ?? GetVisibleColumns();
                
                // Limpiar las columnas para que sean serializables (quitar ValueTransform)
                var serializableColumns = columns.Select(c => new ExcelColumnConfig
                {
                    PropertyPath = c.PropertyPath,
                    DisplayName = c.DisplayName,
                    Format = c.Format,
                    CustomFormat = c.CustomFormat,
                    Width = c.Width,
                    Bold = c.Bold,
                    Alignment = c.Alignment,
                    WrapText = c.WrapText,
                    BackgroundColor = c.BackgroundColor,
                    TextColor = c.TextColor,
                    Order = c.Order,
                    Visible = c.Visible,
                    Comment = c.Comment
                    // ValueTransform se omite intencionalmente porque no es serializable
                }).ToList();
                
                var fileName = string.IsNullOrEmpty(ExcelFileName) 
                    ? $"{typeof(T).Name}_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                    : ExcelFileName;

                await apiService.DownloadExcelAsync(
                    lastLoadDataArgs, 
                    FileDownloadService, 
                    serializableColumns, 
                    fileName
                );
            }
            else
            {
                await DialogService.Alert("No hay datos para exportar", "Información");
            }
        }
        catch (Exception ex)
        {
            await DialogService.Alert($"Error al exportar: {ex.Message}", "Error");
        }
    }

    #endregion

    #region Private Methods - UI Layout

    private int GetSearchColumnSize()
    {
        // Ajustar tamaño considerando vista y botones
        if (HasViewSelector())
        {
            return 6; // Espacio medio cuando hay vista
        }
        
        // Sin vista, calcular basado en botones
        var buttonCount = (ShowRefreshButton ? 1 : 0) + (ShowAutoRefresh ? 1 : 0) + (ShowExcelExport ? 1 : 0) + (ShowColumnConfig ? 1 : 0);
        return buttonCount switch
        {
            0 => 12,
            1 => 10, 
            2 => 8,
            3 => 6,
            _ => 6
        };
    }

    private int GetButtonsColumnSize()
    {
        // Ajustar tamaño según elementos presentes
        if (HasViewSelector() && ShowSearchBar)
        {
            return 3; // Mínimo cuando hay vista y búsqueda
        }
        else if (HasViewSelector() || ShowSearchBar)
        {
            return 4; // Medio cuando hay uno u otro
        }
        return 12; // Toda la fila cuando solo hay botones
    }

    #endregion

    #region Private Methods - Column Generation

    private PropertyInfo[] GetAutoColumns()
    {
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        if (IncludeProperties?.Any() == true)
        {
            properties = properties.Where(p => IncludeProperties.Contains(p.Name)).ToArray();
        }
        else if (ExcludeProperties?.Any() == true)
        {
            properties = properties.Where(p => !ExcludeProperties.Contains(p.Name)).ToArray();
        }
        
        return properties;
    }

    private string GetDisplayName(string propertyName)
    {
        // Convertir PascalCase a texto con espacios
        return System.Text.RegularExpressions.Regex.Replace(propertyName, "(\\B[A-Z])", " $1");
    }

    private double ConvertToExcelWidth(string? width)
    {
        if (string.IsNullOrEmpty(width)) return 15;
        
        if (width.EndsWith("px"))
        {
            if (double.TryParse(width.Replace("px", ""), out var pixels))
            {
                return pixels / 7;
            }
        }
        else if (width.EndsWith("%"))
        {
            if (double.TryParse(width.Replace("%", ""), out var percentage))
            {
                return Math.Max(10, percentage / 5);
            }
        }
        else if (double.TryParse(width, out var directValue))
        {
            return directValue;
        }
        
        return 15;
    }

    private void ApplyColumnSpecificFormatting(ExcelColumnConfig column)
    {
        switch (column.PropertyPath.ToLower())
        {
            case "active":
                column.DisplayName = "Estado";
                // ValueTransform no se asigna aquí porque no es serializable
                // El backend manejará la transformación basándose en Format
                column.Format = ExcelFormat.ActiveInactive;
                column.Width = 15;
                break;
                
            case "fechacreacion":
            case "fechamodificacion":
            case "fecha":
                column.CustomFormat = "dd/mm/yyyy";
                column.Width = 18;
                break;
                
            case "nombre":
            case "name":
                column.Width = 20;
                break;
                
            case "descripcion":
            case "description":
                column.Width = 30;
                break;
        }
    }

    #endregion

    #region Private Methods - Auto Refresh

    private async Task OnSplitButtonClick(RadzenSplitButtonItem item, string buttonName = "AutoRefresh")
    {
        if (item != null && item.Value != null)
        {
            // Item del dropdown seleccionado
            if (int.TryParse(item.Value.ToString(), out var interval))
            {
                await SetAutoRefresh(interval);
            }
        }
        else
        {
            // Click en el botón principal
            await ManualRefresh();
        }
    }

    private async Task ManualRefresh()
    {
        // Si hay auto-refresh activo, resetear el temporizador
        if (currentAutoRefreshInterval > 0)
        {
            autoRefreshCountdown = currentAutoRefreshInterval;
        }
        
        if (OnRefresh.HasDelegate)
        {
            await OnRefresh.InvokeAsync();
        }
        else
        {
            await Reload();
        }
    }

    private async Task SetAutoRefresh(int intervalSeconds)
    {
        // Detener timers existentes
        StopAutoRefresh();

        if (intervalSeconds > 0)
        {
            currentAutoRefreshInterval = intervalSeconds;
            autoRefreshCountdown = intervalSeconds;

            // Timer para el countdown (cada segundo)
            countdownTimer = new Timer(async _ =>
            {
                autoRefreshCountdown--;
                if (autoRefreshCountdown <= 0)
                {
                    autoRefreshCountdown = currentAutoRefreshInterval;
                    await InvokeAsync(async () =>
                    {
                        await ManualRefresh();
                        StateHasChanged();
                    });
                }
                await InvokeAsync(StateHasChanged);
            }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }
    }

    private void StopAutoRefresh()
    {
        currentAutoRefreshInterval = 0;
        autoRefreshCountdown = 0;
        autoRefreshTimer?.Dispose();
        countdownTimer?.Dispose();
        autoRefreshTimer = null;
        countdownTimer = null;
    }

    private string GetRefreshText()
    {
        if (currentAutoRefreshInterval > 0)
        {
            return $"Actualizar ({autoRefreshCountdown}s)";
        }
        return RefreshButtonText;
    }

    private string GetAutoRefreshLabel(int seconds)
    {
        if (seconds <= 0) return "Nunca";
        
        // Calcular horas, minutos y segundos
        var hours = seconds / 3600;
        var minutes = (seconds % 3600) / 60;
        var remainingSeconds = seconds % 60;
        
        var parts = new List<string>();
        
        // Agregar horas si existen
        if (hours > 0)
        {
            parts.Add(hours == 1 ? "1 hora" : $"{hours} horas");
        }
        
        // Agregar minutos si existen
        if (minutes > 0)
        {
            parts.Add(minutes == 1 ? "1 minuto" : $"{minutes} minutos");
        }
        
        // Agregar segundos si existen (y no hay horas para mantener legibilidad)
        if (remainingSeconds > 0 && hours == 0)
        {
            parts.Add(remainingSeconds == 1 ? "1 segundo" : $"{remainingSeconds} segundos");
        }
        
        // Si no hay partes (seconds = 0), retornar "Nunca"
        if (!parts.Any()) return "Nunca";
        
        // Unir las partes con "y" si hay múltiples
        var result = parts.Count switch
        {
            1 => parts[0],
            2 => $"{parts[0]} y {parts[1]}",
            _ => $"{string.Join(", ", parts.Take(parts.Count - 1))} y {parts.Last()}"
        };
        
        return $"Cada {result}";
    }

    #endregion

    #region Private Methods - Column Configuration

    private async Task OpenColumnConfigModal()
    {
        try
        {
            // Construir lista de columnas disponibles
            var columnItems = BuildColumnVisibilityItems();
            
            // Obtener path actual para localStorage
            currentPath = GetCurrentPath();
            
            // Abrir modal usando DialogService
            var result = await DialogService.OpenAsync<ColumnConfigDialog>("Configurar Columnas",
                new Dictionary<string, object>
                {
                    { "ColumnItems", columnItems },
                    { "CurrentPath", currentPath }
                },
                new DialogOptions 
                { 
                    Width = "500px", 
                    Height = "600px",
                    Resizable = true,
                    Draggable = true
                });

            // Si se aplicaron cambios, actualizar grid
            if (result != null && result is List<ColumnVisibilityItem>)
            {
                var updatedItems = (List<ColumnVisibilityItem>)result;
                await ApplyColumnVisibility(updatedItems);
            }
        }
        catch (Exception ex)
        {
            await DialogService.Alert($"Error abriendo configuración: {ex.Message}", "Error");
        }
    }

    private List<ColumnVisibilityItem> BuildColumnVisibilityItems()
    {
        var items = new List<ColumnVisibilityItem>();

        // Obtener columnas del grid si están disponibles
        if (grid?.ColumnsCollection != null)
        {
            foreach (var column in grid.ColumnsCollection)
            {
                if (!string.IsNullOrEmpty(column.Property))
                {
                    items.Add(new ColumnVisibilityItem
                    {
                        Property = column.Property,
                        DisplayName = column.Title ?? GetDisplayName(column.Property),
                        IsVisible = column.Visible,
                        ColumnType = ColumnSourceType.RenderFragment
                    });
                }
            }
        }
        else if (ColumnConfigs != null)
        {
            // Si no hay grid disponible, usar ColumnConfigs
            foreach (var config in ColumnConfigs)
            {
                items.Add(new ColumnVisibilityItem
                {
                    Property = config.Property,
                    DisplayName = config.Title ?? GetDisplayName(config.Property),
                    IsVisible = config.Visible,
                    ColumnType = ColumnSourceType.ColumnConfig
                });
            }
        }
        else
        {
            // Auto-generadas
            var properties = GetAutoColumns();
            foreach (var prop in properties)
            {
                items.Add(new ColumnVisibilityItem
                {
                    Property = prop.Name,
                    DisplayName = GetDisplayName(prop.Name),
                    IsVisible = true,
                    ColumnType = ColumnSourceType.Auto
                });
            }
        }

        return items;
    }

    private async Task ApplyColumnVisibility(List<ColumnVisibilityItem> columnItems)
    {
        // Aplicar cambios dependiendo del tipo de columnas
        if (grid?.ColumnsCollection != null)
        {
            foreach (var column in grid.ColumnsCollection)
            {
                if (!string.IsNullOrEmpty(column.Property))
                {
                    var item = columnItems.FirstOrDefault(i => i.Property == column.Property);
                    if (item != null)
                    {
                        column.Visible = item.IsVisible;
                    }
                }
            }
        }
        else if (ColumnConfigs != null)
        {
            foreach (var config in ColumnConfigs)
            {
                var item = columnItems.FirstOrDefault(i => i.Property == config.Property);
                if (item != null)
                {
                    config.Visible = item.IsVisible;
                }
            }
        }

        // Forzar re-render del grid
        StateHasChanged();
        
        // Recargar datos con la nueva configuración de columnas
        // Esto hace que se recalcule el Select string y se ejecute una nueva query
        Console.WriteLine("[DEBUG] Recargando datos después de cambio de columnas");
        await Reload();
    }

    private async Task LoadColumnConfigFromLocalStorage()
    {
        try
        {
            currentPath = GetCurrentPath();
            if (string.IsNullOrEmpty(currentPath)) return;

            var storageKey = $"column_config_{currentPath}";
            var json = await JSRuntime.InvokeAsync<string>("localStorage.getItem", storageKey);
            
            if (!string.IsNullOrEmpty(json))
            {
                var config = JsonSerializer.Deserialize<Dictionary<string, bool>>(json);
                if (config != null)
                {
                    // Aplicar configuración a ColumnConfigs si existe
                    if (ColumnConfigs != null)
                    {
                        foreach (var columnConfig in ColumnConfigs)
                        {
                            if (config.TryGetValue(columnConfig.Property, out var isVisible))
                            {
                                columnConfig.Visible = isVisible;
                            }
                        }
                    }
                    // Para columnas RenderFragment, se aplicará después de que grid se renderice
                }
            }
        }
        catch (Exception)
        {
            // Si hay error cargando configuración, usar valores por defecto
        }
    }

    private async Task ApplyStoredColumnConfiguration()
    {
        try
        {
            if (grid?.ColumnsCollection == null) return;

            currentPath = GetCurrentPath();
            var storageKey = $"column_config_{currentPath}";
            var json = await JSRuntime.InvokeAsync<string>("localStorage.getItem", storageKey);
            
            if (!string.IsNullOrEmpty(json))
            {
                var config = JsonSerializer.Deserialize<Dictionary<string, bool>>(json);
                if (config != null)
                {
                    var hasChanges = false;
                    
                    foreach (var column in grid.ColumnsCollection)
                    {
                        if (!string.IsNullOrEmpty(column.Property) && 
                            config.TryGetValue(column.Property, out var isVisible))
                        {
                            if (column.Visible != isVisible)
                            {
                                column.Visible = isVisible;
                                hasChanges = true;
                            }
                        }
                    }
                    
                    if (hasChanges)
                    {
                        Console.WriteLine("[DEBUG] Aplicando configuración guardada y recargando datos");
                        StateHasChanged();
                        
                        // Esperar un poco para que se apliquen los cambios de visibilidad
                        await Task.Delay(100);
                        
                        // Recargar datos con la nueva configuración
                        await Reload();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Error aplicando configuración guardada: {ex.Message}");
        }
    }

    private string GetCurrentPath()
    {
        // Usar el tipo T para generar una clave única por entidad
        return typeof(T).Name.ToLower();
    }

    #endregion

    #region Private Methods - Select Optimization

    private bool ShouldUseSelectOptimization()
    {
        var shouldUse = EnableSelectOptimization && SelectOnlyVisibleColumns && HasVisibleColumnsConfigured();
        Console.WriteLine($"[DEBUG] ShouldUseSelectOptimization: {shouldUse} (EnableSelectOptimization: {EnableSelectOptimization}, SelectOnlyVisibleColumns: {SelectOnlyVisibleColumns}, HasVisibleColumnsConfigured: {HasVisibleColumnsConfigured()})");
        return shouldUse;
    }

    private bool HasVisibleColumnsConfigured()
    {
        // PRIORIDAD 1: Si hay ColumnConfigs, siempre usar optimización para traer solo campos necesarios
        if (ColumnConfigs != null && ColumnConfigs.Any())
        {
            var visibleConfigs = ColumnConfigs.Where(c => c.Visible).ToList();
            
            Console.WriteLine($"[DEBUG] ColumnConfigs detectadas - Total: {ColumnConfigs.Count}, Visible: {visibleConfigs.Count}");
            Console.WriteLine($"[DEBUG] Campos visibles: {string.Join(", ", visibleConfigs.Select(c => c.Property))}");
            
            // Si hay ColumnConfigs definidas, usar optimización para traer solo esos campos
            var shouldOptimize = visibleConfigs.Any();
            Console.WriteLine($"[DEBUG] ColumnConfigs optimization decision: {shouldOptimize}");
            return shouldOptimize;
        }
        
        // PRIORIDAD 2: Solo usar optimización si tenemos configuración personalizada de columnas del grid
        // y no todas las columnas están visibles (indicando que el usuario hizo una selección específica)
        if (grid?.ColumnsCollection != null)
        {
            var allColumns = grid.ColumnsCollection.Where(c => !string.IsNullOrEmpty(c.Property) && c.Property != "Actions").ToList();
            var visibleColumns = allColumns.Where(c => c.Visible).ToList();
            
            // Solo optimizar si hay columnas específicas visibles y algunas ocultas
            // (indicando que el usuario personalizó la vista)
            var hasCustomizedView = allColumns.Count > visibleColumns.Count && visibleColumns.Count > 0;
            
            Console.WriteLine($"[DEBUG] Grid columns - Total: {allColumns.Count}, Visible: {visibleColumns.Count}, Customized: {hasCustomizedView}");
            Console.WriteLine($"[DEBUG] Columnas disponibles: {string.Join(", ", allColumns.Select(c => $"{c.Property}({c.Visible})"))}");
            
            return hasCustomizedView;
        }

        // Si no hay configuración específica, no optimizar
        Console.WriteLine("[DEBUG] No hay configuración específica de columnas");
        return false;
    }

    private string BuildSelectString()
    {
        var fields = new HashSet<string>();

        // Siempre incluir campos obligatorios
        if (AlwaysSelectFields != null)
        {
            foreach (var field in AlwaysSelectFields)
            {
                fields.Add(field);
            }
        }

        // Agregar campos de columnas visibles
        var visibleFields = GetVisibleFieldNames();
        foreach (var field in visibleFields)
        {
            fields.Add(field);
        }

        // Si no hay campos visibles (solo obligatorios), retornar select básico
        if (visibleFields.Count == 0)
        {
            Console.WriteLine("[DEBUG] No hay columnas visibles específicas, usando select básico");
            return "new { Id }";
        }

        // Construir select string
        var fieldsStr = string.Join(", ", fields.OrderBy(f => f));
        var selectString = $"new {{ {fieldsStr} }}";
        
        Console.WriteLine($"[DEBUG] Select string generado: {selectString}");
        return selectString;
    }

    private List<string> GetVisibleFieldNames()
    {
        var fields = new List<string>();

        // PRIORIDAD 1: Si hay ColumnConfigs definidas, usarlas
        if (ColumnConfigs != null && ColumnConfigs.Any())
        {
            foreach (var config in ColumnConfigs.Where(c => c.Visible))
            {
                if (IsSimpleProperty(config.Property))
                {
                    fields.Add(config.Property);
                }
            }
            
            Console.WriteLine($"[DEBUG] Campos desde ColumnConfigs: {string.Join(", ", fields)}");
        }
        // PRIORIDAD 2: Si no hay ColumnConfigs, obtener del grid
        else if (grid?.ColumnsCollection != null)
        {
            // Solo obtener las columnas que realmente están visibles y configuradas por el usuario
            var userVisibleColumns = grid.ColumnsCollection.Where(c => 
                c.Visible && 
                !string.IsNullOrEmpty(c.Property) && 
                c.Property != "Actions" // Excluir columna de acciones
            ).ToList();

            foreach (var column in userVisibleColumns)
            {
                // Solo agregar propiedades simples (no templates complejos)
                if (IsSimpleProperty(column.Property!))
                {
                    fields.Add(column.Property!);
                }
            }

            Console.WriteLine($"[DEBUG] Campos desde Grid: {string.Join(", ", fields)}");
        }

        return fields.Distinct().ToList();
    }

    private bool IsSimpleProperty(string propertyName)
    {
        // Evitar propiedades complejas o de navegación
        var complexTypes = new[] { ".", "Usuario", "Modificado", "Creado" };
        return !complexTypes.Any(complex => propertyName.Contains(complex, StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Private Methods - Hybrid Column Management

    /// <summary>
    /// Obtiene la configuración de columnas efectiva combinando RenderFragment y ColumnConfigs
    /// Regla: Si una propiedad está definida en RenderFragment Columns, NO se incluye desde ColumnConfigs
    /// </summary>
    private List<ColumnConfig<T>> GetEffectiveColumnConfigs()
    {
        var effectiveConfigs = new List<ColumnConfig<T>>();
        
        // Si no hay ColumnConfigs, retornar lista vacía
        if (ColumnConfigs == null || !ColumnConfigs.Any())
            return effectiveConfigs;
            
        // Si no hay RenderFragmentProperties definidas, retornar todas las ColumnConfigs
        if (RenderFragmentProperties == null || !RenderFragmentProperties.Any())
        {
            return ColumnConfigs.ToList();
        }
        
        // Filtrar ColumnConfigs excluyendo las propiedades ya definidas en RenderFragment
        effectiveConfigs = ColumnConfigs
            .Where(config => !RenderFragmentProperties.Contains(config.Property))
            .ToList();
            
        return effectiveConfigs;
    }
    
    /// <summary>
    /// Verifica si debe usar la lógica de combinación de columnas
    /// </summary>
    private bool ShouldCombineColumns()
    {
        return CombineColumnsAndConfigs && 
               Columns != null && 
               ColumnConfigs != null && 
               ColumnConfigs.Any();
    }
    
    /// <summary>
    /// Obtiene las configuraciones finales ordenadas por índice SI O SI si ForceOrderByIndex es true
    /// </summary>
    private List<ColumnConfig<T>> GetFinalOrderedConfigs()
    {
        var configs = GetEffectiveColumnConfigs();
        
        if (!configs.Any())
            return configs;
            
        if (ForceOrderByIndex)
        {
            // Ordenar por Order SI O SI, asignando 999 como valor por defecto
            return configs.OrderBy(c => c.Order ?? 999).ToList();
        }
        
        return configs;
    }
    

    #endregion

    #region Private Methods - View Management

    private bool HasViewSelector()
    {
        return ViewConfigurations != null && ViewConfigurations.Count > 1;
    }

    private string GetCurrentViewDisplayName()
    {
        if (CurrentView == null) return "";
        
        var property = CurrentView.GetType().GetProperty(ViewDisplayNameProperty);
        return property?.GetValue(CurrentView)?.ToString() ?? "";
    }

    private async Task OnViewChangedInternal(object args)
    {
        if (OnViewChanged.HasDelegate)
        {
            await OnViewChanged.InvokeAsync(args);
        }
    }


    private int GetMainContentColumnSize()
    {
        return HasViewSelector() ? 8 : 12;
    }

    private int GetMainContentColumnSizeMD()
    {
        return HasViewSelector() ? 7 : 12;
    }

    #endregion

    public void Dispose()
    {
        StopAutoRefresh();
    }
}

public class ColumnConfig<T>
{
    public string Property { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Width { get; set; }
    public bool Sortable { get; set; } = true;
    public bool Filterable { get; set; } = true;
    public TextAlign TextAlign { get; set; } = TextAlign.Left;
    public bool Frozen { get; set; } = false;
    public bool Visible { get; set; } = true;
    public int? Order { get; set; }
    public RenderFragment<T>? Template { get; set; }
    
    /// <summary>
    /// Indica el origen de la columna: Auto, ColumnConfig, o RenderFragment
    /// </summary>
    public ColumnSourceType SourceType { get; set; } = ColumnSourceType.ColumnConfig;
    
    /// <summary>
    /// Si está definida en RenderFragment Columns, se excluye de ColumnConfigs automáticamente
    /// </summary>
    public bool IsDefinedInRenderFragment { get; set; } = false;
}

public enum ColumnSourceType
{
    Auto,           // Generada automáticamente por reflexión
    ColumnConfig,   // Definida en ColumnConfigs
    RenderFragment  // Definida en RenderFragment Columns
}

public class ExcelExportContext<T> where T : class
{
    public LoadDataArgs? LastLoadDataArgs { get; set; }
    public List<ExcelColumnConfig> VisibleColumns { get; set; } = new();
    public List<T> CurrentEntities { get; set; } = new();
    public int TotalCount { get; set; }
    public string SearchTerm { get; set; } = "";
    public BaseApiService<T>? ApiService { get; set; }
    public FileDownloadService? FileDownloadService { get; set; }
    public string DefaultFileName { get; set; } = "";
}

public enum SearchOperator
{
    Contains,    // Default - buscar texto contenido
    Equals,      // Búsqueda exacta
    StartsWith,  // Comienza con
    EndsWith     // Termina con
}