using Microsoft.JSInterop;
using System.Reflection;
using System.Text.Json;
using Frontend.Components.Base.Dialogs;
using Radzen;
using Shared.Models.Export;

namespace Frontend.Components.Base.Tables;

public partial class EntityTable<T>
{
    #region Column Management Methods

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

    private async Task OpenColumnConfigModal()
    {
        try
        {
            var columnItems = BuildColumnVisibilityItems();
            currentPath = GetCurrentPath();
            
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
                        IsVisible = column.Visible
                    });
                }
            }
        }
        else if (ColumnConfigs != null)
        {
            foreach (var config in ColumnConfigs)
            {
                items.Add(new ColumnVisibilityItem
                {
                    Property = config.Property,
                    DisplayName = config.Title ?? GetDisplayName(config.Property),
                    IsVisible = config.Visible
                });
            }
        }
        else
        {
            var properties = GetAutoColumns();
            foreach (var prop in properties)
            {
                items.Add(new ColumnVisibilityItem
                {
                    Property = prop.Name,
                    DisplayName = GetDisplayName(prop.Name),
                    IsVisible = true
                });
            }
        }

        return items;
    }

    private async Task ApplyColumnVisibility(List<ColumnVisibilityItem> columnItems)
    {
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

        StateHasChanged();
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
                }
            }
        }
        catch (Exception)
        {
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
                        StateHasChanged();
                        
                        await Task.Delay(100);
                        await Reload();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log error silently
        }
    }

    private string GetCurrentPath()
    {
        return typeof(T).Name.ToLower();
    }

    #endregion
}