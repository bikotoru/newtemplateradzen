using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Models.Export;
using Frontend.Services;

namespace Frontend.Components.Base.Tables;

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
    
    public object? FilterValue { get; set; }
    public FilterOperator? FilterOperator { get; set; }
    
    // 🔥 NUEVAS PROPIEDADES PARA FILTROS HÍBRIDOS
    /// <summary>
    /// Indica si esta columna debe usar checkbox filter (FilterMode.CheckBoxList) en lugar del filtro tradicional
    /// </summary>
    public bool UseCheckBoxFilter { get; set; } = false;
    
    /// <summary>
    /// FilterMode específico para esta columna. Si es null, usa el FilterMode global del grid
    /// </summary>
    public FilterMode? ColumnFilterMode { get; set; } = null;
    
    /// <summary>
    /// Para propiedades relacionadas, especifica qué propiedad usar para mostrar en el filtro
    /// Ejemplo: Para "Category.Name", RelatedDisplayProperty sería "Name"
    /// </summary>
    public string? RelatedDisplayProperty { get; set; }
    
    /// <summary>
    /// Para propiedades relacionadas, especifica la entidad relacionada
    /// Ejemplo: Para "Category.Name", RelatedEntityProperty sería "Category"
    /// </summary>
    public string? RelatedEntityProperty { get; set; }
    
    /// <summary>
    /// Configuración específica para checkbox filters
    /// </summary>
    public CheckboxFilterConfig? CheckboxFilterOptions { get; set; }
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
    Contains,
    Equals,
    StartsWith,
    EndsWith
}

public interface IViewConfiguration<T> where T : class
{
    string DisplayName { get; set; }
    QueryBuilder<T>? QueryBuilder { get; set; }
    List<ColumnConfig<T>>? ColumnConfigs { get; set; }
}

public class ViewConfiguration<T> : IViewConfiguration<T> where T : class
{
    public string DisplayName { get; set; } = "";
    public QueryBuilder<T>? QueryBuilder { get; set; }
    public List<ColumnConfig<T>>? ColumnConfigs { get; set; }

    public ViewConfiguration()
    {
        QueryBuilder = null!;
    }

    public ViewConfiguration(string displayName, QueryBuilder<T>? queryBuilder, List<ColumnConfig<T>>? columnConfigs = null)
    {
        DisplayName = displayName;
        QueryBuilder = queryBuilder;
        ColumnConfigs = columnConfigs;
    }
}

/// <summary>
/// Configuración específica para filtros de checkbox
/// </summary>
public class CheckboxFilterConfig
{
    /// <summary>
    /// Tiempo de caché para valores únicos (para mejorar performance)
    /// </summary>
    public TimeSpan CacheTimeout { get; set; } = TimeSpan.FromMinutes(5);
    
    /// <summary>
    /// Máximo número de items a mostrar en el filtro
    /// </summary>
    public int MaxItems { get; set; } = 100;
    
    /// <summary>
    /// Habilita búsqueda dentro del filtro de checkbox
    /// </summary>
    public bool EnableSearch { get; set; } = true;
    
    /// <summary>
    /// Carga valores solo cuando se abre el filtro (lazy loading)
    /// </summary>
    public bool LoadOnDemand { get; set; } = true;
    
    /// <summary>
    /// Habilita virtualización para listas grandes
    /// </summary>
    public bool EnableVirtualization { get; set; } = true;
    
    /// <summary>
    /// Incluye valores null/empty en la lista de checkboxes
    /// </summary>
    public bool IncludeNullValues { get; set; } = true;
    
    /// <summary>
    /// Texto a mostrar para valores null
    /// </summary>
    public string NullValueText { get; set; } = "(Vacío)";
}