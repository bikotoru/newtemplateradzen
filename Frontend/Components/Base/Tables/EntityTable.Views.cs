using Radzen.Blazor;
using Radzen;

namespace Frontend.Components.Base.Tables;

public partial class EntityTable<T>
{
    #region View Management Methods

    private bool HasViewSelector()
    {
        return ViewConfigurations != null && ViewConfigurations.Count > 1;
    }

    private string GetCurrentViewDisplayName()
    {
        if (CurrentView == null) return "";
        
        var property = CurrentView.GetType().GetProperty(ViewDisplayNameProperty);
        return property?.GetValue(CurrentView)?.ToString() ?? "";
    }

    private async Task OnViewChangedInternal(object args)
    {
        Console.WriteLine($"[EntityTable] OnViewChangedInternal llamado con args: {args?.GetType().Name}");
        Console.WriteLine($"[EntityTable] OnViewChanged.HasDelegate: {OnViewChanged.HasDelegate}");
        
        if (OnViewChanged.HasDelegate && args is IViewConfiguration<T> viewConfig)
        {
            Console.WriteLine($"[EntityTable] Invocando OnViewChanged con vista: {viewConfig.DisplayName}");
            await OnViewChanged.InvokeAsync(viewConfig);
        }
        else
        {
            Console.WriteLine($"[EntityTable] ERROR: No se puede invocar OnViewChanged. HasDelegate: {OnViewChanged.HasDelegate}, args es IViewConfiguration: {args is IViewConfiguration<T>}");
        }
    }

    private int GetMainContentColumnSize()
    {
        return HasViewSelector() ? 8 : 12;
    }

    private int GetMainContentColumnSizeMD()
    {
        return HasViewSelector() ? 7 : 12;
    }
    
    private string GetViewSelectorClass()
    {
        return "d-flex pe-2 mb-sm-3 mb-md-0";
    }
    
    private string GetMainContentClass()
    {
        return "ps-lg-2 ps-md-2 ps-sm-0";
    }
    
    private Orientation GetStackOrientation()
    {
        return Orientation.Horizontal;
    }
    
    private AlignItems GetStackAlignment()
    {
        return AlignItems.Center;
    }
    
    private JustifyContent GetStackJustification()
    {
        return JustifyContent.End;
    }
    
    private string GetSearchContainerClass()
    {
        return "search-container w-100 w-md-auto";
    }
    
    private string GetButtonsContainerClass()
    {
        return "d-flex gap-2 justify-content-center justify-content-md-end w-100 w-md-auto mt-2 mt-md-0";
    }

    private int GetSearchColumnSize()
    {
        if (HasViewSelector())
        {
            return 6;
        }
        
        var buttonCount = (ShowRefreshButton ? 1 : 0) + (ShowAutoRefresh ? 1 : 0) + (ShowExcelExport ? 1 : 0) + (ShowColumnConfig ? 1 : 0);
        return buttonCount switch
        {
            0 => 12,
            1 => 10, 
            2 => 8,
            3 => 6,
            _ => 6
        };
    }

    private int GetButtonsColumnSize()
    {
        if (HasViewSelector() && ShowSearchBar)
        {
            return 3;
        }
        else if (HasViewSelector() || ShowSearchBar)
        {
            return 4;
        }
        return 12;
    }

    #endregion
}