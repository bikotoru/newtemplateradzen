using Microsoft.AspNetCore.Components;

namespace Frontend.Components.Base;

public partial class CrmTab : ComponentBase
{
    [Parameter] public string Id { get; set; } = "";
    [Parameter] public string Title { get; set; } = "";
    [Parameter] public string Icon { get; set; } = "";
    [Parameter] public string IconColor { get; set; } = "";
    [Parameter] public string TitleColor { get; set; } = "";
    [Parameter] public bool IsVisible { get; set; } = true;
    [Parameter] public RenderFragment? ChildContent { get; set; }
    
    [CascadingParameter] private CrmTabs? ParentTabs { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && ParentTabs != null && !string.IsNullOrEmpty(Id) && !string.IsNullOrEmpty(Title))
        {
            ParentTabs.AddTab(Id, Title, Icon, IconColor, TitleColor, ChildContent!, IsVisible);
        }
    }
}