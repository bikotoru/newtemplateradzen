using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using Shared.Models.Export;
using Frontend.Services;
using Shared.Models.QueryModels;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Frontend.Components.Base.Tables;

public partial class EntityTable<T> : ComponentBase, IDisposable where T : class
{
    [Inject] private IServiceProvider ServiceProvider { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private FileDownloadService FileDownloadService { get; set; } = null!;

    private RadzenDataGrid<T>? grid;
    private IEnumerable<T> entities;
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

    #endregion

    #region Parameters - Excel Export

    [Parameter] public bool ShowExcelExport { get; set; } = false;
    [Parameter] public EventCallback OnExcelExport { get; set; }
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

    protected override void OnInitialized()
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
                // Usar el servicio API si está disponible
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
            isLoading = false;
            StateHasChanged();
        }
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

    private async Task OnSearchSubmit()
    {
        if (OnSearch.HasDelegate)
        {
            await OnSearch.InvokeAsync(searchTerm);
        }
        else
        {
            // Búsqueda automática: recargar grid con filtro
            await FirstPage();
        }
    }

    #endregion

    #region Private Methods - Excel Export

    private async Task ExportToExcel()
    {
        try
        {
            if (OnExcelExport.HasDelegate)
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
        // Calcular el tamaño de la columna de búsqueda basado en cuántos botones se muestran
        var buttonCount = (ShowRefreshButton ? 1 : 0) + (ShowAutoRefresh ? 1 : 0) + (ShowExcelExport ? 1 : 0);
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
        return 12 - (ShowSearchBar ? GetSearchColumnSize() : 0);
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

    public void Dispose()
    {
        StopAutoRefresh();
    }

    #endregion
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
}