using Microsoft.AspNetCore.Components;
using Radzen.Blazor;
using Radzen;
using Shared.Models.Entities;

namespace Frontend.Pages.Demo;

public partial class List : ComponentBase
{
    private RadzenDataGrid<Categoria>? grid;
    private IEnumerable<Categoria>? categorias;
    private bool isLoading = false;
    private string errorMessage = string.Empty;
    private int count = 0;

    private async Task LoadData(LoadDataArgs args)
    {
        try
        {
            isLoading = true;
            errorMessage = string.Empty;

            // Usar el método LoadDataAsync de BaseApiService que convierte LoadDataArgs a QueryRequest
            var response = await CategoriaService.LoadDataAsync(args);
            
            if (response.Success && response.Data != null)
            {
                categorias = response.Data.Data;
                count = response.Data.TotalCount;
            }
            else
            {
                errorMessage = response.Message ?? "Error al cargar categorías";
                categorias = new List<Categoria>();
                count = 0;
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error de conexión: {ex.Message}";
            categorias = new List<Categoria>();
            count = 0;
        }
        finally
        {
            isLoading = false;
        }
    }
}