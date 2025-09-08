using Microsoft.AspNetCore.Components;
using Shared.Models.Entities;
using Shared.Models.Builders;
using Frontend.Services.Validation;
using Radzen;
using VentaEntity = Shared.Models.Entities.Venta;
using System.Linq.Expressions;

namespace Frontend.Modules.Ventas.Ventas;

public partial class VentaFast : ComponentBase
{
    [Inject] private VentaService VentaService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    
    [Parameter] public EventCallback<VentaEntity> OnEntityCreated { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private VentaEntity entity = new() 
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
            .Field("Numventa", field => field
                .Range(0, 999999, "El valor debe ser mayor a 0"))
            .Field("Montototal", field => field
                .Range(0, 999999, "El valor debe ser mayor a 0"))
            .Build();
    }
    
    private async Task SaveEntity()
    {
        try
        {
            isLoading = true;

            // Numventa - Sin validación específica

            // Montototal - Sin validación específica

            var createRequest = new CreateRequestBuilder<VentaEntity>(entity)
                .Build();

            var response = await VentaService.CreateAsync(createRequest);

            if (response.Success && response.Data != null)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "¡Éxito!",
                    Detail = "Venta creado exitosamente",
                    Duration = 4000
                });
                
                await OnEntityCreated.InvokeAsync(response.Data);
                
                // Resetear el formulario
                entity = new VentaEntity 
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
                    Detail = response.Message ?? "Error al crear venta",
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