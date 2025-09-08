using Microsoft.AspNetCore.Components;
using Frontend.Components.Base.Tables;
using Shared.Models.Entities;
using Frontend.Services;
using Radzen;
using MarcaEntity = Shared.Models.Entities.Marca;

namespace Frontend.Modules.Catalogo.Marcas;

public partial class MarcaList : ComponentBase
{
    [Inject] private MarcaService MarcaService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private QueryService QueryService { get; set; } = null!;

    private EntityTable<MarcaEntity>? entityTable;
    private MarcaViewManager viewManager = null!;
    private ViewConfiguration<MarcaEntity> currentView = null!;
    
    private List<IViewConfiguration<MarcaEntity>>? ViewConfigurationsTyped => viewManager?.ViewConfigurations?.Cast<IViewConfiguration<MarcaEntity>>().ToList();
    
    protected override void OnInitialized()
    {
        viewManager = new MarcaViewManager(QueryService);
        currentView = viewManager.GetDefaultView();
        base.OnInitialized();
    }

    private async Task HandleEdit(MarcaEntity marca)
    {
        Navigation.NavigateTo($"/catalogo/marca/formulario/{marca.Id}");
    }

    private async Task HandleDelete(MarcaEntity marca)
    {
        var response = await MarcaService.DeleteAsync(marca.Id);
        
        if (!response.Success)
        {
            await DialogService.Alert(
                response.Message ?? "Error al eliminar marca", 
                "Error"
            );
        }
    }
    
    private async Task OnViewChanged(IViewConfiguration<MarcaEntity> selectedView)
    {
        if (selectedView is ViewConfiguration<MarcaEntity> viewConfig)
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