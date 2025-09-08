using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Shared.Models.Entities;
using Shared.Models.Builders;
using Frontend.Services.Validation;
using Frontend.Components.Validation;
using ProductoEntity = Shared.Models.Entities.Producto;
using Radzen;
using System.Linq.Expressions;

namespace Frontend.Modules.Catalogo.Productos;

public partial class ProductoFormulario : ComponentBase
{
    [Inject] private ProductoService ProductoService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private MarcaService MarcaService { get; set; } = null!;
    [Inject] private CategoriaService CategoriaService { get; set; } = null!;
    [Parameter] public Guid? Id { get; set; }

    private ProductoEntity entity = new();
    private bool isLoading = false;
    private bool isEditMode => Id.HasValue;
    private bool isFormValid = false;
    private bool isNewlyCreated = false;
    private Expression<Func<Marca, object>>[] marcaSearchFields = new Expression<Func<Marca, object>>[] { x => x.Nombre };
    private Expression<Func<Categoria, object>>[] categoriaSearchFields = new Expression<Func<Categoria, object>>[] { x => x.Nombre };

    protected override async Task OnInitializedAsync()
    {
        if (isEditMode && Id.HasValue)
        {
            await LoadEntity();
            
            if (Navigation.Uri.Contains("created=true"))
            {
                isNewlyCreated = true;
                
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "¡Éxito!",
                    Detail = "Producto creado exitosamente. Ahora puedes editarlo.",
                    Duration = 5000
                });
                
                Navigation.NavigateTo($"/catalogo/producto/formulario/{Id}", replace: true);
            }
        }
        else
        {
            entity = new ProductoEntity 
            { 
                Active = true,
                FechaCreacion = DateTime.Now,
                FechaModificacion = DateTime.Now
            };
        }
        
        // No lookups to initialize
    }

    private async Task LoadEntity()
    {
        try
        {
            isLoading = true;
            var response = await ProductoService.GetByIdAsync(Id!.Value);
            
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
                    Detail = "No se pudo cargar el producto",
                    Duration = 5000
                });
                Navigation.NavigateTo("/catalogo/producto/list");
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = $"Error cargando producto: {ex.Message}",
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
    
    private FormValidator? formValidator;

    private async Task SaveForm()
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

            if (isEditMode)
            {
                var updateRequest = new UpdateRequestBuilder<ProductoEntity>(entity)
                    .Build();

                var response = await ProductoService.UpdateAsync(updateRequest);

                if (response.Success)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "¡Éxito!",
                        Detail = "Producto actualizado exitosamente",
                        Duration = 4000
                    });
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Error",
                        Detail = response.Message ?? "Error al actualizar producto",
                        Duration = 5000
                    });
                }
            }
            else
            {
                var createRequest = new CreateRequestBuilder<ProductoEntity>(entity)
                    .Build();

                var response = await ProductoService.CreateAsync(createRequest);

                if (response.Success && response.Data != null)
                {
                    entity = response.Data;
                    Navigation.NavigateTo($"/catalogo/producto/formulario/{entity.Id}?created=true");
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