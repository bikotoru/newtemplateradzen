using Microsoft.AspNetCore.Components;
using Frontend.Components.Base.Tables;
using Shared.Models.Entities;
using Frontend.Services;
using Radzen;
using VentaEntity = Shared.Models.Entities.Venta;

namespace Frontend.Modules.Ventas.Ventas;

public partial class VentaList : ComponentBase
{
    [Inject] private VentaService VentaService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private QueryService QueryService { get; set; } = null!;

    private EntityTable<VentaEntity>? entityTable;
    private VentaViewManager viewManager = null!;
    private ViewConfiguration<VentaEntity> currentView = null!;
    
    private List<IViewConfiguration<VentaEntity>>? ViewConfigurationsTyped => viewManager?.ViewConfigurations?.Cast<IViewConfiguration<VentaEntity>>().ToList();
    
    protected override void OnInitialized()
    {
        viewManager = new VentaViewManager(QueryService);
        currentView = viewManager.GetDefaultView();
        base.OnInitialized();
    }

    private async Task HandleEdit(VentaEntity venta)
    {
        Navigation.NavigateTo($"/ventas/venta/formulario/{venta.Id}");
    }

    private async Task HandleDelete(VentaEntity venta)
    {
        var response = await VentaService.DeleteAsync(venta.Id);
        
        if (!response.Success)
        {
            await DialogService.Alert(
                response.Message ?? "Error al eliminar venta", 
                "Error"
            );
        }
    }
    
    private async Task OnViewChanged(IViewConfiguration<VentaEntity> selectedView)
    {
        if (selectedView is ViewConfiguration<VentaEntity> viewConfig)
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