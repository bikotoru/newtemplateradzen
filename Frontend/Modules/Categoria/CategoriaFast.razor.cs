using Microsoft.AspNetCore.Components;
using Shared.Models.Entities;
using Shared.Models.Builders;
using Frontend.Services.Validation;
using Radzen;
using CategoriaEntity = Shared.Models.Entities.Categoria;

namespace Frontend.Modules.Categoria;

public partial class CategoriaFast : ComponentBase
{
    [Inject] private CategoriaService CategoriaService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Parameter] public EventCallback<CategoriaEntity> OnEntityCreated { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private CategoriaEntity entity = new() 
    { 
        Active = true,
        FechaCreacion = DateTime.Now,
        FechaModificacion = DateTime.Now
    };
    private bool isLoading = false;

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
    
    private async Task SaveEntity()
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

            var createRequest = new CreateRequestBuilder<CategoriaEntity>(entity)
                .Build();

            var response = await CategoriaService.CreateAsync(createRequest);

            if (response.Success && response.Data != null)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "¡Éxito!",
                    Detail = "Categoría creada exitosamente",
                    Duration = 4000
                });
                
                await OnEntityCreated.InvokeAsync(response.Data);
                
                // Resetear el formulario
                entity = new CategoriaEntity 
                { 
                    Active = true,
                    FechaCreacion = DateTime.Now,
                    FechaModificacion = DateTime.Now
                };
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

    private async Task Cancel()
    {
        await OnCancel.InvokeAsync();
    }
}