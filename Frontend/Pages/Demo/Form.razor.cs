using Microsoft.AspNetCore.Components;
using Frontend.Modules.Categoria;
using Shared.Models.Entities;
using Shared.Models.Requests;
using Frontend.Services;

namespace Frontend.Pages.Demo;

public partial class Form : ComponentBase
{
    [Inject] private CategoriaService CategoriaService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private Categoria categoria = new();
    private string mensaje = string.Empty;
    private string errorMessage = string.Empty;
    private Guid? categoriaRelacionadaId;
    private QueryBuilder<Categoria>? baseQueryFilter;

    protected override void OnInitialized()
    {
        // Crear QueryBuilder que filtre categorías que empiecen con "A"
        baseQueryFilter = CategoriaService.Query().Where(c => c.Nombre.StartsWith("Z"));
    }

    private async Task SaveForm()
    {
        try
        {
            mensaje = string.Empty;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(categoria.Nombre))
            {
                errorMessage = "El nombre es obligatorio";
                return;
            }

            var createRequest = new CreateRequest<Categoria>
            {
                Entity = categoria
            };

            var response = await CategoriaService.CreateAsync(createRequest);

            if (response.Success)
            {
                mensaje = "Categoría creada exitosamente";
                categoria = new Categoria();
                StateHasChanged();
                
                // Limpiar mensaje después de 3 segundos
                await Task.Delay(3000);
                mensaje = string.Empty;
                StateHasChanged();
            }
            else
            {
                errorMessage = response.Message ?? "Error al crear la categoría";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error inesperado: {ex.Message}";
        }
    }
}