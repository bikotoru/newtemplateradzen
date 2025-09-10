using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Models.DTOs.RolePermissions;
using Shared.Models.QueryModels;
using Shared.Models.Responses;
using Frontend.Services;

namespace Frontend.Modules.Admin.SystemPermissions.Components;

public partial class PermissionRolesManager : ComponentBase
{
    [Inject] private SystemPermissionService SystemPermissionService { get; set; } = default!;
    [Inject] private SystemRoleService SystemRoleService { get; set; } = default!;
    [Inject] private NotificationService NotificationService { get; set; } = default!;
    [Inject] private DialogService DialogService { get; set; } = default!;

    [Parameter] public Guid PermissionId { get; set; }
    [Parameter] public string PermissionName { get; set; } = string.Empty;
    [Parameter] public bool CanEdit { get; set; } = true;

    private bool isLoading = false;
    private string? errorMessage = null;
    private string searchTerm = string.Empty;
    private bool showOnlyWithPermission = true;
    private int currentPage = 1;
    private int pageSize = 10;
    private int totalPages = 1;
    
    private List<PermissionRoleDto>? permissionRoles;
    private PagedResult<PermissionRoleDto>? pagedResult;

    protected override async Task OnInitializedAsync()
    {
        await LoadRoles();
    }

    private async Task LoadRoles()
    {
        try
        {
            isLoading = true;
            errorMessage = null;

            var request = new PermissionRoleSearchRequest
            {
                PermissionId = PermissionId,
                SearchTerm = searchTerm,
                ShowOnlyWithPermission = showOnlyWithPermission,
                Page = currentPage,
                PageSize = pageSize
            };

            pagedResult = await SystemPermissionService.GetPermissionRolesPagedAsync(request);
            permissionRoles = pagedResult?.Data?.ToList();
            totalPages = pagedResult != null ? (int)Math.Ceiling((double)pagedResult.TotalCount / pagedResult.PageSize) : 1;
        }
        catch (Exception ex)
        {
            errorMessage = $"Error al cargar roles: {ex.Message}";
            permissionRoles = null;
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task OnSearchChanged(ChangeEventArgs e)
    {
        searchTerm = e.Value?.ToString() ?? string.Empty;
        currentPage = 1;
        await LoadRoles();
    }

    private async Task OnShowOnlyWithPermissionChanged(bool value)
    {
        showOnlyWithPermission = value;
        currentPage = 1;
        await LoadRoles();
    }

    private async Task OnPageChanged(int page)
    {
        currentPage = page;
        await LoadRoles();
    }

    private string GetRoleItemStyle(PermissionRoleDto role)
    {
        return role.HasPermission 
            ? "border-left: 4px solid var(--rz-success);" 
            : "border-left: 4px solid var(--rz-border-color);";
    }

    private async Task ShowAddRoleModal()
    {
        if (!CanEdit)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Sin permisos",
                Detail = "No tienes permisos para asignar permisos a roles"
            });
            return;
        }

        try
        {
            // Obtener roles disponibles (sin el permiso)
            var availableRolesRequest = new PermissionRoleSearchRequest
            {
                PermissionId = PermissionId,
                ShowOnlyWithPermission = false,
                Page = 1,
                PageSize = 100
            };

            var availableRolesResult = await SystemPermissionService.GetPermissionRolesPagedAsync(availableRolesRequest);
            var availableRoles = availableRolesResult?.Data?.Where(r => !r.HasPermission && r.Active).ToList() ?? new List<PermissionRoleDto>();

            if (!availableRoles.Any())
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Info,
                    Summary = "Sin roles disponibles",
                    Detail = "Todos los roles activos ya tienen este permiso"
                });
                return;
            }

            // TODO: Crear modal de selección de rol
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Info,
                Summary = "Funcionalidad pendiente",
                Detail = "Modal de selección de rol por implementar"
            });
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = $"Error al obtener roles disponibles: {ex.Message}"
            });
        }
    }

    private async Task ShowAssignPermissionModal(PermissionRoleDto role)
    {
        if (!CanEdit)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Sin permisos",
                Detail = "No tienes permisos para asignar permisos a roles"
            });
            return;
        }

        var confirmed = await DialogService.Confirm(
            $"¿Estás seguro de que deseas asignar el permiso '{PermissionName}' al rol '{role.Nombre}'?",
            "Confirmar Asignación",
            new ConfirmOptions
            {
                OkButtonText = "Sí, Asignar",
                CancelButtonText = "Cancelar"
            }
        );

        if (confirmed == true)
        {
            await AssignPermissionToRole(role);
        }
    }

    private async Task ShowRemovePermissionModal(PermissionRoleDto role)
    {
        if (!CanEdit)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Sin permisos",
                Detail = "No tienes permisos para remover permisos de roles"
            });
            return;
        }

        var confirmed = await DialogService.Confirm(
            $"¿Estás seguro de que deseas remover el permiso '{PermissionName}' del rol '{role.Nombre}'?",
            "Confirmar Remoción",
            new ConfirmOptions
            {
                OkButtonText = "Sí, Remover",
                CancelButtonText = "Cancelar"
            }
        );

        if (confirmed == true)
        {
            await RemovePermissionFromRole(role);
        }
    }

    private async Task AssignPermissionToRole(PermissionRoleDto role)
    {
        try
        {
            isLoading = true;
            
            // TODO: Implementar asignación de permiso a rol
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Info,
                Summary = "Funcionalidad pendiente",
                Detail = "Asignación de permiso a rol por implementar"
            });

            await LoadRoles();
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error al asignar permiso",
                Detail = $"No se pudo asignar el permiso: {ex.Message}"
            });
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task RemovePermissionFromRole(PermissionRoleDto role)
    {
        try
        {
            isLoading = true;
            
            // TODO: Implementar remoción de permiso de rol
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Info,
                Summary = "Funcionalidad pendiente",
                Detail = "Remoción de permiso de rol por implementar"
            });

            await LoadRoles();
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error al remover permiso",
                Detail = $"No se pudo remover el permiso: {ex.Message}"
            });
        }
        finally
        {
            isLoading = false;
        }
    }
}