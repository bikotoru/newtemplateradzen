using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Shared.Models.DTOs.UserRoles;
using Shared.Models.QueryModels;
using Shared.Models.Responses;
using Radzen;
using Radzen.Blazor;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Frontend.Modules.Admin.SystemUsers.Components
{
    /// <summary>
    /// Componente para gestionar roles de usuarios del sistema
    /// </summary>
    public partial class SystemUserRolesManager : ComponentBase, IDisposable
    {
        [Inject] private SystemUserService SystemUserService { get; set; } = default!;
        [Inject] private DialogService DialogService { get; set; } = default!;
        [Inject] private NotificationService NotificationService { get; set; } = default!;

        /// <summary>
        /// ID del usuario para gestionar sus roles
        /// </summary>
        [Parameter] public Guid UserId { get; set; }

        /// <summary>
        /// Nombre del usuario (para mostrar en confirmaciones)
        /// </summary>
        [Parameter] public string? UserName { get; set; }

        #region Estado del componente

        // Datos de roles
        private PagedResult<UserRoleDto>? pagedResult;
        private List<UserRoleDto> roles = new();

        // Estado de la UI
        private bool isLoading = false;
        private string? errorMessage = null;

        // Filtros y búsqueda
        private string searchTerm = string.Empty;
        private bool showOnlyActive = true;

        // Paginación
        private int currentPage = 1;
        private int pageSize = 10;
        private int totalPages => pagedResult != null ? (int)Math.Ceiling((double)pagedResult.TotalCount / pageSize) : 0;

        // Timer para debounce de búsqueda
        private System.Timers.Timer? searchDebounceTimer;

        #endregion

        #region Eventos del ciclo de vida

        protected override async Task OnInitializedAsync()
        {
            await LoadRolesAsync();
        }

        public void Dispose()
        {
            searchDebounceTimer?.Dispose();
        }

        #endregion

        #region Métodos de carga de datos

        /// <summary>
        /// Cargar roles del usuario con los filtros actuales
        /// </summary>
        private async Task LoadRolesAsync()
        {
            // No cargar si no hay UserId válido
            if (UserId == Guid.Empty)
            {
                pagedResult = null;
                roles = new();
                return;
            }

            isLoading = true;
            errorMessage = null;

            try
            {
                var request = new UserRoleSearchRequest
                {
                    UserId = UserId,
                    Filter = string.IsNullOrWhiteSpace(searchTerm) ? null : $"Nombre.Contains(\"{searchTerm}\")",
                    ShowOnlyActive = showOnlyActive,
                    Skip = (currentPage - 1) * pageSize,
                    Take = pageSize
                };

                var response = await SystemUserService.GetUserRolesPagedAsync(UserId, request);
                
                if (response.Success && response.Data != null)
                {
                    pagedResult = response.Data;
                    roles = response.Data.Data;
                }
                else
                {
                    errorMessage = response.Message ?? "Error al cargar roles";
                    pagedResult = null;
                    roles = new();
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error al cargar roles: {ex.Message}";
                pagedResult = null;
                roles = new();
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        #endregion

        #region Eventos de filtros y búsqueda

        /// <summary>
        /// Manejar cambio en el término de búsqueda
        /// </summary>
        private void OnSearchChanged(ChangeEventArgs e)
        {
            searchTerm = e.Value?.ToString() ?? string.Empty;
            
            // Debounce search
            searchDebounceTimer?.Stop();
            searchDebounceTimer?.Dispose();
            
            searchDebounceTimer = new System.Timers.Timer(300);
            searchDebounceTimer.Elapsed += async (sender, args) =>
            {
                searchDebounceTimer.Stop();
                await InvokeAsync(async () =>
                {
                    currentPage = 1; // Reset to first page
                    await LoadRolesAsync();
                });
            };
            searchDebounceTimer.Start();
        }

        /// <summary>
        /// Manejar cambio en filtro de solo activos
        /// </summary>
        private async Task OnShowOnlyActiveChanged(bool value)
        {
            showOnlyActive = value;
            currentPage = 1; // Reset to first page
            await LoadRolesAsync();
        }

        /// <summary>
        /// Manejar cambio de página
        /// </summary>
        private async Task OnPageChanged(int page)
        {
            currentPage = page;
            await LoadRolesAsync();
        }

        #endregion

        #region Gestión de roles

        /// <summary>
        /// Mostrar modal para agregar rol
        /// </summary>
        private async Task ShowAddRoleModal()
        {
            if (UserId == Guid.Empty)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Usuario requerido",
                    Detail = "Debe seleccionar un usuario para asignar roles",
                    Duration = 3000
                });
                return;
            }

            try
            {
                var result = await DialogService.OpenAsync<AddRoleToUserModal>(
                    "Agregar Rol al Usuario",
                    new Dictionary<string, object>
                    {
                        { "UserId", UserId },
                        { "UserName", UserName ?? "Usuario" }
                    },
                    new DialogOptions()
                    {
                        Width = "800px",
                        Height = "600px",
                        Resizable = true,
                        Draggable = true,
                        CloseDialogOnOverlayClick = false,
                        ShowClose = true
                    }
                );

                // Si se seleccionó un rol, recargar la lista
                if (result is AvailableRoleDto selectedRole)
                {
                    await AssignRoleToUser(selectedRole);
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = $"Error al abrir modal: {ex.Message}",
                    Duration = 5000
                });
            }
        }

        /// <summary>
        /// Asignar rol al usuario
        /// </summary>
        private async Task AssignRoleToUser(AvailableRoleDto role)
        {
            try
            {
                var response = await SystemUserService.AssignRoleToUserAsync(UserId, role.RoleId);
                
                if (response.Success)
                {
                    // Mostrar notificación de éxito
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Rol Asignado",
                        Detail = $"El rol '{role.Nombre}' ha sido asignado exitosamente al usuario",
                        Duration = 4000
                    });

                    // Recargar la lista de roles
                    await LoadRolesAsync();
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Error al Asignar Rol",
                        Detail = response.Message ?? "No se pudo asignar el rol al usuario",
                        Duration = 5000
                    });
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = $"Error al asignar rol: {ex.Message}",
                    Duration = 5000
                });
            }
        }

        /// <summary>
        /// Remover rol del usuario
        /// </summary>
        private async Task RemoveRole(UserRoleDto role)
        {
            try
            {
                // Mostrar confirmación
                var confirmed = await DialogService.Confirm(
                    $"¿Está seguro que desea remover el rol '{role.Nombre}' del usuario?",
                    "Confirmar Eliminación",
                    new ConfirmOptions()
                    {
                        OkButtonText = "Sí, Remover",
                        CancelButtonText = "Cancelar"
                    }
                );

                if (confirmed != true) return;

                var response = await SystemUserService.RemoveRoleFromUserAsync(UserId, role.RoleId);
                
                if (response.Success)
                {
                    // Mostrar notificación de éxito
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Rol Removido",
                        Detail = $"El rol '{role.Nombre}' ha sido removido exitosamente del usuario",
                        Duration = 4000
                    });

                    // Recargar la lista de roles
                    await LoadRolesAsync();
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Error al Remover Rol",
                        Detail = response.Message ?? "No se pudo remover el rol del usuario",
                        Duration = 5000
                    });
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = $"Error al remover rol: {ex.Message}",
                    Duration = 5000
                });
            }
        }

        #endregion
    }
}