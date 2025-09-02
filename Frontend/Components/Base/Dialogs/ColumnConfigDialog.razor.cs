using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using System.Text.Json;
using Frontend.Components.Base.Tables;

namespace Frontend.Components.Base.Dialogs;

public partial class ColumnConfigDialog : ComponentBase
{
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;

    [Parameter] public List<ColumnVisibilityItem> ColumnItems { get; set; } = new();
    [Parameter] public string CurrentPath { get; set; } = "";

    private List<ColumnVisibilityItem> originalItems = new();

    protected override void OnInitialized()
    {
        // Hacer una copia de los items originales para poder cancelar
        originalItems = ColumnItems.Select(item => new ColumnVisibilityItem
        {
            Property = item.Property,
            DisplayName = item.DisplayName,
            IsVisible = item.IsVisible,
            ColumnType = item.ColumnType
        }).ToList();
    }

    private void OnColumnVisibilityChanged(ColumnVisibilityItem item)
    {
        // El cambio ya se reflejó en item.IsVisible por el @bind-Value
        StateHasChanged();
    }

    private void ShowAllColumns()
    {
        foreach (var item in ColumnItems)
        {
            item.IsVisible = true;
        }
        StateHasChanged();
    }

    private void HideAllColumns()
    {
        foreach (var item in ColumnItems)
        {
            item.IsVisible = false;
        }
        StateHasChanged();
    }

    private async Task ApplyChanges()
    {
        try
        {
            // Guardar configuración en localStorage
            await SaveColumnConfigToLocalStorage();
            
            // Cerrar modal con los cambios aplicados
            DialogService.Close(ColumnItems);
        }
        catch (Exception ex)
        {
            // Si hay error guardando, mostrar alerta pero continuar
            await DialogService.Alert($"Error guardando configuración: {ex.Message}", "Advertencia");
            DialogService.Close(ColumnItems);
        }
    }

    private void Cancel()
    {
        // Restaurar valores originales
        for (int i = 0; i < ColumnItems.Count; i++)
        {
            if (i < originalItems.Count)
            {
                ColumnItems[i].IsVisible = originalItems[i].IsVisible;
            }
        }
        
        // Cerrar modal sin cambios
        DialogService.Close(null);
    }

    private async Task SaveColumnConfigToLocalStorage()
    {
        if (string.IsNullOrEmpty(CurrentPath)) return;

        var storageKey = $"column_config_{CurrentPath}";
        var config = ColumnItems.ToDictionary(
            item => item.Property, 
            item => item.IsVisible
        );

        var json = JsonSerializer.Serialize(config);
        await JSRuntime.InvokeVoidAsync("localStorage.setItem", storageKey, json);
    }

    private string GetColumnTypeBadge(ColumnSourceType columnType)
    {
        return columnType switch
        {
            ColumnSourceType.RenderFragment => "",
            ColumnSourceType.ColumnConfig => "Config",
            ColumnSourceType.Auto => "Auto",
            _ => "Unknown"
        };
    }

    private BadgeStyle GetColumnTypeBadgeStyle(ColumnSourceType columnType)
    {
        return columnType switch
        {
            ColumnSourceType.RenderFragment => BadgeStyle.Info,
            ColumnSourceType.ColumnConfig => BadgeStyle.Secondary,
            ColumnSourceType.Auto => BadgeStyle.Light,
            _ => BadgeStyle.Light
        };
    }

    private string GetColumnItemClass(ColumnVisibilityItem item)
    {
        var baseClass = "column-item";
        var statusClass = item.IsVisible ? "column-visible" : "column-hidden";
        return $"{baseClass} {statusClass}";
    }
}

public class ColumnVisibilityItem
{
    public string Property { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public bool IsVisible { get; set; } = true;
    public ColumnSourceType ColumnType { get; set; }
}

// Enum movido a EntityTable.razor.cs para evitar duplicación