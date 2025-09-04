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
                Width = "600px",
                Height = "auto",
                Resizable = false,
                Draggable = true,
                ShowTitle = false,
                ShowClose = false,
                Style = "max-width: 100%"
            });

        if (result != null && result is CustomFilterResult)
        {
            var filterResult = (CustomFilterResult)result;
            await ApplyCustomFilter(filterResult);
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
        // Aplicar filtros custom
        if (activeCustomFilters.Any())
        {
            args.Filters = new List<FilterDescriptor>();
            
            foreach (var filter in activeCustomFilters)
            {
                ((List<FilterDescriptor>)args.Filters).Add(new FilterDescriptor 
                { 
                    Property = filter.Key, 
                    FilterOperator = ConvertStringToFilterOperator(filter.Value.Operator!),
                    FilterValue = filter.Value.Value
                });
            }
        }
        
        // Aplicar sorts custom
        if (activeSorts.Any())
        {
            var sortProperty = activeSorts.First();
            
            args.Sorts = new List<SortDescriptor>
            {
                 new SortDescriptor
                        {
                            Property = sortProperty.Key,
                            SortOrder = sortProperty.Value
                        }
            };

            args.OrderBy = $"{sortProperty.Key} {(sortProperty.Value == SortOrder.Descending ? "desc" : "asc")}";
        }

        return args;
    }
    private async Task ReloadWithCustomFiltersAndSorts()
    {
        if (grid == null)
        {
            return;
        }

        try
        {
            // SetLoadData se encargará de aplicar filtros y sorts a los args
            await grid.Reload();
        }
        catch (Exception ex)
        {
            // Log error to proper logging system if needed
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
        // Validar que el propertyName no esté vacío
        if (string.IsNullOrEmpty(propertyName))
        {
            return;
        }

        // Determinar el nuevo orden (toggle entre Ascending, Descending)
        var newSortOrder = SortOrder.Ascending;
        if (activeSorts.ContainsKey(propertyName))
        {
            newSortOrder = activeSorts[propertyName] == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
        }

        // Limpiar todos los otros sorts (solo un sort a la vez)
        activeSorts.Clear();
        activeSorts[propertyName] = newSortOrder;

        // Recargar con filtros y sorts custom
        await ReloadWithCustomFiltersAndSorts();
        StateHasChanged();
    }

    #endregion
}