using Microsoft.AspNetCore.Components;
using Frontend.Modules.Categoria;
using Shared.Models.Entities;
using Shared.Models.Requests;
using Frontend.Services;
using System.Linq.Expressions;

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
    
    // Campos buscables para el Lookup de categorías
    private Expression<Func<Categoria, object>>[] categoriaSearchFields = Array.Empty<Expression<Func<Categoria, object>>>();
    
    // Ejemplo de OnLoad personalizado (comentado por defecto)
    // private Func<LoadDataArgs, QueryBuilder<Categoria>?, Task<ApiResponse<PagedResult<Categoria>>>>? customOnLoad;

    protected override void OnInitialized()
    {
        // Crear QueryBuilder que filtre categorías que empiecen con "Z"
        baseQueryFilter = CategoriaService.Query().Where(c => c.Nombre.StartsWith("Z"));
        
        // Configurar campos buscables: solo Nombre y Descripción
        categoriaSearchFields = SearchFields<Categoria>(c => c.Nombre, c => c.Descripcion);
        
        // Ejemplo de cómo combinar QueryBuilders (comentado para demostrar flexibilidad):
        // var otherFilter = CategoriaService.Query().Where(c => c.Descripcion != null);
        // var combinedAnd = baseQueryFilter.And(otherFilter); // Z + Descripción no null
        // var combinedOr = baseQueryFilter.Or(otherFilter);   // Z OR Descripción no null
        
        // Ejemplo de OnLoad personalizado (comentado):
        // customOnLoad = async (args, query) =>
        // {
        //     // Lógica personalizada - por ejemplo, agregar logging, cache, transformaciones
        //     Console.WriteLine($"Cargando datos: Skip={args.Skip}, Top={args.Top}, Filter={args.Filter}");
        //     
        //     // Llamar al servicio con la query combinada
        //     var result = query != null 
        //         ? await CategoriaService.LoadDataAsync(args, query)
        //         : await CategoriaService.LoadDataAsync(args);
        //     
        //     // Aplicar transformaciones post-carga si es necesario
        //     // if (result.Success && result.Data != null) { ... }
        //     
        //     return result;
        // };
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
    
    // Helper method para crear campos buscables de forma fuertemente tipada
    private static Expression<Func<T, object>>[] SearchFields<T>(params Expression<Func<T, object>>[] fields)
    {
        return fields;
    }
}