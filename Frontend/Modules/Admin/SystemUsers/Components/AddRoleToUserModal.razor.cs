using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Shared.Models.DTOs.UserRoles;
using Radzen;
using Radzen.Blazor;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Frontend.Modules.Admin.SystemUsers.Components
{
    public partial class AddRoleToUserModal : ComponentBase, IDisposable
    {
        [Inject] private SystemUserService SystemUserService { get; set; } = default!;
        [Inject] private DialogService DialogService { get; set; } = default!;
        [Inject] private NotificationService NotificationService { get; set; } = default!;

        [Parameter] public Guid UserId { get; set; }
        [Parameter] public string UserName { get; set; } = string.Empty;

        #region Estado del componente

        // Datos de roles
        private List<AvailableRoleDto> availableRoles = new();
        private List<AvailableRoleDto> filteredRoles => string.IsNullOrWhiteSpace(searchTerm) 
            ? availableRoles 
            : availableRoles.Where(r => r.Nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                       (!string.IsNullOrEmpty(r.Descripcion) && r.Descripcion.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                            .ToList();

        // Estado de la UI
        private bool isLoading = false;
        private string? errorMessage = null;

        // Búsqueda
        private string searchTerm = string.Empty;
        private System.Timers.Timer? searchDebounceTimer;

        #endregion

        #region Eventos del ciclo de vida

        protected override async Task OnInitializedAsync()
        {
            await LoadAvailableRolesAsync();
        }

        public void Dispose()
        {
            searchDebounceTimer?.Dispose();
        }

        #endregion

        #region Métodos de carga de datos

        /// <summary>
        /// Cargar roles disponibles para el usuario
        /// </summary>
        private async Task LoadAvailableRolesAsync()
        {
            if (UserId == Guid.Empty) return;

            isLoading = true;
            errorMessage = null;

            try
            {
                var response = await SystemUserService.GetAvailableRolesAsync(UserId, searchTerm);
                
                if (response.Success && response.Data != null)
                {
                    availableRoles = response.Data;
                }
                else
                {
                    errorMessage = response.Message ?? "Error al cargar roles disponibles";
                    availableRoles = new();
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error al cargar roles: {ex.Message}";
                availableRoles = new();
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        #endregion

        #region Eventos de búsqueda

        /// <summary>
        /// Manejar cambio en el término de búsqueda
        /// </summary>
        private void OnSearchChanged(ChangeEventArgs e)
        {
            searchTerm = e.Value?.ToString() ?? string.Empty;
            
            // Debounce search - pero como es filtro local, solo actualizamos la vista
            StateHasChanged();
        }

        #endregion

        #region Acciones del modal

        /// <summary>
        /// Seleccionar rol para asignar
        /// </summary>
        private async Task SelectRole(AvailableRoleDto role)
        {
            if (role.YaAsignado) return;

            try
            {
                // Mostrar confirmación con preview de permisos
                var confirmed = await ShowConfirmationDialog(role);
                
                if (confirmed)
                {
                    DialogService.Close(role);
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = $"Error al seleccionar rol: {ex.Message}",
                    Duration = 5000
                });
            }
        }

        /// <summary>
        /// Mostrar diálogo de confirmación con preview de permisos
        /// </summary>
        private async Task<bool> ShowConfirmationDialog(AvailableRoleDto role)
        {
            var result = await DialogService.OpenAsync<Modals.ConfirmRoleAssignmentModal>(
                "Confirmar Asignación de Rol",
                new Dictionary<string, object>
                {
                    { "Role", role },
                    { "UserName", UserName }
                },
                new DialogOptions()
                {
                    Width = "600px",
                    Height = "auto",
                    Resizable = false,
                    Draggable = true,
                    CloseDialogOnOverlayClick = false,
                    ShowClose = true
                }
            );

            return result == true;
        }

        /// <summary>
        /// Mostrar detalle completo de permisos del rol
        /// </summary>
        private async Task ShowPermissionsDetail(AvailableRoleDto role)
        {
            await DialogService.OpenAsync<Modals.RolePermissionsDetailModal>(
                "Detalle de Permisos",
                new Dictionary<string, object>
                {
                    { "Role", role }
                },
                new DialogOptions()
                {
                    Width = "500px",
                    Height = "auto",
                    Resizable = false,
                    Draggable = true,
                    CloseDialogOnOverlayClick = true,
                    ShowClose = true
                }
            );
        }

        /// <summary>
        /// Cancelar y cerrar modal
        /// </summary>
        private void Cancel()
        {
            DialogService.Close();
        }

        #endregion
    }
}