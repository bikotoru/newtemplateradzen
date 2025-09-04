using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Shared.Models.Entities;
using Shared.Models.Builders;
using Frontend.Services.Validation;
using Frontend.Components.Validation;
using CategoriaEntity = Shared.Models.Entities.Categoria;
using Radzen;

namespace Frontend.Modules.Categoria;

public partial class CategoriaFormulario : ComponentBase
{
    [Inject] private CategoriaService CategoriaService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Parameter] public Guid? Id { get; set; }

    private CategoriaEntity entity = new();
    private bool isLoading = false;
    private bool isEditMode => Id.HasValue;
    private bool isFormValid = false;
    private bool isNewlyCreated = false;

    protected override async Task OnInitializedAsync()
    {
        if (isEditMode && Id.HasValue)
        {
            await LoadEntity();
            
            // Verificar si viene de una creación reciente
            if (Navigation.Uri.Contains("created=true"))
            {
                isNewlyCreated = true;
                
                // Mostrar notificación de éxito
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "¡Éxito!",
                    Detail = "Categoría creada exitosamente. Ahora puedes editarla.",
                    Duration = 5000
                });
                
                // Limpiar el parámetro de la URL sin recargar
                Navigation.NavigateTo($"/categoria/formulario/{Id}", replace: true);
            }
        }
        else
        {
            // Inicializar entidad nueva con valores por defecto
            entity = new CategoriaEntity 
            { 
                Active = true,
                FechaCreacion = DateTime.Now,
                FechaModificacion = DateTime.Now
            };
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
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = "No se pudo cargar la categoría",
                    Duration = 5000
                });
                Navigation.NavigateTo("/categoria/list");
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = $"Error cargando categoría: {ex.Message}",
                Duration = 5000
            });
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private FormValidationRules GetValidationRules()
    {
        return FormValidationRulesBuilder
            .Create()
            .Field("Nombre", field => field
                .Required("El nombre es obligatorio")
                .Length(3, 100, "El nombre debe tener entre 3 y 100 caracteres"))
            .Field("Descripcion", field => field
                .MaxLength(255, "La descripción no puede exceder 255 caracteres"))
            .Build();
    }
    
    private FormValidator? formValidator;

    private async Task SaveForm()
    {
        try
        {
            isLoading = true;

            // Validación simple - verificar campos requeridos
            if (string.IsNullOrWhiteSpace(entity.Nombre))
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Validación",
                    Detail = "El nombre es obligatorio",
                    Duration = 4000
                });
                return;
            }
            
            if (entity.Nombre.Length < 3 || entity.Nombre.Length > 100)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Validación",
                    Detail = "El nombre debe tener entre 3 y 100 caracteres",
                    Duration = 4000
                });
                return;
            }
            
            if (!string.IsNullOrEmpty(entity.Descripcion) && entity.Descripcion.Length > 255)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Validación",
                    Detail = "La descripción no puede exceder 255 caracteres",
                    Duration = 4000
                });
                return;
            }

            if (isEditMode)
            {
                var updateRequest = new UpdateRequestBuilder<CategoriaEntity>(entity)
                    .Build();

                var response = await CategoriaService.UpdateAsync(updateRequest);

                if (response.Success)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "¡Éxito!",
                        Detail = "Categoría actualizada exitosamente",
                        Duration = 4000
                    });
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Error",
                        Detail = response.Message ?? "Error al actualizar categoría",
                        Duration = 5000
                    });
                }
            }
            else
            {
                var createRequest = new CreateRequestBuilder<CategoriaEntity>(entity)
                    .Build();

                var response = await CategoriaService.CreateAsync(createRequest);

                if (response.Success && response.Data != null)
                {
                    // Actualizar la entidad con los datos devueltos (incluyendo el ID)
                    entity = response.Data;
                    
                    // Navegar al modo edición con parámetro de éxito
                    Navigation.NavigateTo($"/categoria/formulario/{entity.Id}?created=true");
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Error",
                        Detail = response.Message ?? "Error al crear categoría",
                        Duration = 5000
                    });
                }
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error Inesperado",
                Detail = ex.Message,
                Duration = 6000
            });
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }
}