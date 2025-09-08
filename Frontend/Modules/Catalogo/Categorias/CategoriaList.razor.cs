using Microsoft.AspNetCore.Components;
using Frontend.Components.Base.Tables;
using Shared.Models.Entities;
using Frontend.Services;
using Radzen;
using CategoriaEntity = Shared.Models.Entities.Categoria;

namespace Frontend.Modules.Catalogo.Categorias;

public partial class CategoriaList : ComponentBase
{
    [Inject] private CategoriaService CategoriaService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private QueryService QueryService { get; set; } = null!;

    private EntityTable<CategoriaEntity>? entityTable;
    private CategoriaViewManager viewManager = null!;
    private ViewConfiguration<CategoriaEntity> currentView = null!;
    
    private List<IViewConfiguration<CategoriaEntity>>? ViewConfigurationsTyped => viewManager?.ViewConfigurations?.Cast<IViewConfiguration<CategoriaEntity>>().ToList();
    
    protected override void OnInitialized()
    {
        viewManager = new CategoriaViewManager(QueryService);
        currentView = viewManager.GetDefaultView();
        base.OnInitialized();
    }

    private async Task HandleEdit(CategoriaEntity categoria)
    {
        Navigation.NavigateTo($"/catalogo/categoria/formulario/{categoria.Id}");
    }

    private async Task HandleDelete(CategoriaEntity categoria)
    {
        var response = await CategoriaService.DeleteAsync(categoria.Id);
        
        if (!response.Success)
        {
            await DialogService.Alert(
                response.Message ?? "Error al eliminar categoria", 
                "Error"
            );
        }
    }
    
    private async Task OnViewChanged(IViewConfiguration<CategoriaEntity> selectedView)
    {
        if (selectedView is ViewConfiguration<CategoriaEntity> viewConfig)
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