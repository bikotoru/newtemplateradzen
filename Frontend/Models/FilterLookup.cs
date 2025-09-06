using Frontend.Services;
using Radzen;

namespace Frontend.Models;

/// <summary>
/// Configuración de columna para un lookup
/// </summary>
public class LookupColumnConfig
{
    public string Property { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Width { get; set; }
    public TextAlign TextAlign { get; set; } = TextAlign.Left;
    public bool Visible { get; set; } = true;
}

/// <summary>
/// Interfaz base para FilterLookup
/// </summary>
public interface IFilterLookup
{
    string ValueProperty { get; }
    string DisplayProperty { get; }
    LookupColumnConfig[] DisplayColumns { get; }
    string[] SearchColumns { get; }
    Type EntityType { get; }
    object? ApiService { get; }
    Task<List<object>> LoadDataAsync(string? searchTerm = null, int skip = 0, int take = 20);
    string GetDisplayText(object item);
    object? GetValue(object item);
    int TotalCount { get; }
}

/// <summary>
/// Configuración de FilterLookup para relaciones
/// </summary>
/// <typeparam name="T">Tipo de la entidad relacionada</typeparam>
public class FilterLookup<T> : IFilterLookup where T : class
{
    /// <summary>
    /// Columnas a mostrar en el lookup
    /// </summary>
    public LookupColumnConfig[] DisplayColumns { get; set; } = Array.Empty<LookupColumnConfig>();

    /// <summary>
    /// Columnas por las que se puede buscar
    /// </summary>
    public string[] SearchColumns { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Propiedad que contiene el valor (por defecto "Id")
    /// </summary>
    public string ValueProperty { get; set; } = "Id";

    /// <summary>
    /// Propiedad principal a mostrar (por defecto "Nombre")
    /// </summary>
    public string DisplayProperty { get; set; } = "Nombre";

    /// <summary>
    /// Tipo de la entidad
    /// </summary>
    public Type EntityType => typeof(T);

    /// <summary>
    /// Servicio API como object para la interfaz
    /// </summary>
    public object? ApiService => _apiService;

    /// <summary>
    /// Servicio API para cargar los datos
    /// </summary>
    private BaseApiService<T>? _apiService;
    
    public BaseApiService<T>? Service
    {
        get => _apiService;
        set => _apiService = value;
    }

    /// <summary>
    /// Datos estáticos (alternativa al ApiService)
    /// </summary>
    public List<T>? StaticData { get; set; }

    /// <summary>
    /// Función personalizada para cargar datos
    /// </summary>
    public Func<string?, int, int, Task<(List<T> data, int totalCount)>>? DataLoader { get; set; }

    /// <summary>
    /// Tamaño de página por defecto
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Texto a mostrar cuando el valor es nulo
    /// </summary>
    public string NullDisplayText { get; set; } = "(Sin seleccionar)";

    /// <summary>
    /// Total de registros (para paginación)
    /// </summary>
    public int TotalCount { get; private set; }

    /// <summary>
    /// Cargar datos con soporte para búsqueda y paginación
    /// </summary>
    public async Task<List<object>> LoadDataAsync(string? searchTerm = null, int skip = 0, int take = 20)
    {
        List<T> results;
        int totalCount = 0;

        if (DataLoader != null)
        {
            var (data, count) = await DataLoader(searchTerm, skip, take);
            results = data;
            totalCount = count;
        }
        else if (_apiService != null)
        {
            if (!string.IsNullOrEmpty(searchTerm) && SearchColumns.Any())
            {
                var searchRequest = new Shared.Models.QueryModels.SearchRequest
                {
                    SearchTerm = searchTerm,
                    SearchFields = SearchColumns,
                    Skip = skip,
                    Take = take
                };

                var response = await _apiService.SearchPagedAsync(searchRequest);
                if (response.Success && response.Data != null)
                {
                    results = response.Data.Data;
                    totalCount = response.Data.TotalCount;
                }
                else
                {
                    results = new List<T>();
                    totalCount = 0;
                }
            }
            else
            {
                var response = await _apiService.GetAllPagedAsync(skip / take + 1, take);
                if (response.Success && response.Data != null)
                {
                    results = response.Data.Data;
                    totalCount = response.Data.TotalCount;
                }
                else
                {
                    results = new List<T>();
                    totalCount = 0;
                }
            }
        }
        else if (StaticData != null)
        {
            var query = StaticData.AsQueryable();
            
            if (!string.IsNullOrEmpty(searchTerm) && SearchColumns.Any())
            {
                // Búsqueda simple en los campos especificados
                var searchLower = searchTerm.ToLower();
                var filteredList = new List<T>();
                
                foreach (var item in query)
                {
                    bool matches = false;
                    foreach (var column in SearchColumns)
                    {
                        var property = typeof(T).GetProperty(column);
                        if (property != null)
                        {
                            var propertyValue = property.GetValue(item);
                            var stringValue = propertyValue != null ? propertyValue.ToString() : null;
                            var value = stringValue != null ? stringValue.ToLower() : null;
                            if (!string.IsNullOrEmpty(value) && value.Contains(searchLower))
                            {
                                matches = true;
                                break;
                            }
                        }
                    }
                    if (matches)
                    {
                        filteredList.Add(item);
                    }
                }
                query = filteredList.AsQueryable();
            }

            totalCount = query.Count();
            results = query.Skip(skip).Take(take).ToList();
        }
        else
        {
            results = new List<T>();
            totalCount = 0;
        }

        TotalCount = totalCount;
        return results.Cast<object>().ToList();
    }

    /// <summary>
    /// Obtener el texto de visualización de un item
    /// </summary>
    public string GetDisplayText(object item)
    {
        if (item == null)
            return NullDisplayText;

        if (!DisplayColumns.Any())
            return item.ToString() ?? "";

        var displayParts = new List<string>();
        
        foreach (var column in DisplayColumns.Where(c => c.Visible))
        {
            var property = typeof(T).GetProperty(column.Property);
            if (property != null)
            {
                var propertyValue = property.GetValue(item);
                var value = propertyValue != null ? propertyValue.ToString() : null;
                if (!string.IsNullOrEmpty(value))
                {
                    displayParts.Add(value);
                }
            }
        }

        return displayParts.Any() ? string.Join(" - ", displayParts) : (item.ToString() ?? "");
    }

    /// <summary>
    /// Obtener el valor de un item
    /// </summary>
    public object? GetValue(object item)
    {
        if (item == null)
            return null;

        var property = typeof(T).GetProperty(ValueProperty);
        return property?.GetValue(item);
    }
}