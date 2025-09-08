using Microsoft.AspNetCore.Components;
using Shared.Models.Entities;
using Shared.Models.Builders;
using Frontend.Services.Validation;
using Radzen;
using ProductoEntity = Shared.Models.Entities.Producto;
using System.Linq.Expressions;

namespace Frontend.Modules.Catalogo.Productos;

public partial class ProductoFast : ComponentBase
{
    [Inject] private ProductoService ProductoService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private MarcaService MarcaService { get; set; } = null!;
    [Inject] private CategoriaService CategoriaService { get; set; } = null!;
    [Parameter] public EventCallback<ProductoEntity> OnEntityCreated { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private ProductoEntity entity = new() 
    { 
        Active = true,
        FechaCreacion = DateTime.Now,
        FechaModificacion = DateTime.Now
    };
    private bool isLoading = false;
    private Expression<Func<Marca, object>>[] marcaSearchFields = new Expression<Func<Marca, object>>[] { x => x.Nombre };
    private Expression<Func<Categoria, object>>[] categoriaSearchFields = new Expression<Func<Categoria, object>>[] { x => x.Nombre };

    private FormValidationRules GetValidationRules()
    {
        return FormValidationRulesBuilder
            .Create()
            .Field("Nombre", field => field
                .Required("El nombre es obligatorio")
                .Length(3, 100, "El nombre debe tener entre 3 y 100 caracteres"))
            .Field("Codigosku", field => field
                .Required("El código es obligatorio")
                .Length(2, 50, "El código debe tener entre 2 y 50 caracteres"))
            .Field("Precioventa", field => field
                .Range(0, 999999, "El valor debe ser mayor a 0"))
            .Field("Preciocompra", field => field
                .Range(0, 999999, "El valor debe ser mayor a 0"))
            .Field("MarcaId", field => field
                .Required("El campo es obligatorio"))
            .Field("CategoriaId", field => field
                .Required("El campo es obligatorio"))
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

            // Validación Codigosku
            if (string.IsNullOrWhiteSpace(entity.Codigosku))
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
            
            if (entity.Codigosku.Length < 2 || entity.Codigosku.Length > 50)
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

            // Precioventa - Sin validación específica

            // Preciocompra - Sin validación específica

            // MarcaId - Sin validación específica

            // CategoriaId - Sin validación específica

            var createRequest = new CreateRequestBuilder<ProductoEntity>(entity)
                .Build();

            var response = await ProductoService.CreateAsync(createRequest);

            if (response.Success && response.Data != null)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "¡Éxito!",
                    Detail = "Producto creado exitosamente",
                    Duration = 4000
                });
                
                await OnEntityCreated.InvokeAsync(response.Data);
                
                // Resetear el formulario
                entity = new ProductoEntity 
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
                    Detail = response.Message ?? "Error al crear producto",
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