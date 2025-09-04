using Microsoft.AspNetCore.Components;
using Radzen;
using System.Reflection;
using Frontend.Components.Base.Dialogs;
using Frontend.Models;

namespace Frontend.Components.Base.Tables;

public partial class EntityTable<T>
{
    #region Custom Filter Methods

    private Dictionary<string, CustomFilterResult> activeCustomFilters = new();
    private Dictionary<string, SortOrder> activeSorts = new();

    private async Task OpenCustomFilter(string fieldName, Type dataType)
    {
        Console.WriteLine($"[CUSTOM FILTERS] Abriendo filtro custom para {fieldName} (tipo: {dataType.Name})");

        var currentFilter = activeCustomFilters.ContainsKey(fieldName) ? activeCustomFilters[fieldName] : null;

        var result = await DialogService.OpenAsync<CustomFilterDialog>("Filtro Personalizado",
            new Dictionary<string, object>
            {
                { "FieldName", fieldName },
                { "DataType", dataType },
                { "CurrentFilterValue", currentFilter?.Value },
                { "CurrentFilterOperator", currentFilter?.Operator }
            },
            new DialogOptions
            {
                Width = "400px",
                Height = "auto",
                Resizable = false,
                Draggable = true
            });

        Console.WriteLine($"[CUSTOM FILTERS] Resultado del diálogo: {result?.GetType().Name ?? "null"}");

        if (result != null && result is CustomFilterResult)
        {
            var filterResult = (CustomFilterResult)result;
            Console.WriteLine($"[CUSTOM FILTERS] Aplicando resultado: {filterResult.FieldName} = {filterResult.Value} ({filterResult.Operator})");
            await ApplyCustomFilter(filterResult);
        }
        else
        {
            Console.WriteLine("[CUSTOM FILTERS] Diálogo cancelado o sin resultado");
        }
    }

    private async Task ApplyCustomFilter(CustomFilterResult filterResult)
    {
        if (filterResult.IsClear)
        {
            // Limpiar filtro
            if (activeCustomFilters.ContainsKey(filterResult.FieldName))
            {
                activeCustomFilters.Remove(filterResult.FieldName);
            }
        }
        else if (!string.IsNullOrEmpty(filterResult.Operator))
        {
            // Aplicar filtro
            activeCustomFilters[filterResult.FieldName] = filterResult;
        }

        // Crear nuevos LoadDataArgs con nuestros filtros y sorts
        await ReloadWithCustomFiltersAndSorts();
        StateHasChanged();
    }
    private LoadDataArgs SetLoadData(LoadDataArgs args)
    {
        Console.WriteLine($"[CUSTOM FILTERS] SetLoadData interceptando args...");
        
        // Aplicar filtros custom
        if (activeCustomFilters.Any())
        {
            Console.WriteLine($"[CUSTOM FILTERS] ✓ Aplicando {activeCustomFilters.Count} filtros custom a args.Filters");
            args.Filters = new List<FilterDescriptor>();
            
            foreach (var filter in activeCustomFilters)
            {
                ((List<FilterDescriptor>)args.Filters).Add(new FilterDescriptor 
                { 
                    Property = filter.Key, 
                    FilterOperator = ConvertStringToFilterOperator(filter.Value.Operator!),
                    FilterValue = filter.Value.Value
                });
                Console.WriteLine($"[CUSTOM FILTERS] ✓ Filtro agregado: {filter.Key} = {filter.Value.Value} ({filter.Value.Operator})");
            }
        }
        else
        {
            Console.WriteLine($"[CUSTOM FILTERS] - Sin filtros custom activos");
        }
        
        // Aplicar sorts custom
        if (activeSorts.Any())
        {
            var sortProperty = activeSorts.First();
            Console.WriteLine($"[CUSTOM FILTERS] ✓ Aplicando sort custom: {sortProperty.Key} = {sortProperty.Value}");
            
            args.Sorts = new List<SortDescriptor>
            {
                 new SortDescriptor
                        {
                            Property = sortProperty.Key,
                            SortOrder = sortProperty.Value
                        }
            };

            args.OrderBy = $"{sortProperty.Key} {(sortProperty.Value == SortOrder.Descending ? "desc" : "asc")}";
            Console.WriteLine($"[CUSTOM FILTERS] ✓ OrderBy configurado: '{args.OrderBy}'");
        }
        else
        {
            Console.WriteLine($"[CUSTOM FILTERS] - Sin sorts activos");
        }

        return args;
    }
    private async Task ReloadWithCustomFiltersAndSorts()
    {
        if (grid == null)
        {
            Console.WriteLine("[CUSTOM FILTERS] ERROR: Grid es null");
            return;
        }

        Console.WriteLine($"[CUSTOM FILTERS] Recargando grid...");
        Console.WriteLine($"[CUSTOM FILTERS] Filtros activos: {activeCustomFilters.Count}");
        Console.WriteLine($"[CUSTOM FILTERS] Sorts activos: {activeSorts.Count}");

        try
        {
            // SetLoadData se encargará de aplicar filtros y sorts a los args
            await grid.Reload();
            Console.WriteLine("[CUSTOM FILTERS] ✓ Recarga completada exitosamente");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CUSTOM FILTERS] ✗ ERROR: {ex.Message}");
        }
    }

    private FilterOperator ConvertStringToFilterOperator(string operatorString)
    {
        return operatorString switch
        {
            "Contains" => FilterOperator.Contains,
            "DoesNotContain" => FilterOperator.DoesNotContain,
            "Equals" => FilterOperator.Equals,
            "NotEquals" => FilterOperator.NotEquals,
            "StartsWith" => FilterOperator.StartsWith,
            "EndsWith" => FilterOperator.EndsWith,
            "GreaterThan" => FilterOperator.GreaterThan,
            "GreaterThanOrEquals" => FilterOperator.GreaterThanOrEquals,
            "LessThan" => FilterOperator.LessThan,
            "LessThanOrEquals" => FilterOperator.LessThanOrEquals,
            "IsNull" => FilterOperator.IsNull,
            "IsNotNull" => FilterOperator.IsNotNull,
            "IsEmpty" => FilterOperator.IsEmpty,
            "IsNotEmpty" => FilterOperator.IsNotEmpty,
            _ => FilterOperator.Contains
        };
    }

    private Type GetPropertyType(string propertyName)
    {
        var property = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        return property?.PropertyType ?? typeof(string);
    }

    private bool IsFilterActive(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName)) return false;
        return activeCustomFilters.ContainsKey(propertyName);
    }

    private bool IsSortActive(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName)) return false;
        return activeSorts.ContainsKey(propertyName);
    }

    private string GetSortIcon(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName)) return "sort";
        if (activeSorts.ContainsKey(propertyName))
        {
            return activeSorts[propertyName] == SortOrder.Ascending ? "keyboard_arrow_up" : "keyboard_arrow_down";
        }
        return "sort";
    }

    private string GetSortTitle(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName)) return "No disponible";
        if (activeSorts.ContainsKey(propertyName))
        {
            var direction = activeSorts[propertyName] == SortOrder.Ascending ? "ascendente" : "descendente";
            return $"Ordenado {direction} - clic para cambiar";
        }
        return "Ordenar";
    }

    private async Task ToggleSort(string propertyName)
    {
        Console.WriteLine($"[CUSTOM FILTERS] ► ToggleSort LLAMADO con propertyName: '{propertyName ?? "NULL"}'");

        // Validar que el propertyName no esté vacío
        if (string.IsNullOrEmpty(propertyName))
        {
            Console.WriteLine("[CUSTOM FILTERS] ✗ No se puede ordenar: propertyName está vacío");
            return;
        }

        Console.WriteLine($"[CUSTOM FILTERS] Toggle sort para {propertyName}");

        // Determinar el nuevo orden (toggle entre Ascending, Descending)
        var newSortOrder = SortOrder.Ascending;
        if (activeSorts.ContainsKey(propertyName))
        {
            newSortOrder = activeSorts[propertyName] == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
        }

        // Limpiar todos los otros sorts (solo un sort a la vez)
        activeSorts.Clear();
        activeSorts[propertyName] = newSortOrder;

        Console.WriteLine($"[CUSTOM FILTERS] Sort configurado: {propertyName} = {newSortOrder}");

        // Recargar con filtros y sorts custom
        await ReloadWithCustomFiltersAndSorts();
        StateHasChanged();
    }

    #endregion
}