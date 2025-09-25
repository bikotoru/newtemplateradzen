using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Shared.Models.Entities;
using Shared.Models.Builders;
using Frontend.Services.Validation;
using Frontend.Components.Validation;
using Frontend.Components.Auth;
using ComunaEntity = Shared.Models.Entities.Comuna;
using Radzen;
using System.Linq.Expressions;

namespace Frontend.Modules.Core.Localidades.Comunas;

public partial class ComunaFormulario : AuthorizedPageBase
{
    [Inject] private ComunaService ComunaService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private RegionService RegionService { get; set; } = null!;
    [Parameter] public Guid? Id { get; set; }

    private ComunaEntity? entity;
    private bool isLoading = false;
    private bool isEditMode => Id.HasValue;
    private bool isFormValid = false;
    private bool isNewlyCreated = false;

    // Propiedades de permisos
    private bool CanView => AuthService.HasPermission("COMUNA.VIEW");
    private bool CanCreate => AuthService.HasPermission("COMUNA.CREATE");
    private bool CanEdit => isEditMode ? AuthService.HasPermission("COMUNA.UPDATE") : AuthService.HasPermission("COMUNA.CREATE");
    private bool CanSave => CanEdit;

    private Expression<Func<Shared.Models.Entities.Region, object>>[] regionSearchFields = new Expression<Func<Shared.Models.Entities.Region, object>>[] { x => x.Nombre };

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync(); // ¡IMPORTANTE! Siempre llamar primero al base para verificar permisos
        
        if (HasRequiredPermissions)
        {
            await OnPermissionsVerifiedAsync();
        }
    }

    protected override async Task OnPermissionsVerifiedAsync()
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
                    Detail = "Comuna creado exitosamente. Ahora puedes editarlo.",
                    Duration = 5000
                });
                
                Navigation.NavigateTo($"/core/localidades/comuna/formulario/{Id}", replace: true);
            }
        }
        else
        {
            entity = new ComunaEntity 
            { 
                Active = true,
                FechaCreacion = DateTime.Now,
                FechaModificacion = DateTime.Now
            };
        }
        
        // No lookups to initialize
        StateHasChanged();
    }

    private async Task LoadEntity()
    {
        try
        {
            isLoading = true;
            await DialogService.OpenLoadingAsync("Obteniendo datos...");
            var response = await ComunaService.GetByIdAsync(Id!.Value);
            
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
                    Detail = "No se pudo cargar el comuna",
                    Duration = 5000
                });
                Navigation.NavigateTo("/core/localidades/comuna/list");
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = $"Error cargando comuna: {ex.Message}",
                Duration = 5000
            });
        }
        finally
        {
            isLoading = false;
            DialogService.Close();
            StateHasChanged();
        }
    }

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
    
    private FormValidator? formValidator;

    private async Task SaveForm()
    {
        try
        {
            // Verificar permisos antes de continuar
            if (!CanSave)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Permisos Insuficientes",
                    Detail = isEditMode ? 
                        "No tienes permisos para editar este registro" : 
                        "No tienes permisos para crear nuevos registros",
                    Duration = 4000
                });
                return;
            }

            if (entity == null)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = "Error interno: entidad no inicializada",
                    Duration = 4000
                });
                return;
            }

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

            if (isEditMode)
            {
                var updateRequest = new UpdateRequestBuilder<ComunaEntity>(entity)
                    .Build();

                await DialogService.OpenLoadingAsync("Actualizando...");
                var response = await ComunaService.UpdateAsync(updateRequest);
                DialogService.Close();

                if (response.Success)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "¡Éxito!",
                        Detail = "Comuna actualizado exitosamente",
                        Duration = 4000
                    });
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Error",
                        Detail = response.Message ?? "Error al actualizar comuna",
                        Duration = 5000
                    });
                }
            }
            else
            {
                var createRequest = new CreateRequestBuilder<ComunaEntity>(entity)
                    .Build();

                await DialogService.OpenLoadingAsync("Creando...");
                var response = await ComunaService.CreateAsync(createRequest);
                DialogService.Close();

                if (response.Success && response.Data != null)
                {
                    entity = response.Data;
                    Navigation.NavigateTo($"/core/localidades/comuna/formulario/{entity.Id}?created=true");
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
