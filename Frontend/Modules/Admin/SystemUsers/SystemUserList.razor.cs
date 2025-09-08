using Microsoft.AspNetCore.Components;
using Frontend.Components.Base.Tables;
using Shared.Models.Entities;
using Frontend.Services;
using Radzen;
using SystemUserEntity = Shared.Models.Entities.SystemEntities.SystemUsers;

namespace Frontend.Modules.Admin.SystemUsers;

public partial class SystemUserList : ComponentBase
{
    [Inject] private SystemUserService SystemUserService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private QueryService QueryService { get; set; } = null!;

    private EntityTable<SystemUserEntity>? entityTable;
    private SystemUserViewManager viewManager = null!;
    private ViewConfiguration<SystemUserEntity> currentView = null!;
    
    private List<IViewConfiguration<SystemUserEntity>>? ViewConfigurationsTyped => viewManager?.ViewConfigurations?.Cast<IViewConfiguration<SystemUserEntity>>().ToList();
    
    protected override void OnInitialized()
    {
        viewManager = new SystemUserViewManager(QueryService);
        currentView = viewManager.GetDefaultView();
        base.OnInitialized();
    }

    private async Task HandleEdit(SystemUserEntity systemuser)
    {
        Navigation.NavigateTo($"/admin/systemuser/formulario/{systemuser.Id}");
    }

    private async Task HandleDelete(SystemUserEntity systemuser)
    {
        var response = await SystemUserService.DeleteAsync(systemuser.Id);
        
        if (!response.Success)
        {
            await DialogService.Alert(
                response.Message ?? "Error al eliminar systemuser", 
                "Error"
            );
        }
    }
    
    private async Task OnViewChanged(IViewConfiguration<SystemUserEntity> selectedView)
    {
        if (selectedView is ViewConfiguration<SystemUserEntity> viewConfig)
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