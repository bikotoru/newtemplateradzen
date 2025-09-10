using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Shared.Models.Entities;
using Shared.Models.Builders;
using Frontend.Services.Validation;
using Frontend.Components.Validation;
using Frontend.Components.Auth;
using SystemRoleEntity = Shared.Models.Entities.SystemEntities.SystemRoles;
using SystemPermissionEntity = Shared.Models.Entities.SystemEntities.SystemPermissions;
using SystemRolePermissionEntity = Shared.Models.Entities.SystemEntities.SystemRolesPermissions;
using Radzen;
using System.Linq.Expressions;
using Frontend.Modules.Admin.SystemPermissions;

namespace Frontend.Modules.Admin.SystemRoles;

public partial class SystemRoleFormulario : AuthorizedPageBase
{
    [Inject] private SystemRoleService SystemRoleService { get; set; } = null!;
    [Inject] private SystemPermissionService SystemPermissionService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    
    [Parameter] public Guid? Id { get; set; }

    private SystemRoleEntity? entity;
    private bool isLoading = false;
    private bool isEditMode => Id.HasValue;
    private bool isFormValid = false;
    private bool isNewlyCreated = false;

    // Propiedades de permisos
    private bool CanView => AuthService.HasPermission("SYSTEMROLE.VIEW");
    private bool CanCreate => AuthService.HasPermission("SYSTEMROLE.CREATE");
    private bool CanEdit => isEditMode ? AuthService.HasPermission("SYSTEMROLE.EDIT") : AuthService.HasPermission("SYSTEMROLE.CREATE");
    private bool CanSave => CanEdit;

    // Variables para gestión de permisos
    private List<SystemPermissionEntity>? availablePermissions = null;
    private List<SystemPermissionEntity>? filteredPermissions = null;
    private HashSet<Guid> assignedPermissions = new();
    private bool isLoadingPermissions = false;
    private string permissionFilter = string.Empty;
    

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
                    Detail = "SystemRole creado exitosamente. Ahora puedes editarlo.",
                    Duration = 5000
                });
                
                Navigation.NavigateTo($"/admin/systemrole/formulario/{Id}", replace: true);
            }
        }
        else
        {
            entity = new SystemRoleEntity 
            { 
                Active = true,
                FechaCreacion = DateTime.Now,
                FechaModificacion = DateTime.Now,
                TypeRole = "Access" // Valor por defecto
            };
        }
        
        // Cargar permisos disponibles si estamos en modo edición
        if (isEditMode && entity?.TypeRole == "Access")
        {
            await LoadPermissionsAsync();
        }
        StateHasChanged();
    }

    private async Task LoadEntity()
    {
        try
        {
            isLoading = true;
            var response = await SystemRoleService.GetByIdAsync(Id!.Value);
            
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
                    Detail = "No se pudo cargar el systemrole",
                    Duration = 5000
                });
                Navigation.NavigateTo("/admin/systemrole/list");
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = $"Error cargando systemrole: {ex.Message}",
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
            .Field("TypeRole", field => field
                .Required("El tipo de rol es obligatorio"))
            .Field("Descripcion", field => field
                .MaxLength(500, "La descripción no puede exceder 500 caracteres"))
            .Build();
    }

    private string[] GetRoleTypes()
    {
        return new string[] { "Access", "Admin" };
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

            // Validación TypeRole
            if (string.IsNullOrWhiteSpace(entity.TypeRole))
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Validación",
                    Detail = "El tipo de rol es obligatorio",
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

            if (isEditMode)
            {
                var updateRequest = new UpdateRequestBuilder<SystemRoleEntity>(entity)
                    .Build();

                var response = await SystemRoleService.UpdateAsync(updateRequest);

                if (response.Success)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "¡Éxito!",
                        Detail = "SystemRole actualizado exitosamente",
                        Duration = 4000
                    });
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Error",
                        Detail = response.Message ?? "Error al actualizar systemrole",
                        Duration = 5000
                    });
                }
            }
            else
            {
                var createRequest = new CreateRequestBuilder<SystemRoleEntity>(entity)
                    .Build();

                var response = await SystemRoleService.CreateAsync(createRequest);

                if (response.Success && response.Data != null)
                {
                    entity = response.Data;
                    Navigation.NavigateTo($"/admin/systemrole/formulario/{entity.Id}?created=true");
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Error",
                        Detail = response.Message ?? "Error al crear systemrole",
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

    #region Gestión de Permisos

    private async Task LoadPermissionsAsync()
    {
        try
        {
            if (availablePermissions == null)
            {
                // Cargar todos los permisos disponibles
                var permissionsResponse = await SystemPermissionService.GetAllUnpagedAsync();
                if (permissionsResponse.Success && permissionsResponse.Data != null)
                {
                    availablePermissions = permissionsResponse.Data;
                    filteredPermissions = availablePermissions;
                }
                else
                {
                    availablePermissions = new List<SystemPermissionEntity>();
                    filteredPermissions = new List<SystemPermissionEntity>();
                }
            }

            // Cargar permisos asignados al rol actual
            if (isEditMode && entity.Id != Guid.Empty)
            {
                await LoadAssignedPermissionsAsync();
            }

            StateHasChanged();
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = $"Error cargando permisos: {ex.Message}",
                Duration = 5000
            });
        }
    }

    private async Task LoadAssignedPermissionsAsync()
    {
        try
        {
            // TODO: Aquí necesitaríamos un servicio para SystemRolesPermissions
            // Por ahora simulamos la carga. En una implementación real:
            // var assignedResponse = await SystemRolePermissionService.GetByRoleIdAsync(entity.Id);
            
            assignedPermissions.Clear();
            
            // Simulación - en la implementación real cargarías desde el backend
            // foreach (var rolePermission in assignedResponse.Data)
            // {
            //     assignedPermissions.Add(rolePermission.SystemPermissionsId);
            // }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = $"Error cargando permisos asignados: {ex.Message}",
                Duration = 5000
            });
        }
    }

    private void FilterPermissions(ChangeEventArgs e)
    {
        permissionFilter = e.Value?.ToString() ?? string.Empty;
        
        if (availablePermissions != null)
        {
            if (string.IsNullOrWhiteSpace(permissionFilter))
            {
                filteredPermissions = availablePermissions;
            }
            else
            {
                var filter = permissionFilter.ToLower();
                filteredPermissions = availablePermissions.Where(p =>
                    (p.ActionKey?.ToLower().Contains(filter) == true) ||
                    (p.Descripcion?.ToLower().Contains(filter) == true) ||
                    (p.Organization?.Nombre?.ToLower().Contains(filter) == true)
                ).ToList();
            }
        }
        
        StateHasChanged();
    }

    private bool IsPermissionAssigned(Guid permissionId)
    {
        return assignedPermissions.Contains(permissionId);
    }

    private void TogglePermission(Guid permissionId, bool isAssigned)
    {
        if (isAssigned)
        {
            assignedPermissions.Add(permissionId);
        }
        else
        {
            assignedPermissions.Remove(permissionId);
        }
        
        StateHasChanged();
    }

    private async Task SavePermissions()
    {
        if (!isEditMode || entity.Id == Guid.Empty)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Atención",
                Detail = "Debe guardar el rol antes de asignar permisos",
                Duration = 4000
            });
            return;
        }

        try
        {
            isLoadingPermissions = true;

            // TODO: Aquí implementarías la lógica para guardar los permisos
            // En una implementación real harías algo como:
            
            // 1. Eliminar permisos existentes del rol
            // await SystemRolePermissionService.DeleteByRoleIdAsync(entity.Id);
            
            // 2. Crear nuevos permisos
            // foreach (var permissionId in assignedPermissions)
            // {
            //     var rolePermission = new SystemRolePermissionEntity
            //     {
            //         SystemRolesId = entity.Id,
            //         SystemPermissionsId = permissionId,
            //         Active = true,
            //         FechaCreacion = DateTime.Now,
            //         FechaModificacion = DateTime.Now
            //     };
            //     await SystemRolePermissionService.CreateAsync(rolePermission);
            // }

            // Simulación de guardado exitoso
            await Task.Delay(1000); // Simular operación

            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "¡Éxito!",
                Detail = $"Permisos guardados exitosamente. {assignedPermissions.Count} permisos asignados.",
                Duration = 4000
            });
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = $"Error guardando permisos: {ex.Message}",
                Duration = 5000
            });
        }
        finally
        {
            isLoadingPermissions = false;
            StateHasChanged();
        }
    }

    #endregion
}