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
                Width = "400px", 
                Height = "auto",
                Resizable = false,
                Draggable = true
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

        // Aplicar todos los filtros activos al lastLoadDataArgs
        if (lastLoadDataArgs != null)
        {
            var filterDescriptors = new List<FilterDescriptor>();
            
            foreach (var activeFilter in activeCustomFilters.Values)
            {
                filterDescriptors.Add(new FilterDescriptor
                {
                    Property = activeFilter.FieldName,
                    FilterValue = activeFilter.Value,
                    FilterOperator = ConvertStringToFilterOperator(activeFilter.Operator!)
                });
            }
            
            lastLoadDataArgs.Filters = filterDescriptors;
        }

        // Recargar datos
        await FirstPage();
        StateHasChanged();
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
        return activeCustomFilters.ContainsKey(propertyName);
    }

    private bool IsSortActive(string propertyName)
    {
        return activeSorts.ContainsKey(propertyName);
    }

    private string GetSortIcon(string propertyName)
    {
        if (activeSorts.ContainsKey(propertyName))
        {
            return activeSorts[propertyName] == SortOrder.Ascending ? "keyboard_arrow_up" : "keyboard_arrow_down";
        }
        return "sort";
    }

    private string GetSortTitle(string propertyName)
    {
        if (activeSorts.ContainsKey(propertyName))
        {
            var direction = activeSorts[propertyName] == SortOrder.Ascending ? "ascendente" : "descendente";
            return $"Ordenado {direction} - clic para cambiar";
        }
        return "Ordenar";
    }

    private async Task ToggleSort(string propertyName)
    {
        // Determinar el nuevo orden (toggle entre Ascending, Descending)
        var newSortOrder = SortOrder.Ascending;
        if (activeSorts.ContainsKey(propertyName))
        {
            newSortOrder = activeSorts[propertyName] == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
        }
        
        // Limpiar todos los otros sorts (solo un sort a la vez)
        activeSorts.Clear();
        activeSorts[propertyName] = newSortOrder;
        
        // Aplicar sort al lastLoadDataArgs
        if (lastLoadDataArgs != null)
        {
            lastLoadDataArgs.Sorts = new List<SortDescriptor>
            {
                new SortDescriptor { Property = propertyName, SortOrder = newSortOrder }
            };
            
            // Forzar recarga
            await Reload();
        }
        
        StateHasChanged();
    }

    #endregion
}