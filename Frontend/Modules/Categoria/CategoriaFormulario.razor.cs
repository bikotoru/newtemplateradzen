using Microsoft.AspNetCore.Components;
using Shared.Models.Entities;
using Shared.Models.Builders;
using CategoriaEntity = Shared.Models.Entities.Categoria;

namespace Frontend.Modules.Categoria;

public partial class CategoriaFormulario : ComponentBase
{
    [Inject] private CategoriaService CategoriaService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Parameter] public Guid? Id { get; set; }

    private CategoriaEntity entity = new();
    private string mensaje = string.Empty;
    private string errorMessage = string.Empty;
    private bool isLoading = false;
    private bool isEditMode => Id.HasValue;

    protected override async Task OnInitializedAsync()
    {
        if (isEditMode && Id.HasValue)
        {
            await LoadEntity();
        }
    }

    private async Task LoadEntity()
    {
        try
        {
            isLoading = true;
            var response = await CategoriaService.GetByIdAsync(Id!.Value);
            
            if (response.Success && response.Data != null)
            {
                entity = response.Data;
            }
            else
            {
                errorMessage = "No se pudo cargar la categoría";
                Navigation.NavigateTo("/categoria/list");
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error cargando categoría: {ex.Message}";
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task SaveForm()
    {
        try
        {
            isLoading = true;
            mensaje = string.Empty;
            errorMessage = string.Empty;

            if (isEditMode)
            {
                var updateRequest = new UpdateRequestBuilder<CategoriaEntity>(entity)
                    .Build();

                var response = await CategoriaService.UpdateAsync(updateRequest);

                if (response.Success)
                {
                    mensaje = "Categoría actualizada exitosamente";
                    await Task.Delay(2000);
                    Navigation.NavigateTo("/categoria/list");
                }
                else
                {
                    errorMessage = response.Message ?? "Error al actualizar categoría";
                }
            }
            else
            {
                var createRequest = new CreateRequestBuilder<CategoriaEntity>(entity)
                    .Build();

                var response = await CategoriaService.CreateAsync(createRequest);

                if (response.Success)
                {
                    mensaje = "Categoría creada exitosamente";
                    entity = new CategoriaEntity();
                    await Task.Delay(2000);
                    Navigation.NavigateTo("/categoria/list");
                }
                else
                {
                    errorMessage = response.Message ?? "Error al crear categoría";
                }
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
}