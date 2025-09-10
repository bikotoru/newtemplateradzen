using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Models.DTOs.RoleUsers;
using Shared.Models.QueryModels;
using Shared.Models.Responses;
using Frontend.Services;
using Frontend.Modules.Admin.SystemRoles.Components.Modals;

namespace Frontend.Modules.Admin.SystemRoles.Components;

public partial class SystemRoleUsersManager : ComponentBase
{
    [Inject] private SystemRoleService SystemRoleService { get; set; } = default!;
    [Inject] private NotificationService NotificationService { get; set; } = default!;
    [Inject] private DialogService DialogService { get; set; } = default!;

    [Parameter] public Guid RoleId { get; set; }
    [Parameter] public string RoleName { get; set; } = string.Empty;
    [Parameter] public bool CanEdit { get; set; } = true;

    private bool isLoading = false;
    private string? errorMessage = null;
    private string searchTerm = string.Empty;
    private bool showOnlyAssigned = true;
    private int currentPage = 1;
    private int pageSize = 10;
    private int totalPages = 1;
    
    private List<RoleUserDto>? roleUsers;
    private PagedResult<RoleUserDto>? pagedResult;

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

            var request = new RoleUserSearchRequest
            {
                RoleId = RoleId,
                SearchTerm = searchTerm,
                ShowOnlyAssigned = showOnlyAssigned,
                Page = currentPage,
                PageSize = pageSize
            };

            pagedResult = await SystemRoleService.GetRoleUsersPagedAsync(request);
            roleUsers = pagedResult?.Data?.ToList();
            totalPages = pagedResult != null ? (int)Math.Ceiling((double)pagedResult.TotalCount / pagedResult.PageSize) : 1;
        }
        catch (Exception ex)
        {
            errorMessage = $"Error al cargar usuarios: {ex.Message}";
            roleUsers = null;
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

    private async Task OnShowOnlyAssignedChanged(bool value)
    {
        showOnlyAssigned = value;
        currentPage = 1;
        await LoadUsers();
    }

    private async Task OnPageChanged(int page)
    {
        currentPage = page;
        await LoadUsers();
    }

    private string GetUserItemStyle(RoleUserDto user)
    {
        return user.IsAssigned 
            ? "border-left: 4px solid var(--rz-success);" 
            : "border-left: 4px solid var(--rz-border-color);";
    }

    private async Task ShowAddUserModal()
    {
        if (!CanEdit)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Sin permisos",
                Detail = "No tienes permisos para agregar usuarios a roles"
            });
            return;
        }

        try
        {
            // Obtener usuarios disponibles (no asignados al rol)
            var availableUsersRequest = new RoleUserSearchRequest
            {
                RoleId = RoleId,
                ShowOnlyAssigned = false,
                Page = 1,
                PageSize = 100 // Obtener más usuarios para el modal
            };

            var availableUsersResult = await SystemRoleService.GetRoleUsersPagedAsync(availableUsersRequest);
            var availableUsers = availableUsersResult?.Data?.Where(u => !u.IsAssigned).ToList() ?? new List<RoleUserDto>();

            if (!availableUsers.Any())
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Info,
                    Summary = "Sin usuarios disponibles",
                    Detail = "Todos los usuarios ya están asignados a este rol"
                });
                return;
            }

            var result = await DialogService.OpenAsync<SelectUserModal>(
                "Agregar Usuario al Rol",
                new Dictionary<string, object>
                {
                    { "RoleName", RoleName },
                    { "AvailableUsers", availableUsers }
                },
                new DialogOptions
                {
                    Width = "600px",
                    Height = "500px",
                    Resizable = true,
                    Draggable = true
                }
            );

            if (result is RoleUserDto selectedUser)
            {
                await AssignUserToRole(selectedUser);
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = $"Error al abrir modal de selección: {ex.Message}"
            });
        }
    }

    private async Task ShowAssignUserModal(RoleUserDto user)
    {
        if (!CanEdit)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Sin permisos",
                Detail = "No tienes permisos para asignar usuarios a roles"
            });
            return;
        }

        var confirmed = await DialogService.Confirm(
            $"¿Estás seguro de que deseas asignar el usuario '{user.Nombre}' al rol '{RoleName}'?",
            "Confirmar Asignación",
            new ConfirmOptions
            {
                OkButtonText = "Sí, Asignar",
                CancelButtonText = "Cancelar"
            }
        );

        if (confirmed == true)
        {
            await AssignUserToRole(user);
        }
    }

    private async Task ShowRemoveUserModal(RoleUserDto user)
    {
        if (!CanEdit)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Sin permisos",
                Detail = "No tienes permisos para remover usuarios de roles"
            });
            return;
        }

        var confirmed = await DialogService.Confirm(
            $"¿Estás seguro de que deseas remover el usuario '{user.Nombre}' del rol '{RoleName}'?",
            "Confirmar Remoción",
            new ConfirmOptions
            {
                OkButtonText = "Sí, Remover",
                CancelButtonText = "Cancelar"
            }
        );

        if (confirmed == true)
        {
            await RemoveUserFromRole(user);
        }
    }

    private async Task AssignUserToRole(RoleUserDto user)
    {
        try
        {
            isLoading = true;
            
            var request = new AssignUserToRoleRequest
            {
                RoleId = RoleId,
                UserId = user.UserId
            };

            await SystemRoleService.AssignUserToRoleAsync(request);

            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Usuario asignado",
                Detail = $"El usuario '{user.Nombre}' ha sido asignado al rol '{RoleName}' exitosamente"
            });

            await LoadUsers();
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error al asignar usuario",
                Detail = $"No se pudo asignar el usuario: {ex.Message}"
            });
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task RemoveUserFromRole(RoleUserDto user)
    {
        try
        {
            isLoading = true;
            
            var request = new RemoveUserFromRoleRequest
            {
                RoleId = RoleId,
                UserId = user.UserId
            };

            await SystemRoleService.RemoveUserFromRoleAsync(request);

            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Usuario removido",
                Detail = $"El usuario '{user.Nombre}' ha sido removido del rol '{RoleName}' exitosamente"
            });

            await LoadUsers();
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error al remover usuario",
                Detail = $"No se pudo remover el usuario: {ex.Message}"
            });
        }
        finally
        {
            isLoading = false;
        }
    }
}