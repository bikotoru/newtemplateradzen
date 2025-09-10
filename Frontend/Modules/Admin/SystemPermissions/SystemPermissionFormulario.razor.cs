using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Shared.Models.Entities;
using Shared.Models.Builders;
using Frontend.Services.Validation;
using Frontend.Components.Validation;
using Frontend.Components.Auth;
using SystemPermissionEntity = Shared.Models.Entities.SystemEntities.SystemPermissions;
using Radzen;
using System.Linq.Expressions;

namespace Frontend.Modules.Admin.SystemPermissions;

public partial class SystemPermissionFormulario : AuthorizedPageBase
{
    [Inject] private SystemPermissionService SystemPermissionService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    
    [Parameter] public Guid? Id { get; set; }

    private SystemPermissionEntity? entity;
    private bool isLoading = false;
    private bool isEditMode => Id.HasValue;
    private bool isFormValid = false;
    private bool isNewlyCreated = false;

    // Propiedades de permisos
    private bool CanView => AuthService.HasPermission("SYSTEMPERMISSION.VIEW");
    private bool CanCreate => AuthService.HasPermission("SYSTEMPERMISSION.CREATE");
    private bool CanEdit => isEditMode ? AuthService.HasPermission("SYSTEMPERMISSION.EDIT") : AuthService.HasPermission("SYSTEMPERMISSION.CREATE");
    private bool CanSave => CanEdit;
    
    // Validación ActionKey en tiempo real
    private ActionKeyValidationState actionKeyValidationState = ActionKeyValidationState.None;
    private System.Timers.Timer? debounceTimer;
    
    // Lookup GrupoNombre
    private List<string> gruposExistentes = new();
    

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
                    Detail = "SystemPermission creado exitosamente. Ahora puedes editarlo.",
                    Duration = 5000
                });
                
                Navigation.NavigateTo($"/admin/systempermission/formulario/{Id}", replace: true);
            }
        }
        else
        {
            entity = new SystemPermissionEntity 
            { 
                Active = true,
                FechaCreacion = DateTime.Now,
                FechaModificacion = DateTime.Now
            };
        }
        
        // Cargar grupos existentes
        await LoadGruposExistentes();
        StateHasChanged();
    }

    private async Task LoadEntity()
    {
        try
        {
            isLoading = true;
            var response = await SystemPermissionService.GetByIdAsync(Id!.Value);
            
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
                    Detail = "No se pudo cargar el systempermission",
                    Duration = 5000
                });
                Navigation.NavigateTo("/admin/systempermission/list");
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = $"Error cargando systempermission: {ex.Message}",
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

            // Validación Nombre/ActionKey
            if (string.IsNullOrWhiteSpace(entity.Nombre))
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Validación",
                    Detail = "El Action Key es obligatorio",
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
                    Detail = "El Action Key debe tener entre 3 y 100 caracteres",
                    Duration = 4000
                });
                return;
            }

            // Verificar ActionKey único si cambió
            if (actionKeyValidationState != ActionKeyValidationState.Valid && !isEditMode)
            {
                var isValid = await ValidateActionKeyUnique();
                if (!isValid)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Warning,
                        Summary = "Validación",
                        Detail = "Este Action Key ya existe. Por favor, utiliza uno diferente.",
                        Duration = 4000
                    });
                    return;
                }
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

            // Sincronizar ActionKey = Nombre antes de guardar
            entity.ActionKey = entity.Nombre;

            if (isEditMode)
            {
                var updateRequest = new UpdateRequestBuilder<SystemPermissionEntity>(entity)
                    .Build();

                var response = await SystemPermissionService.UpdateAsync(updateRequest);

                if (response.Success)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "¡Éxito!",
                        Detail = "SystemPermission actualizado exitosamente",
                        Duration = 4000
                    });
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Error",
                        Detail = response.Message ?? "Error al actualizar systempermission",
                        Duration = 5000
                    });
                }
            }
            else
            {
                var createRequest = new CreateRequestBuilder<SystemPermissionEntity>(entity)
                    .Build();

                var response = await SystemPermissionService.CreateAsync(createRequest);

                if (response.Success && response.Data != null)
                {
                    entity = response.Data;
                    Navigation.NavigateTo($"/admin/systempermission/formulario/{entity.Id}?created=true");
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Error",
                        Detail = response.Message ?? "Error al crear systempermission",
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

    #region Validación ActionKey en tiempo real
    
    private async Task OnActionKeyChanged(Microsoft.AspNetCore.Components.ChangeEventArgs e)
    {
        entity.Nombre = e.Value?.ToString() ?? "";
        entity.ActionKey = entity.Nombre; // Sincronizar siempre
        
        // Cancelar timer anterior
        debounceTimer?.Stop();
        debounceTimer?.Dispose();
        
        if (string.IsNullOrWhiteSpace(entity.Nombre))
        {
            actionKeyValidationState = ActionKeyValidationState.None;
            StateHasChanged();
            return;
        }
        
        if (entity.Nombre.Length < 3)
        {
            actionKeyValidationState = ActionKeyValidationState.None;
            StateHasChanged();
            return;
        }
        
        // Estado de verificación
        actionKeyValidationState = ActionKeyValidationState.Checking;
        StateHasChanged();
        
        // Debounce de 800ms
        debounceTimer = new System.Timers.Timer(800);
        debounceTimer.Elapsed += async (sender, args) =>
        {
            debounceTimer.Stop();
            await InvokeAsync(async () =>
            {
                await ValidateActionKeyUnique();
                StateHasChanged();
            });
        };
        debounceTimer.Start();
    }
    
    private async Task<bool> ValidateActionKeyUnique()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(entity.Nombre))
            {
                actionKeyValidationState = ActionKeyValidationState.None;
                return false;
            }
            
            var response = await SystemPermissionService.ValidateActionKeyAsync(entity.Nombre, isEditMode ? entity.Id : null);
            
            if (response.Success)
            {
                actionKeyValidationState = response.Data ? ActionKeyValidationState.Valid : ActionKeyValidationState.Invalid;
                return response.Data;
            }
            else
            {
                actionKeyValidationState = ActionKeyValidationState.None;
                return false;
            }
        }
        catch (Exception ex)
        {
            actionKeyValidationState = ActionKeyValidationState.None;
            return false;
        }
    }
    
    #endregion
    
    #region Lookup GrupoNombre
    
    private async Task LoadGruposExistentes()
    {
        try
        {
            var response = await SystemPermissionService.GetGruposExistentesAsync();
            if (response.Success && response.Data != null)
            {
                gruposExistentes = response.Data;
            }
        }
        catch (Exception ex)
        {
            // Log error but don't show notification for optional data
            gruposExistentes = new List<string>();
        }
    }
    
    #endregion
    
    public void Dispose()
    {
        debounceTimer?.Stop();
        debounceTimer?.Dispose();
    }
}

public enum ActionKeyValidationState
{
    None,
    Checking,
    Valid,
    Invalid
}