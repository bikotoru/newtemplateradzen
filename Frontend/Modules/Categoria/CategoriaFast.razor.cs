using Microsoft.AspNetCore.Components;
using Shared.Models.Entities;
using Shared.Models.Builders;
using CategoriaEntity = Shared.Models.Entities.Categoria;

namespace Frontend.Modules.Categoria;

public partial class CategoriaFast : ComponentBase
{
    [Inject] private CategoriaService CategoriaService { get; set; } = null!;
    [Parameter] public EventCallback<CategoriaEntity> OnEntityCreated { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private CategoriaEntity entity = new();
    private string errorMessage = string.Empty;
    private bool isLoading = false;

    private async Task SaveEntity()
    {
        try
        {
            isLoading = true;
            errorMessage = string.Empty;

            var createRequest = new CreateRequestBuilder<CategoriaEntity>(entity)
                .Build();

            var response = await CategoriaService.CreateAsync(createRequest);

            if (response.Success && response.Data != null)
            {
                await OnEntityCreated.InvokeAsync(response.Data);
                entity = new CategoriaEntity();
            }
            else
            {
                errorMessage = response.Message ?? "Error al crear categoría";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error inesperado: {ex.Message}";
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task Cancel()
    {
        await OnCancel.InvokeAsync();
    }
}