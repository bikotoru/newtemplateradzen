using Microsoft.AspNetCore.Components;
using Frontend.Components.Base.Tables;
using Shared.Models.Entities;
using Frontend.Services;
using Radzen;
using Frontend.Components.Auth;
using SystemRoleEntity = Shared.Models.Entities.SystemEntities.SystemRoles;

namespace Frontend.Modules.Admin.SystemRoles;

public partial class SystemRoleList : AuthorizedPageBase
{
    [Inject] private SystemRoleService SystemRoleService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private QueryService QueryService { get; set; } = null!;

    private EntityTable<SystemRoleEntity>? entityTable;
    private SystemRoleViewManager? viewManager;
    private ViewConfiguration<SystemRoleEntity>? currentView;
    
    private List<IViewConfiguration<SystemRoleEntity>>? ViewConfigurationsTyped => viewManager?.ViewConfigurations?.Cast<IViewConfiguration<SystemRoleEntity>>().ToList();
    
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync(); // ¡IMPORTANTE! Siempre llamar primero al base para verificar permisos
        
        if (HasRequiredPermissions)
        {
            viewManager = new SystemRoleViewManager(QueryService);
            currentView = viewManager.GetDefaultView();
        }
    }

    private async Task HandleEdit(SystemRoleEntity systemrole)
    {
        Navigation.NavigateTo($"/admin/systemrole/formulario/{systemrole.Id}");
    }

    private async Task HandleDelete(SystemRoleEntity systemrole)
    {
        var response = await SystemRoleService.DeleteAsync(systemrole.Id);
        
        if (!response.Success)
        {
            await DialogService.Alert(
                response.Message ?? "Error al eliminar systemrole", 
                "Error"
            );
        }
    }
    
    private async Task OnViewChanged(IViewConfiguration<SystemRoleEntity> selectedView)
    {
        if (selectedView is ViewConfiguration<SystemRoleEntity> viewConfig)
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