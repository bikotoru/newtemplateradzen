using Microsoft.AspNetCore.Components;
using Frontend.Components.Base.Tables;
using Shared.Models.Entities;
using Frontend.Services;
using Radzen;
using Frontend.Modules.Categoria.Models;
using CategoriaEntity = Shared.Models.Entities.Categoria;

namespace Frontend.Modules.Categoria;

public partial class CategoriaList : ComponentBase
{
    [Inject] private CategoriaService CategoriaService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private QueryService QueryService { get; set; } = null!;

    private EntityTable<CategoriaEntity>? entityTable;
    private CategoriaViewManager viewManager = null!;
    private CategoriaViewConfig currentView = null!;
    
    // Propiedad para EntityTable
    private List<object> ViewConfigurationsAsObjects => viewManager?.ViewConfigurations?.Cast<object>().ToList() ?? new List<object>();
    
    protected override void OnInitialized()
    {
        viewManager = new CategoriaViewManager(QueryService);
        currentView = viewManager.GetDefaultView();
        base.OnInitialized();
    }

    private void GoToCreate()
    {
        Navigation.NavigateTo("/categoria/formulario");
    }

    private async Task HandleEdit(CategoriaEntity categoria)
    {
        Navigation.NavigateTo($"/categoria/formulario/{categoria.Id}");
    }

    private async Task HandleDelete(CategoriaEntity categoria)
    {
        var response = await CategoriaService.DeleteAsync(categoria.Id);
        
        if (!response.Success)
        {
            await DialogService.Alert(
                response.Message ?? "Error al eliminar categoría", 
                "Error"
            );
        }
    }
    
    private async Task OnViewChanged(object args)
    {
        var viewName = args?.ToString();
        if (string.IsNullOrEmpty(viewName)) return;
        
        var selectedView = viewManager.GetViewByName(viewName);
        if (selectedView != null)
        {
            currentView = selectedView;
            
            // Forzar reconstrucción completa del grid
            await InvokeAsync(StateHasChanged);
        }
    }
    
    /// <summary>
    /// Genera una clave única para forzar la reconstrucción del grid cuando cambie la vista
    /// </summary>
    private string GetGridKey()
    {
        // Clave única basada SOLO en la vista actual - no cambiar en cada render
        return $"grid_{currentView?.DisplayName ?? "default"}";
    }
}