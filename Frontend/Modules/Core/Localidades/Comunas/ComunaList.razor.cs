using Microsoft.AspNetCore.Components;
using Frontend.Components.Base.Tables;
using Shared.Models.Entities;
using Frontend.Services;
using Radzen;
using Frontend.Components.Auth;
using ComunaEntity = Shared.Models.Entities.Comuna;

namespace Frontend.Modules.Core.Localidades.Comunas;

public partial class ComunaList : AuthorizedPageBase
{
    [Inject] private ComunaService ComunaService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private QueryService QueryService { get; set; } = null!;

    private EntityTable<ComunaEntity>? entityTable;
    private ComunaViewManager? viewManager;
    private ViewConfiguration<ComunaEntity>? currentView;
    
    private List<IViewConfiguration<ComunaEntity>>? ViewConfigurationsTyped => viewManager?.ViewConfigurations?.Cast<IViewConfiguration<ComunaEntity>>().ToList();
    
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync(); // ¡IMPORTANTE! Siempre llamar primero al base para verificar permisos
        
        if (HasRequiredPermissions)
        {
            viewManager = new ComunaViewManager(QueryService);
            currentView = viewManager.GetDefaultView();
        }
    }

    private async Task HandleEdit(ComunaEntity comuna)
    {
        Navigation.NavigateTo($"/core/localidades/comuna/formulario/{comuna.Id}");
    }

    private async Task HandleDelete(ComunaEntity comuna)
    {
        try
        {
            await DialogService.OpenLoadingAsync("Eliminando...");
            var response = await ComunaService.DeleteAsync(comuna.Id);
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
    
    private async Task OnViewChanged(IViewConfiguration<ComunaEntity> selectedView)
    {
        if (selectedView is ViewConfiguration<ComunaEntity> viewConfig)
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