using Microsoft.AspNetCore.Components;
using Frontend.Components.Base.Tables;
using Shared.Models.Entities;
using Frontend.Services;
using Radzen;
using Frontend.Components.Auth;
using SystemPermissionEntity = Shared.Models.Entities.SystemEntities.SystemPermissions;

namespace Frontend.Modules.Admin.SystemPermissions;

public partial class SystemPermissionList : AuthorizedPageBase
{
    [Inject] private SystemPermissionService SystemPermissionService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private QueryService QueryService { get; set; } = null!;

    private EntityTable<SystemPermissionEntity>? entityTable;
    private SystemPermissionViewManager? viewManager;
    private ViewConfiguration<SystemPermissionEntity>? currentView;
    
    private List<IViewConfiguration<SystemPermissionEntity>>? ViewConfigurationsTyped => viewManager?.ViewConfigurations?.Cast<IViewConfiguration<SystemPermissionEntity>>().ToList();
    
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync(); // ¡IMPORTANTE! Siempre llamar primero al base para verificar permisos
        
        if (HasRequiredPermissions)
        {
            viewManager = new SystemPermissionViewManager(QueryService);
            currentView = viewManager.GetDefaultView();
        }
    }

    private async Task HandleEdit(SystemPermissionEntity systempermission)
    {
        Navigation.NavigateTo($"/admin/systempermission/formulario/{systempermission.Id}");
    }

    private async Task HandleDelete(SystemPermissionEntity systempermission)
    {
        var response = await SystemPermissionService.DeleteAsync(systempermission.Id);
        
        if (!response.Success)
        {
            await DialogService.Alert(
                response.Message ?? "Error al eliminar systempermission", 
                "Error"
            );
        }
    }
    
    private async Task OnViewChanged(IViewConfiguration<SystemPermissionEntity> selectedView)
    {
        if (selectedView is ViewConfiguration<SystemPermissionEntity> viewConfig)
        {
            currentView = viewConfig;
            await InvokeAsync(StateHasChanged);
        }
    }
    
    private string GetGridKey()
    {
        return $"grid_{currentView?.DisplayName ?? "default"}";
    }
}