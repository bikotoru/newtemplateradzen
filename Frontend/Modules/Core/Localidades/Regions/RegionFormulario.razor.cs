using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Shared.Models.Entities;
using Shared.Models.Builders;
using Frontend.Services.Validation;
using Frontend.Components.Validation;
using Frontend.Components.Auth;
using RegionEntity = Shared.Models.Entities.Region;
using Radzen;
using System.Linq.Expressions;

namespace Frontend.Modules.Core.Localidades.Regions;

public partial class RegionFormulario : AuthorizedPageBase
{
    [Inject] private RegionService RegionService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    
    [Parameter] public Guid? Id { get; set; }

    private RegionEntity? entity;
    private bool isLoading = false;
    private bool isEditMode => Id.HasValue;
    private bool isFormValid = false;
    private bool isNewlyCreated = false;

    // Propiedades de permisos
    private bool CanView => AuthService.HasPermission("REGION.VIEW");
    private bool CanCreate => AuthService.HasPermission("REGION.CREATE");
    private bool CanEdit => isEditMode ? AuthService.HasPermission("REGION.UPDATE") : AuthService.HasPermission("REGION.CREATE");
    private bool CanSave => CanEdit;

    

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
                    Detail = "Region creado exitosamente. Ahora puedes editarlo.",
                    Duration = 5000
                });
                
                Navigation.NavigateTo($"/core/localidades/region/formulario/{Id}", replace: true);
            }
        }
        else
        {
            entity = new RegionEntity 
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
            var response = await RegionService.GetByIdAsync(Id!.Value);
            
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
                    Detail = "No se pudo cargar el region",
                    Duration = 5000
                });
                Navigation.NavigateTo("/core/localidades/region/list");
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = $"Error cargando region: {ex.Message}",
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
            .Field("Nombre", field => field
                .Required("El nombre es obligatorio")
                .Length(3, 100, "El nombre debe tener entre 3 y 100 caracteres"))
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

            if (isEditMode)
            {
                var updateRequest = new UpdateRequestBuilder<RegionEntity>(entity)
                    .Build();

                await DialogService.OpenLoadingAsync("Actualizando...");
                var response = await RegionService.UpdateAsync(updateRequest);
                DialogService.Close();

                if (response.Success)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "¡Éxito!",
                        Detail = "Region actualizado exitosamente",
                        Duration = 4000
                    });
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Error",
                        Detail = response.Message ?? "Error al actualizar region",
                        Duration = 5000
                    });
                }
            }
            else
            {
                var createRequest = new CreateRequestBuilder<RegionEntity>(entity)
                    .Build();

                await DialogService.OpenLoadingAsync("Creando...");
                var response = await RegionService.CreateAsync(createRequest);
                DialogService.Close();

                if (response.Success && response.Data != null)
                {
                    entity = response.Data;
                    Navigation.NavigateTo($"/core/localidades/region/formulario/{entity.Id}?created=true");
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Error",
                        Detail = response.Message ?? "Error al crear region",
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
