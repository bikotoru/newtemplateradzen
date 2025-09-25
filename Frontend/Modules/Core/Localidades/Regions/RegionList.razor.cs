using Microsoft.AspNetCore.Components;
using Frontend.Components.Base.Tables;
using Shared.Models.Entities;
using Frontend.Services;
using Radzen;
using Frontend.Components.Auth;
using RegionEntity = Shared.Models.Entities.Region;

namespace Frontend.Modules.Core.Localidades.Regions;

public partial class RegionList : AuthorizedPageBase
{
    [Inject] private RegionService RegionService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private QueryService QueryService { get; set; } = null!;

    private EntityTable<RegionEntity>? entityTable;
    private RegionViewManager? viewManager;
    private ViewConfiguration<RegionEntity>? currentView;
    
    private List<IViewConfiguration<RegionEntity>>? ViewConfigurationsTyped => viewManager?.ViewConfigurations?.Cast<IViewConfiguration<RegionEntity>>().ToList();
    
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync(); // ¡IMPORTANTE! Siempre llamar primero al base para verificar permisos
        
        if (HasRequiredPermissions)
        {
            viewManager = new RegionViewManager(QueryService);
            currentView = viewManager.GetDefaultView();
        }
    }

    private async Task HandleEdit(RegionEntity region)
    {
        Navigation.NavigateTo($"/core/localidades/region/formulario/{region.Id}");
    }

    private async Task HandleDelete(RegionEntity region)
    {
        try
        {
            await DialogService.OpenLoadingAsync("Eliminando...");
            var response = await RegionService.DeleteAsync(region.Id);
            DialogService.Close();
            
            if (!response.Success)
            {
                await DialogService.Alert(
                    response.Message ?? "Error al eliminar", 
                    "Error"
                );
            }
            else
            {
                // Refrescar la tabla después de eliminar
                if (entityTable != null)
                {
                    await entityTable.Reload();
                }
            }
        }
        catch (Exception ex)
        {
            DialogService.Close();
            await DialogService.Alert(
                $"Error inesperado al eliminar: {ex.Message}", 
                "Error"
            );
        }
    }
    
    private async Task OnViewChanged(IViewConfiguration<RegionEntity> selectedView)
    {
        if (selectedView is ViewConfiguration<RegionEntity> viewConfig)
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