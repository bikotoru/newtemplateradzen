using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Shared.Models.DTOs.UserPermissions;
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
    /// Componente para gestionar permisos directos de usuarios
    /// Permite agregar/remover permisos directos con interfaz intuitiva
    /// Distingue entre permisos directos y heredados de roles
    /// </summary>
    public partial class SystemUserPermissionsManager : ComponentBase
    {
        #region Inyecciones de dependencias

        [Inject] private SystemUserService SystemUserService { get; set; } = null!;
        [Inject] private NotificationService NotificationService { get; set; } = null!;
        [Inject] private DialogService DialogService { get; set; } = null!;

        #endregion

        #region Parámetros

        /// <summary>
        /// ID del usuario cuyos permisos se van a gestionar
        /// </summary>
        [Parameter, EditorRequired] public Guid UserId { get; set; }

        /// <summary>
        /// Nombre del usuario para mostrar en confirmaciones
        /// </summary>
        [Parameter] public string? UserName { get; set; }

        /// <summary>
        /// Callback que se ejecuta cuando se guardan cambios exitosamente
        /// </summary>
        [Parameter] public EventCallback OnPermissionsUpdated { get; set; }

        #endregion

        #region Campos privados

        // Estado de carga y datos
        private bool isLoading = false;
        private string? errorMessage = null;
        private PagedResult<UserPermissionDto>? pagedResult = null;
        private List<IGrouping<string, UserPermissionDto>>? groupedPermissions = null;

        // Filtros y búsqueda
        private string searchTerm = string.Empty;
        private string? selectedGroup = null;
        private bool showOnlyDirectlyAssigned = false;
        private List<GroupOption> availableGroups = new();

        // Paginación
        private int currentPage = 1;
        private int pageSize = 20;
        private int totalPages => pagedResult != null ? (int)Math.Ceiling((double)pagedResult.TotalCount / pageSize) : 0;

        // Cambios pendientes - para mostrar en UI
        private UserPermissionChangesSummary pendingChanges = new() 
        { 
            UserName = "Usuario",
            PermissionsToAdd = new List<UserPermissionDto>(),
            PermissionsToRemove = new List<UserPermissionDto>()
        };
        private bool hasChanges => pendingChanges.PermissionsToAdd.Any() || pendingChanges.PermissionsToRemove.Any();

        // Timer para debounce de búsqueda
        private System.Timers.Timer? searchDebounceTimer;
        
        // Control de grupos expandidos/colapsados
        private HashSet<string> expandedGroups = new();
        
        // Estado original de permisos para comparar cambios
        private Dictionary<Guid, bool> originalDirectPermissionStates = new();

        #endregion

        #region Clases auxiliares

        public class GroupOption
        {
            public string? Value { get; set; }
            public string Text { get; set; } = string.Empty;
        }

        #endregion

        #region Eventos del ciclo de vida

        protected override async Task OnInitializedAsync()
        {
            await LoadAvailableGroupsAsync();
            await LoadPermissionsAsync();
        }

        protected override void OnParametersSet()
        {
            // Resetear cambios pendientes si cambia el usuario
            if (string.IsNullOrEmpty(pendingChanges.UserName) || UserId != Guid.Empty)
            {
                pendingChanges = new UserPermissionChangesSummary 
                { 
                    UserName = UserName ?? "Usuario",
                    PermissionsToAdd = new List<UserPermissionDto>(),
                    PermissionsToRemove = new List<UserPermissionDto>()
                };
            }
        }

        public void Dispose()
        {
            searchDebounceTimer?.Dispose();
        }

        #endregion

        #region Métodos de carga de datos

        /// <summary>
        /// Cargar grupos de permisos disponibles para el filtro
        /// </summary>
        private async Task LoadAvailableGroupsAsync()
        {
            try
            {
                var response = await SystemUserService.GetAvailablePermissionGroupsAsync();
                if (response.Success && response.Data != null)
                {
                    availableGroups = new List<GroupOption>
                    {
                        new() { Value = null, Text = "Todos los grupos" }
                    };
                    availableGroups.AddRange(response.Data.Select(g => new GroupOption { Value = g, Text = g }));
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error al cargar grupos: {ex.Message}";
            }
        }

        /// <summary>
        /// Cargar permisos con los filtros actuales
        /// </summary>
        private async Task LoadPermissionsAsync()
        {
            // No cargar si no hay UserId válido
            if (UserId == Guid.Empty)
            {
                pagedResult = null;
                groupedPermissions = null;
                return;
            }

            isLoading = true;
            errorMessage = null;

            try
            {
                var request = new UserPermissionSearchRequest
                {
                    UserId = UserId,
                    Filter = string.IsNullOrWhiteSpace(searchTerm) ? null : $"Nombre.Contains(\"{searchTerm}\")",
                    GroupKey = selectedGroup,
                    ShowOnlyDirectlyAssigned = showOnlyDirectlyAssigned,
                    Skip = (currentPage - 1) * pageSize,
                    Take = pageSize
                };

                var response = await SystemUserService.GetUserPermissionsPagedAsync(UserId, request);
                
                if (response.Success && response.Data != null)
                {
                    pagedResult = response.Data;
                    
                    // Guardar estado original de permisos directos antes de aplicar cambios pendientes
                    SaveOriginalDirectPermissionStates();
                    
                    // Aplicar cambios pendientes a los datos cargados
                    ApplyPendingChangesToData();
                    
                    // Agrupar por GrupoNombre
                    GroupPermissions();
                }
                else
                {
                    errorMessage = response.Message ?? "Error al cargar permisos";
                    pagedResult = null;
                    groupedPermissions = null;
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error al cargar permisos: {ex.Message}";
                pagedResult = null;
                groupedPermissions = null;
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        /// <summary>
        /// Guardar el estado original de permisos directos desde el servidor
        /// </summary>
        private void SaveOriginalDirectPermissionStates()
        {
            if (pagedResult?.Data == null) return;

            foreach (var permission in pagedResult.Data)
            {
                // Solo guardar si no existe ya (para mantener el estado original en paginación)
                if (!originalDirectPermissionStates.ContainsKey(permission.PermissionId))
                {
                    originalDirectPermissionStates[permission.PermissionId] = permission.IsDirectlyAssigned;
                }
            }
        }

        /// <summary>
        /// Aplicar cambios pendientes a los datos mostrados
        /// </summary>
        private void ApplyPendingChangesToData()
        {
            if (pagedResult?.Data == null) return;

            foreach (var permission in pagedResult.Data)
            {
                // Verificar si está en la lista de agregar
                var toAdd = pendingChanges.PermissionsToAdd.Any(p => p.PermissionId == permission.PermissionId);
                // Verificar si está en la lista de remover
                var toRemove = pendingChanges.PermissionsToRemove.Any(p => p.PermissionId == permission.PermissionId);

                // Aplicar el estado pendiente solo a permisos directos
                if (toAdd) permission.IsDirectlyAssigned = true;
                if (toRemove) permission.IsDirectlyAssigned = false;
            }
        }

        /// <summary>
        /// Agrupar permisos por GrupoNombre
        /// </summary>
        private void GroupPermissions()
        {
            if (pagedResult?.Data == null)
            {
                groupedPermissions = null;
                return;
            }

            groupedPermissions = pagedResult.Data
                .GroupBy(p => p.GrupoNombre ?? "Sin Grupo")
                .OrderBy(g => g.Key)
                .ToList();

            // Expandir todos los grupos por defecto al cargar por primera vez
            if (!expandedGroups.Any() && groupedPermissions.Any())
            {
                foreach (var group in groupedPermissions)
                {
                    expandedGroups.Add(group.Key);
                }
            }
        }

        #endregion

        #region Eventos de la interfaz

        /// <summary>
        /// Manejar cambio en el término de búsqueda con debounce
        /// </summary>
        private void OnSearchChanged(ChangeEventArgs args)
        {
            searchTerm = args.Value?.ToString() ?? string.Empty;

            // Resetear el timer de debounce
            searchDebounceTimer?.Stop();
            searchDebounceTimer?.Dispose();

            searchDebounceTimer = new System.Timers.Timer(500); // 500ms de debounce
            searchDebounceTimer.Elapsed += async (sender, e) =>
            {
                searchDebounceTimer.Stop();
                currentPage = 1; // Resetear a primera página
                await InvokeAsync(LoadPermissionsAsync);
            };
            searchDebounceTimer.Start();
        }

        /// <summary>
        /// Manejar cambio en el filtro de grupo
        /// </summary>
        private async Task OnGroupChanged()
        {
            currentPage = 1;
            await LoadPermissionsAsync();
        }

        /// <summary>
        /// Manejar cambio en el switch de "solo directos"
        /// </summary>
        private async Task OnShowOnlyDirectlyAssignedChanged(bool value)
        {
            showOnlyDirectlyAssigned = value;
            currentPage = 1;
            await LoadPermissionsAsync();
        }

        /// <summary>
        /// Manejar cambio de página
        /// </summary>
        private async Task OnPageChanged(int page)
        {
            currentPage = page;
            await LoadPermissionsAsync();
        }

        /// <summary>
        /// Manejar toggle de permiso directo individual
        /// </summary>
        private void TogglePermission(UserPermissionDto permission, bool isChecked)
        {
            // Solo modificar permisos directos, no los heredados
            if (!CanModifyPermission(permission)) return;

            // Obtener el estado original del servidor (sin cambios pendientes)
            var originallyDirectlyAssigned = GetOriginalDirectlyAssignedState(permission);

            if (isChecked && !originallyDirectlyAssigned)
            {
                // Agregar permiso directo
                AddPendingChange(permission, true);
            }
            else if (!isChecked && originallyDirectlyAssigned)
            {
                // Remover permiso directo
                AddPendingChange(permission, false);
            }
            else
            {
                // Volver al estado original - remover cambio pendiente
                RemovePendingChange(permission);
            }

            // Actualizar UI
            permission.IsDirectlyAssigned = isChecked;
            StateHasChanged();
        }

        /// <summary>
        /// Determinar si se puede modificar un permiso
        /// </summary>
        private bool CanModifyPermission(UserPermissionDto permission)
        {
            // Siempre se pueden modificar permisos directos
            // Los heredados no se pueden modificar desde aquí
            return true;
        }

        /// <summary>
        /// Obtener el estado original de asignación directa (sin cambios pendientes)
        /// </summary>
        private bool GetOriginalDirectlyAssignedState(UserPermissionDto permission)
        {
            // Usar el estado original guardado desde el servidor
            if (originalDirectPermissionStates.TryGetValue(permission.PermissionId, out var originalState))
            {
                return originalState;
            }

            // Fallback: si no tenemos el estado original, usar el estado actual
            // (esto debería ser raro, pero es una protección)
            return permission.IsDirectlyAssigned;
        }

        /// <summary>
        /// Agregar cambio pendiente
        /// </summary>
        private void AddPendingChange(UserPermissionDto permission, bool isAdd)
        {
            // Primero remover cualquier cambio previo de este permiso
            RemovePendingChange(permission);

            // Agregar nuevo cambio
            if (isAdd)
            {
                pendingChanges.PermissionsToAdd.Add(new UserPermissionDto
                {
                    PermissionId = permission.PermissionId,
                    Nombre = permission.Nombre,
                    Descripcion = permission.Descripcion,
                    GroupKey = permission.GroupKey,
                    GrupoNombre = permission.GrupoNombre
                });
            }
            else
            {
                pendingChanges.PermissionsToRemove.Add(new UserPermissionDto
                {
                    PermissionId = permission.PermissionId,
                    Nombre = permission.Nombre,
                    Descripcion = permission.Descripcion,
                    GroupKey = permission.GroupKey,
                    GrupoNombre = permission.GrupoNombre
                });
            }
        }

        /// <summary>
        /// Remover cambio pendiente
        /// </summary>
        private void RemovePendingChange(UserPermissionDto permission)
        {
            pendingChanges.PermissionsToAdd.RemoveAll(p => p.PermissionId == permission.PermissionId);
            pendingChanges.PermissionsToRemove.RemoveAll(p => p.PermissionId == permission.PermissionId);
        }

        /// <summary>
        /// Verificar si un permiso tiene cambios pendientes
        /// </summary>
        private bool HasPendingChange(UserPermissionDto permission)
        {
            return pendingChanges.PermissionsToAdd.Any(p => p.PermissionId == permission.PermissionId) ||
                   pendingChanges.PermissionsToRemove.Any(p => p.PermissionId == permission.PermissionId);
        }

        #endregion

        #region Guardar cambios

        /// <summary>
        /// Guardar todos los cambios pendientes de permisos directos
        /// </summary>
        private async Task SavePermissions()
        {
            if (!hasChanges || UserId == Guid.Empty) return;

            try
            {
                // Mostrar modal de confirmación con resumen de cambios
                var confirmed = await ShowConfirmationDialog();
                if (!confirmed) return;

                isLoading = true;
                StateHasChanged();

                // Crear request con IDs únicamente
                var updateRequest = new UserPermissionUpdateRequest
                {
                    UserId = UserId,
                    PermissionsToAdd = pendingChanges.PermissionsToAdd.Select(p => p.PermissionId).ToList(),
                    PermissionsToRemove = pendingChanges.PermissionsToRemove.Select(p => p.PermissionId).ToList()
                };

                var response = await SystemUserService.UpdateUserPermissionsAsync(UserId, updateRequest);

                if (response.Success)
                {
                    // Limpiar cambios pendientes y estado original
                    pendingChanges = new UserPermissionChangesSummary 
                    { 
                        UserName = UserName ?? "Usuario",
                        PermissionsToAdd = new List<UserPermissionDto>(),
                        PermissionsToRemove = new List<UserPermissionDto>()
                    };
                    originalDirectPermissionStates.Clear();

                    // Mostrar notificación de éxito
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Permisos Directos Actualizados",
                        Detail = response.Message ?? "Los permisos directos del usuario han sido actualizados exitosamente",
                        Duration = 4000
                    });

                    // Recargar datos
                    await LoadPermissionsAsync();

                    // Notificar al componente padre
                    await OnPermissionsUpdated.InvokeAsync();
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Error al Guardar",
                        Detail = response.Message ?? "No se pudieron guardar los cambios",
                        Duration = 6000
                    });
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error Inesperado",
                    Detail = $"Error al guardar permisos: {ex.Message}",
                    Duration = 6000
                });
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        /// <summary>
        /// Mostrar modal de confirmación con resumen de cambios
        /// </summary>
        private async Task<bool> ShowConfirmationDialog()
        {
            // Crear el componente de confirmación y mostrarlo
            var result = await DialogService.OpenAsync<UserPermissionsConfirmationDialog>(
                "Confirmar Cambios en Permisos Directos",
                new Dictionary<string, object>
                {
                    { "UserName", UserName ?? "Usuario" },
                    { "PermissionsToAdd", pendingChanges.PermissionsToAdd },
                    { "PermissionsToRemove", pendingChanges.PermissionsToRemove }
                },
                new DialogOptions
                {
                    Width = "80%",
                    Height = "100%",
                    Style = "max-height: 100%",
                    Resizable = true,
                    Draggable = true,
                    CloseDialogOnOverlayClick = false,
                    ShowClose = true
                });

            return result is bool confirmed && confirmed;
        }

        #endregion

        #region Métodos de estilo visual

        /// <summary>
        /// Obtiene el estilo CSS para un item de permiso
        /// </summary>
        private string GetPermissionItemStyle(UserPermissionDto permission)
        {
            var baseStyle = "cursor: pointer;";
            
            if (HasPendingChange(permission))
            {
                baseStyle += " border-width: 2px; border-style: dashed;";
                
                if (pendingChanges.PermissionsToAdd.Any(p => p.PermissionId == permission.PermissionId))
                {
                    baseStyle += " border-color: var(--rz-warning);";
                }
                else if (pendingChanges.PermissionsToRemove.Any(p => p.PermissionId == permission.PermissionId))
                {
                    baseStyle += " border-color: var(--rz-danger);";
                }
            }
            
            return baseStyle;
        }

        /// <summary>
        /// Obtiene la clase CSS basada en el estado del permiso
        /// </summary>
        private string GetPermissionStatusClass(UserPermissionDto permission)
        {
            if (permission.IsDirectlyAssigned && permission.IsInheritedFromRole)
                return "permission-both";
            else if (permission.IsDirectlyAssigned)
                return "permission-direct-only";
            else if (permission.IsInheritedFromRole)
                return "permission-inherited-only";
            else
                return "permission-none";
        }

        #endregion

        #region Modal de roles heredados

        /// <summary>
        /// Mostrar modal con los roles que otorgan un permiso heredado
        /// </summary>
        private async Task ShowInheritedRolesModal(UserPermissionDto permission)
        {
            if (!permission.IsInheritedFromRole || !permission.InheritedFromRoles.Any()) return;

            await DialogService.OpenAsync<InheritedRolesModal>(
                "Roles que Otorgan Este Permiso",
                new Dictionary<string, object>
                {
                    { "PermissionName", permission.Nombre },
                    { "InheritedFromRoles", permission.InheritedFromRoles }
                },
                new DialogOptions()
                {
                    Width = "600px",
                    Height = "auto",
                    Resizable = false,
                    Draggable = true,
                    CloseDialogOnOverlayClick = true,
                    ShowClose = true
                }
            );
        }

        #endregion

        #region Manejo de paneles colapsables

        /// <summary>
        /// Manejar expansión de grupo
        /// </summary>
        private void OnGroupExpanded(string groupName)
        {
            expandedGroups.Add(groupName);
        }

        /// <summary>
        /// Manejar colapso de grupo
        /// </summary>
        private void OnGroupCollapsed(string groupName)
        {
            expandedGroups.Remove(groupName);
        }

        #endregion
    }
}