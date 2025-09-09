using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Models.Export;
using Frontend.Services;
using Frontend.Models;

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
    
    /// <summary>
    /// Expresión tipada para formatear el valor mostrado. Ejemplo: p => p.Organization?.Nombre ?? "Global"
    /// </summary>
    public Func<T, string>? FormatExpression { get; set; }
    
    public object? FilterValue { get; set; }
    public FilterOperator? FilterOperator { get; set; }
    
    /// <summary>
    /// Configuración de lookup para filtros de relaciones
    /// </summary>
    public IFilterLookup? FilterLookup { get; set; }
    
    /// <summary>
    /// Indica si esta columna representa una relación
    /// </summary>
    public bool IsRelationship { get; set; } = false;
    
    /// <summary>
    /// Nombre de la propiedad de navegación relacionada (ej: "Categoria" para "CategoriaId")
    /// </summary>
    public string? RelationshipProperty { get; set; }
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