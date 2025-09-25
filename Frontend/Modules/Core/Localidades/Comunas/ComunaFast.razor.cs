using Microsoft.AspNetCore.Components;
using Shared.Models.Entities;
using Shared.Models.Builders;
using Frontend.Services.Validation;
using Radzen;
using ComunaEntity = Shared.Models.Entities.Comuna;
using System.Linq.Expressions;

namespace Frontend.Modules.Core.Localidades.Comunas;

public partial class ComunaFast : ComponentBase
{
    [Inject] private ComunaService ComunaService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private RegionService RegionService { get; set; } = null!;
    [Parameter] public EventCallback<ComunaEntity> OnEntityCreated { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
    private bool CanEdit => true;

    private ComunaEntity entity = new() 
    { 
        Active = true,
        FechaCreacion = DateTime.Now,
        FechaModificacion = DateTime.Now
    };
    private bool isLoading = false;
    private Expression<Func<Shared.Models.Entities.Region, object>>[] regionSearchFields = new Expression<Func<Shared.Models.Entities.Region, object>>[] { x => x.Nombre };

    private FormValidationRules GetValidationRules()
    {
        return FormValidationRulesBuilder
            .Create()
            .Field("CustomFields", field => field
                .MaxLength(255, "El campo no puede exceder 255 caracteres"))
            .Field("Nombre", field => field
                .Required("El nombre es obligatorio")
                .Length(3, 100, "El nombre debe tener entre 3 y 100 caracteres"))
            .Field("RegionId", field => field
                .Required("El campo es obligatorio"))
            .Build();
    }
    
    private async Task SaveEntity()
    {
        try
        {
            isLoading = true;

            // CustomFields - Sin validación específica

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

            // RegionId - Sin validación específica

            var createRequest = new CreateRequestBuilder<ComunaEntity>(entity)
                .Build();

            var response = await ComunaService.CreateAsync(createRequest);

            if (response.Success && response.Data != null)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "¡Éxito!",
                    Detail = "Comuna creado exitosamente",
                    Duration = 4000
                });

                if (OnEntityCreated.HasDelegate)
                {
                    await OnEntityCreated.InvokeAsync(response.Data);
                }
                else
                {
                    DialogService.Close(response.Data);
                }
                
                // Resetear el formulario
                entity = new ComunaEntity 
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
                    Detail = response.Message ?? "Error al crear comuna",
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