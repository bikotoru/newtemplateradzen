using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Shared.Models.Entities;
using Shared.Models.Builders;
using Frontend.Services.Validation;
using Frontend.Components.Validation;
using Frontend.Components.Auth;
using SystemUserEntity = Shared.Models.Entities.SystemEntities.SystemUsers;
using Radzen;
using System.Linq.Expressions;

namespace Frontend.Modules.Admin.SystemUsers;

public partial class SystemUserFormulario : AuthorizedPageBase
{
    [Inject] private SystemUserService SystemUserService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    
    [Parameter] public Guid? Id { get; set; }

    private SystemUserEntity? entity;
    private bool isLoading = false;
    private bool isEditMode => Id.HasValue;
    private bool isFormValid = false;
    private bool isNewlyCreated = false;

    // Propiedades de permisos
    private bool CanView => AuthService.HasPermission("SYSTEMUSER.VIEW");
    private bool CanCreate => AuthService.HasPermission("SYSTEMUSER.CREATE");
    private bool CanEdit => isEditMode ? AuthService.HasPermission("SYSTEMUSER.EDIT") : AuthService.HasPermission("SYSTEMUSER.CREATE");
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
                    Detail = "SystemUser creado exitosamente. Ahora puedes editarlo.",
                    Duration = 5000
                });
                
                Navigation.NavigateTo($"/admin/systemuser/formulario/{Id}", replace: true);
            }
        }
        else
        {
            entity = new SystemUserEntity 
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
            var response = await SystemUserService.GetByIdAsync(Id!.Value);
            
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
                    Detail = "No se pudo cargar el systemuser",
                    Duration = 5000
                });
                Navigation.NavigateTo("/admin/systemuser/list");
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = $"Error cargando systemuser: {ex.Message}",
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
                .MaxLength(500, "La descripción no puede exceder 500 caracteres"))
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
                var updateRequest = new UpdateRequestBuilder<SystemUserEntity>(entity)
                    .Build();

                var response = await SystemUserService.UpdateAsync(updateRequest);

                if (response.Success)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "¡Éxito!",
                        Detail = "SystemUser actualizado exitosamente",
                        Duration = 4000
                    });
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Error",
                        Detail = response.Message ?? "Error al actualizar systemuser",
                        Duration = 5000
                    });
                }
            }
            else
            {
                var createRequest = new CreateRequestBuilder<SystemUserEntity>(entity)
                    .Build();

                var response = await SystemUserService.CreateAsync(createRequest);

                if (response.Success && response.Data != null)
                {
                    entity = response.Data;
                    Navigation.NavigateTo($"/admin/systemuser/formulario/{entity.Id}?created=true");
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Error",
                        Detail = response.Message ?? "Error al crear systemuser",
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