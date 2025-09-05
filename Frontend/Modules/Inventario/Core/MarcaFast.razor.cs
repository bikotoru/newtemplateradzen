using Microsoft.AspNetCore.Components;
using Shared.Models.Entities;
using Shared.Models.Builders;
using Frontend.Services.Validation;
using Radzen;
using MarcaEntity = Shared.Models.Entities.Marca;

namespace Frontend.Modules.Inventario.Core;

public partial class MarcaFast : ComponentBase
{
    [Inject] private MarcaService MarcaService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Parameter] public EventCallback<MarcaEntity> OnEntityCreated { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private MarcaEntity entity = new() 
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
                .MaxLength(500, "La descripción no puede exceder 500 caracteres"))
            .Field("CodigoInterno", field => field
                .Required("El código es obligatorio")
                .Length(2, 50, "El código debe tener entre 2 y 50 caracteres"))
            .Build();
    }
    
    private async Task SaveEntity()
    {
        try
        {
            isLoading = true;

            // Validación Nombre
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

            // Validación Descripcion
            if (!string.IsNullOrEmpty(entity.Descripcion) && entity.Descripcion.Length > 500)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Validación",
                    Detail = "La descripción no puede exceder 500 caracteres",
                    Duration = 4000
                });
                return;
            }

            // Validación CodigoInterno
            if (string.IsNullOrWhiteSpace(entity.CodigoInterno))
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Validación",
                    Detail = "El código es obligatorio",
                    Duration = 4000
                });
                return;
            }
            
            if (entity.CodigoInterno.Length < 2 || entity.CodigoInterno.Length > 50)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Validación",
                    Detail = "El código debe tener entre 2 y 50 caracteres",
                    Duration = 4000
                });
                return;
            }

            var createRequest = new CreateRequestBuilder<MarcaEntity>(entity)
                .Build();

            var response = await MarcaService.CreateAsync(createRequest);

            if (response.Success && response.Data != null)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "¡Éxito!",
                    Detail = "Marca creado exitosamente",
                    Duration = 4000
                });
                
                await OnEntityCreated.InvokeAsync(response.Data);
                
                // Resetear el formulario
                entity = new MarcaEntity 
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
                    Detail = response.Message ?? "Error al crear marca",
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