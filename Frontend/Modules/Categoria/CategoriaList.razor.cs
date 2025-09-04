using Microsoft.AspNetCore.Components;
using Frontend.Components.Base.Tables;
using Shared.Models.Entities;
using Frontend.Services;
using Radzen;
using CategoriaEntity = Shared.Models.Entities.Categoria;

namespace Frontend.Modules.Categoria;

public partial class CategoriaList : ComponentBase
{
    [Inject] private CategoriaService CategoriaService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private QueryService QueryService { get; set; } = null!;

    private EntityTable<CategoriaEntity>? entityTable;
    private CategoriaViewManager viewManager = null!;
    private ViewConfiguration<CategoriaEntity> currentView = null!;
    
    // 🔥 VARIABLES PARA FILTROS HÍBRIDOS
    private bool enableHybridFilters = true;
    private FilterMode defaultFilterMode = FilterMode.CheckBoxList;
    
    // Opciones para el dropdown de FilterMode
    private readonly List<object> filterModeOptions = new()
    {
        new { Text = "CheckBox Lists", Value = FilterMode.CheckBoxList },
        new { Text = "Advanced Filters", Value = FilterMode.Advanced },
        new { Text = "Simple Filters", Value = FilterMode.Simple }
    };
    
    // Propiedad para EntityTable - ahora es type-safe
    private List<IViewConfiguration<CategoriaEntity>>? ViewConfigurationsTyped => viewManager?.ViewConfigurations?.Cast<IViewConfiguration<CategoriaEntity>>().ToList();
    
    protected override void OnInitialized()
    {
        Console.WriteLine("[CategoriaList] OnInitialized iniciado");
        
        viewManager = new CategoriaViewManager(QueryService);
        currentView = viewManager.GetDefaultView();
        
        Console.WriteLine($"[CategoriaList] ViewManager creado con {viewManager.ViewConfigurations.Count} vistas:");
        foreach (var view in viewManager.ViewConfigurations)
        {
            Console.WriteLine($"[CategoriaList]   Vista: {view.DisplayName} - {view.ColumnConfigs?.Count ?? 0} columnas");
        }
        
        Console.WriteLine($"[CategoriaList] Vista por defecto: {currentView?.DisplayName}");
        Console.WriteLine($"[CategoriaList] Vista por defecto tiene {currentView?.ColumnConfigs?.Count ?? 0} columnas:");
        
        if (currentView?.ColumnConfigs != null)
        {
            foreach (var col in currentView.ColumnConfigs)
            {
                Console.WriteLine($"[CategoriaList]   - {col.Property} ({col.Title}) - Visible: {col.Visible}, Order: {col.Order}");
            }
        }
        
        base.OnInitialized();
    }

    private void GoToCreate()
    {
        Navigation.NavigateTo("/categoria/formulario");
    }

    private async Task HandleEdit(CategoriaEntity categoria)
    {
        Navigation.NavigateTo($"/categoria/formulario/{categoria.Id}");
    }

    private async Task HandleDelete(CategoriaEntity categoria)
    {
        var response = await CategoriaService.DeleteAsync(categoria.Id);
        
        if (!response.Success)
        {
            await DialogService.Alert(
                response.Message ?? "Error al eliminar categoría", 
                "Error"
            );
        }
    }
    
    private async Task OnViewChanged(IViewConfiguration<CategoriaEntity> selectedView)
    {
        Console.WriteLine($"[CategoriaList] OnViewChanged llamado con vista: {selectedView?.DisplayName}");
        Console.WriteLine($"[CategoriaList] Vista anterior: {currentView?.DisplayName}");
        
        if (selectedView is ViewConfiguration<CategoriaEntity> viewConfig)
        {
            var previousView = currentView?.DisplayName;
            currentView = viewConfig;
            
            Console.WriteLine($"[CategoriaList] Vista cambiada de '{previousView}' a '{currentView.DisplayName}'");
            Console.WriteLine($"[CategoriaList] Nueva vista tiene {currentView.ColumnConfigs?.Count ?? 0} columnas:");
            
            if (currentView.ColumnConfigs != null)
            {
                foreach (var col in currentView.ColumnConfigs)
                {
                    Console.WriteLine($"[CategoriaList]   - {col.Property} ({col.Title}) - Visible: {col.Visible}, Order: {col.Order}");
                }
            }
            
            // Forzar reconstrucción completa del grid
            await InvokeAsync(StateHasChanged);
            Console.WriteLine("[CategoriaList] StateHasChanged ejecutado");
        }
        else
        {
            Console.WriteLine("[CategoriaList] ERROR: selectedView no es ViewConfiguration<CategoriaEntity>");
        }
    }
    
    /// <summary>
    /// Genera una clave única para forzar la reconstrucción del grid cuando cambie la configuración
    /// </summary>
    private string GetGridKey()
    {
        // Incluir el estado de filtros híbridos en la clave para forzar recreación
        return $"grid_{currentView?.DisplayName ?? "default"}_{enableHybridFilters}_{defaultFilterMode}";
    }
    
    /// <summary>
    /// 🔥 OBTIENE LA CONFIGURACIÓN DE COLUMNAS ACTUAL
    /// </summary>
    private List<ColumnConfig<CategoriaEntity>> GetCurrentColumnConfigs()
    {
        // Si los filtros híbridos están habilitados, usar la configuración híbrida
        if (enableHybridFilters)
        {
            return CategoriaConfig.HybridFilters.GetHybridFilterColumns();
        }
        
        // Si no, usar la configuración de la vista actual o una básica
        return currentView?.ColumnConfigs ?? CategoriaConfig.HybridFilters.GetTraditionalColumns();
    }
    
    /// <summary>
    /// 🔥 MANEJA EL CAMBIO DEL TOGGLE DE FILTROS HÍBRIDOS
    /// </summary>
    private async Task OnHybridFiltersToggled()
    {
        Console.WriteLine($"[CategoriaList] Hybrid filters toggled to: {enableHybridFilters}");
        
        // Forzar recreación del componente
        await InvokeAsync(StateHasChanged);
        
        // Limpiar cache cuando se cambia de modo
        if (entityTable != null)
        {
            await entityTable.ClearFilterCache();
        }
        
        // Mostrar notificación
        NotificationService.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Info,
            Summary = "Filtros Actualizados",
            Detail = enableHybridFilters 
                ? "Filtros híbridos habilitados. Ahora tienes CheckBox filters para algunos campos." 
                : "Filtros híbridos deshabilitados. Usando filtros tradicionales.",
            Duration = 4000
        });
    }
    
    /// <summary>
    /// 🔥 MANEJA EL CAMBIO DEL MODO DE FILTRO DEFAULT
    /// </summary>
    private async Task OnFilterModeChanged()
    {
        Console.WriteLine($"[CategoriaList] Default filter mode changed to: {defaultFilterMode}");
        
        // Forzar recreación del componente
        await InvokeAsync(StateHasChanged);
        
        // Limpiar cache
        if (entityTable != null)
        {
            await entityTable.ClearFilterCache();
        }
    }
    
    /// <summary>
    /// 🔥 LIMPIA EL CACHE DE FILTROS
    /// </summary>
    private async Task ClearFilterCache()
    {
        if (entityTable != null)
        {
            await entityTable.ClearFilterCache();
            
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Cache Limpiado",
                Detail = "El cache de filtros ha sido limpiado exitosamente.",
                Duration = 3000
            });
            
            Console.WriteLine("[CategoriaList] Filter cache cleared");
        }
    }
    
    /// <summary>
    /// 🔥 MUESTRA ESTADÍSTICAS DEL CACHE
    /// </summary>
    private async Task ShowCacheStats()
    {
        if (entityTable != null)
        {
            var stats = await entityTable.GetFilterCacheStats();
            
            var message = $@"
📊 Estadísticas del Cache de Filtros:

• Total de entradas: {stats.totalEntries}
• Entradas expiradas: {stats.expiredEntries}  
• Entrada más antigua: {stats.oldestEntry.TotalMinutes:F1} minutos

💡 El cache mejora el rendimiento evitando consultas repetidas.
🔄 Las entradas expiradas se limpian automáticamente.";

            await DialogService.Alert(message, "📈 Estadísticas de Cache");
            
            Console.WriteLine($"[CategoriaList] Cache stats - Total: {stats.totalEntries}, Expired: {stats.expiredEntries}");
        }
    }

    /// <summary>
    /// 🔥 HANDLER PERSONALIZADO PARA FILTROS DE CHECKBOX
    /// </summary>
    private async Task HandleCustomFilterData(DataGridLoadColumnFilterDataEventArgs<CategoriaEntity> args)
    {
        var propertyName = args.Column?.GetFilterProperty();
        Console.WriteLine($"[CategoriaList] Loading filter data for: {propertyName}");
        
        try
        {
            switch (propertyName)
            {
                case nameof(CategoriaEntity.Active):
                    // Personalizar valores booleanos con emojis y texto descriptivo
                    var boolOptions = new[]
                    {
                        new { Value = true, Text = "✅ Categorías Activas" },
                        new { Value = false, Text = "❌ Categorías Inactivas" }
                    };
                    
                    // Aplicar filtro de búsqueda si existe
                    if (!string.IsNullOrEmpty(args.Filter))
                    {
                        var filtered = boolOptions.Where(o => 
                            o.Text.Contains(args.Filter, StringComparison.OrdinalIgnoreCase))
                            .ToArray();
                        args.Data = filtered;
                        args.Count = filtered.Length;
                    }
                    else
                    {
                        args.Data = boolOptions;
                        args.Count = boolOptions.Length;
                    }
                    
                    Console.WriteLine($"[CategoriaList] Custom boolean filter loaded: {args.Count} items");
                    break;
                    
                case nameof(CategoriaEntity.Nombre):
                    // Para nombres, usar datos mock personalizados
                    Console.WriteLine("[CategoriaList] Loading category names with custom mock data...");
                    
                    var nameOptions = new[]
                    {
                        new { Nombre = "Electrónicos" },
                        new { Nombre = "Ropa y Accesorios" },
                        new { Nombre = "Hogar y Jardín" },
                        new { Nombre = "Deportes" },
                        new { Nombre = "Libros" }
                    };
                    
                    // Aplicar filtro de búsqueda si existe
                    if (!string.IsNullOrEmpty(args.Filter))
                    {
                        var filtered = nameOptions.Where(o => 
                            o.Nombre.Contains(args.Filter, StringComparison.OrdinalIgnoreCase))
                            .ToArray();
                        args.Data = filtered;
                        args.Count = filtered.Length;
                    }
                    else
                    {
                        args.Data = nameOptions;
                        args.Count = nameOptions.Length;
                    }
                    
                    Console.WriteLine($"[CategoriaList] Custom name filter loaded: {args.Count} items");
                    break;
                    
                case "Organization.Nombre":
                    // Para organizaciones, usar lógica automática
                    Console.WriteLine("[CategoriaList] Loading organizations with auto-generation...");
                    return;
                    
                default:
                    // Para otros campos, usar auto-generación de EntityTable
                    return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CategoriaList] Error in HandleCustomFilterData: {ex.Message}");
            
            // En caso de error, mostrar notificación y usar auto-generación
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Error en Filtro",
                Detail = $"Error cargando filtro para {propertyName}. Usando valores por defecto.",
                Duration = 3000
            });
        }
    }

    /// <summary>
    /// Obtiene las clases CSS para la tarjeta de control de filtros
    /// </summary>
    private string GetCardCssClass()
    {
        return enableHybridFilters 
            ? "rz-mb-3 hybrid-filters-active" 
            : "rz-mb-3";
    }
}