using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Models.DTOs.UserPermissions;
using Shared.Models.QueryModels;
using Shared.Models.Responses;
using Frontend.Services;

namespace Frontend.Modules.Admin.SystemPermissions.Components;

public partial class PermissionUsersManager : ComponentBase
{
    [Inject] private SystemPermissionService SystemPermissionService { get; set; } = default!;
    [Inject] private SystemUserService SystemUserService { get; set; } = default!;
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
    
    private List<PermissionUserDto>? permissionUsers;
    private PagedResult<PermissionUserDto>? pagedResult;

    protected override async Task OnInitializedAsync()
    {
        await LoadUsers();
    }

    private async Task LoadUsers()
    {
        try
        {
            isLoading = true;
            errorMessage = null;

            var request = new PermissionUserSearchRequest
            {
                PermissionId = PermissionId,
                SearchTerm = searchTerm,
                ShowOnlyWithPermission = showOnlyWithPermission,
                Page = currentPage,
                PageSize = pageSize
            };

            pagedResult = await SystemPermissionService.GetPermissionUsersPagedAsync(request);
            permissionUsers = pagedResult?.Data?.ToList();
            totalPages = pagedResult != null ? (int)Math.Ceiling((double)pagedResult.TotalCount / pagedResult.PageSize) : 1;
        }
        catch (Exception ex)
        {
            errorMessage = $"Error al cargar usuarios: {ex.Message}";
            permissionUsers = null;
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
        await LoadUsers();
    }

    private async Task OnShowOnlyWithPermissionChanged(bool value)
    {
        showOnlyWithPermission = value;
        currentPage = 1;
        await LoadUsers();
    }

    private async Task OnPageChanged(int page)
    {
        currentPage = page;
        await LoadUsers();
    }

    private string GetUserItemStyle(PermissionUserDto user)
    {
        if (user.IsDirectlyAssigned)
            return "border-left: 4px solid var(--rz-success);";
        else if (user.IsInheritedFromRole)
            return "border-left: 4px solid var(--rz-info);";
        else
            return "border-left: 4px solid var(--rz-border-color);";
    }

    private async Task ShowAddUserModal()
    {
        if (!CanEdit)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Sin permisos",
                Detail = "No tienes permisos para asignar permisos a usuarios"
            });
            return;
        }

        try
        {
            // Obtener usuarios disponibles (sin el permiso)
            var availableUsersRequest = new PermissionUserSearchRequest
            {
                PermissionId = PermissionId,
                ShowOnlyWithPermission = false,
                Page = 1,
                PageSize = 100
            };

            var availableUsersResult = await SystemPermissionService.GetPermissionUsersPagedAsync(availableUsersRequest);
            var availableUsers = availableUsersResult?.Data?.Where(u => !u.HasPermission).ToList() ?? new List<PermissionUserDto>();

            if (!availableUsers.Any())
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Info,
                    Summary = "Sin usuarios disponibles",
                    Detail = "Todos los usuarios ya tienen este permiso"
                });
                return;
            }

            // TODO: Crear modal de selección de usuario
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Info,
                Summary = "Funcionalidad pendiente",
                Detail = "Modal de selección de usuario por implementar"
            });
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = $"Error al obtener usuarios disponibles: {ex.Message}"
            });
        }
    }

    private async Task ShowAssignPermissionModal(PermissionUserDto user)
    {
        if (!CanEdit)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Sin permisos",
                Detail = "No tienes permisos para asignar permisos a usuarios"
            });
            return;
        }

        var confirmed = await DialogService.Confirm(
            $"¿Estás seguro de que deseas asignar el permiso '{PermissionName}' directamente al usuario '{user.Nombre}'?",
            "Confirmar Asignación",
            new ConfirmOptions
            {
                OkButtonText = "Sí, Asignar",
                CancelButtonText = "Cancelar"
            }
        );

        if (confirmed == true)
        {
            await AssignPermissionToUser(user);
        }
    }

    private async Task ShowRemovePermissionModal(PermissionUserDto user)
    {
        if (!CanEdit)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Sin permisos",
                Detail = "No tienes permisos para remover permisos de usuarios"
            });
            return;
        }

        var confirmed = await DialogService.Confirm(
            $"¿Estás seguro de que deseas remover el permiso '{PermissionName}' del usuario '{user.Nombre}'?",
            "Confirmar Remoción",
            new ConfirmOptions
            {
                OkButtonText = "Sí, Remover",
                CancelButtonText = "Cancelar"
            }
        );

        if (confirmed == true)
        {
            await RemovePermissionFromUser(user);
        }
    }

    private async Task AssignPermissionToUser(PermissionUserDto user)
    {
        try
        {
            isLoading = true;
            
            // TODO: Implementar asignación de permiso
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Info,
                Summary = "Funcionalidad pendiente",
                Detail = "Asignación de permiso por implementar"
            });

            await LoadUsers();
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

    private async Task RemovePermissionFromUser(PermissionUserDto user)
    {
        try
        {
            isLoading = true;
            
            // TODO: Implementar remoción de permiso
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Info,
                Summary = "Funcionalidad pendiente",
                Detail = "Remoción de permiso por implementar"
            });

            await LoadUsers();
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