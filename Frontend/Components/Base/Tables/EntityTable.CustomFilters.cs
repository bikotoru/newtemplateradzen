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
            
            // Limpiar filtro en el grid
            if (grid?.ColumnsCollection != null)
            {
                var column = grid.ColumnsCollection.FirstOrDefault(c => c.Property == filterResult.FieldName);
                if (column != null)
                {
                    column.FilterValue = null;
                    column.FilterOperator = FilterOperator.Contains;
                }
            }
        }
        else if (!string.IsNullOrEmpty(filterResult.Operator))
        {
            // Aplicar filtro
            activeCustomFilters[filterResult.FieldName] = filterResult;
            
            // Aplicar filtro en el grid
            if (grid?.ColumnsCollection != null)
            {
                var column = grid.ColumnsCollection.FirstOrDefault(c => c.Property == filterResult.FieldName);
                if (column != null)
                {
                    column.FilterValue = filterResult.Value;
                    column.FilterOperator = ConvertStringToFilterOperator(filterResult.Operator);
                }
            }
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

    #endregion
}