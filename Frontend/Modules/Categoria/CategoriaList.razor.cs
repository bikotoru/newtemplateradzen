using Microsoft.AspNetCore.Components;
using Frontend.Components.Base.Tables;
using Shared.Models.Entities;
using Frontend.Services;
using Radzen;
using CategoriaEntity = Shared.Models.Entities.Categoria;

namespace Frontend.Modules.Categoria;

public partial class CategoriaList : ComponentBase
{
    [Inject] private CategoriaService CategoriaService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;

    private EntityTable<CategoriaEntity>? entityTable;

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
}