using Microsoft.AspNetCore.Components;
using Frontend.Components.Base.Tables;
using Shared.Models.Entities;
using Frontend.Services;
using Radzen;
using ProductoEntity = Shared.Models.Entities.Producto;

namespace Frontend.Modules.Catalogo.Productos;

public partial class ProductoList : ComponentBase
{
    [Inject] private ProductoService ProductoService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private QueryService QueryService { get; set; } = null!;

    private EntityTable<ProductoEntity>? entityTable;
    private ProductoViewManager viewManager = null!;
    private ViewConfiguration<ProductoEntity> currentView = null!;
    
    private List<IViewConfiguration<ProductoEntity>>? ViewConfigurationsTyped => viewManager?.ViewConfigurations?.Cast<IViewConfiguration<ProductoEntity>>().ToList();
    
    protected override void OnInitialized()
    {
        viewManager = new ProductoViewManager(QueryService);
        currentView = viewManager.GetDefaultView();
        base.OnInitialized();
    }

    private async Task HandleEdit(ProductoEntity producto)
    {
        Navigation.NavigateTo($"/catalogo/producto/formulario/{producto.Id}");
    }

    private async Task HandleDelete(ProductoEntity producto)
    {
        var response = await ProductoService.DeleteAsync(producto.Id);
        
        if (!response.Success)
        {
            await DialogService.Alert(
                response.Message ?? "Error al eliminar producto", 
                "Error"
            );
        }
    }
    
    private async Task OnViewChanged(IViewConfiguration<ProductoEntity> selectedView)
    {
        if (selectedView is ViewConfiguration<ProductoEntity> viewConfig)
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