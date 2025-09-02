using Microsoft.AspNetCore.Components;
using Radzen.Blazor;
using Shared.Models.Entities;

namespace Frontend.Pages.Demo;

public partial class List : ComponentBase
{
    private RadzenDataGrid<Categoria>? grid;
    private List<Categoria>? categorias;
    private bool isLoading = true;
    private string errorMessage = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await LoadCategorias();
    }

    private async Task LoadCategorias()
    {
        try
        {
            isLoading = true;
            errorMessage = string.Empty;
            StateHasChanged();

            var response = await CategoriaService.GetAllUnpagedAsync();
            
            if (response.Success)
            {
                categorias = response.Data ?? new List<Categoria>();
            }
            else
            {
                errorMessage = response.Message ?? "Error al cargar categorías";
                categorias = new List<Categoria>();
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error de conexión: {ex.Message}";
            categorias = new List<Categoria>();
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }
}