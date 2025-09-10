using Microsoft.AspNetCore.Components;
using Shared.Models.DTOs.RolePermissions;
using Shared.Models.QueryModels;
using Shared.Models.Responses;
using Radzen;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Frontend.Modules.Admin.SystemRoles.Components
{
    /// <summary>
    /// Componente para gestionar permisos de roles
    /// Permite agregar/remover permisos con interfaz intuitiva
    /// </summary>
    public partial class SystemRolePermissionsManager : ComponentBase
    {
        #region Inyecciones de dependencias

        [Inject] private SystemRoleService SystemRoleService { get; set; } = null!;
        [Inject] private NotificationService NotificationService { get; set; } = null!;
        [Inject] private DialogService DialogService { get; set; } = null!;

        #endregion

        #region Parámetros

        /// <summary>
        /// ID del rol cuyos permisos se van a gestionar
        /// </summary>
        [Parameter, EditorRequired] public Guid RoleId { get; set; }

        /// <summary>
        /// Nombre del rol para mostrar en confirmaciones
        /// </summary>
        [Parameter] public string? RoleName { get; set; }

        /// <summary>
        /// Callback que se ejecuta cuando se guardan cambios exitosamente
        /// </summary>
        [Parameter] public EventCallback OnPermissionsUpdated { get; set; }

        #endregion

        #region Campos privados

        // Estado de carga y datos
        private bool isLoading = false;
        private string? errorMessage = null;
        private PagedResult<RolePermissionDto>? pagedResult = null;
        private List<IGrouping<string, RolePermissionDto>>? groupedPermissions = null;

        // Filtros y búsqueda
        private string searchTerm = string.Empty;
        private string? selectedGroup = null;
        private bool showOnlyAssigned = false;
        private List<GroupOption> availableGroups = new();

        // Paginación
        private int currentPage = 1;
        private int pageSize = 20;
        private int totalPages => pagedResult != null ? (int)Math.Ceiling((double)pagedResult.TotalCount / pageSize) : 0;

        // Cambios pendientes - para mostrar en UI
        private RolePermissionChangesSummary pendingChanges = new();
        private bool hasChanges => pendingChanges.PermissionsToAdd.Any() || pendingChanges.PermissionsToRemove.Any();

        // Timer para debounce de búsqueda
        private System.Timers.Timer? searchDebounceTimer;
        
        // Control de grupos expandidos/colapsados
        private HashSet<string> expandedGroups = new();
        
        // Estado original de permisos para comparar cambios
        private Dictionary<Guid, bool> originalPermissionStates = new();

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
            // Resetear cambios pendientes si cambia el rol
            if (string.IsNullOrEmpty(pendingChanges.RoleName) || RoleId != Guid.Empty)
            {
                pendingChanges = new RolePermissionChangesSummary 
                { 
                    RoleName = RoleName ?? "Rol" 
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
                var response = await SystemRoleService.GetAvailablePermissionGroupsAsync();
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
            isLoading = true;
            errorMessage = null;

            try
            {
                var request = new RolePermissionSearchRequest
                {
                    RoleId = RoleId,
                    SearchTerm = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm,
                    GroupKey = selectedGroup,
                    ShowOnlyAssigned = showOnlyAssigned,
                    Page = currentPage,
                    PageSize = pageSize
                };

                var response = await SystemRoleService.GetRolePermissionsPagedAsync(RoleId, request);
                
                if (response.Success && response.Data != null)
                {
                    pagedResult = response.Data;
                    
                    // Guardar estado original de permisos antes de aplicar cambios pendientes
                    SaveOriginalPermissionStates();
                    
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
        /// Guardar el estado original de permisos desde el servidor
        /// </summary>
        private void SaveOriginalPermissionStates()
        {
            if (pagedResult?.Data == null) return;

            foreach (var permission in pagedResult.Data)
            {
                // Solo guardar si no existe ya (para mantener el estado original en paginación)
                if (!originalPermissionStates.ContainsKey(permission.PermissionId))
                {
                    originalPermissionStates[permission.PermissionId] = permission.IsAssigned;
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

                // Aplicar el estado pendiente
                if (toAdd) permission.IsAssigned = true;
                if (toRemove) permission.IsAssigned = false;
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
        /// Manejar cambio en el switch de "solo asignados"
        /// </summary>
        private async Task OnShowOnlyAssignedChanged(bool value)
        {
            showOnlyAssigned = value;
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
        /// Manejar toggle de permiso individual
        /// </summary>
        private void TogglePermission(RolePermissionDto permission, bool isChecked)
        {
            // Obtener el estado original del servidor (sin cambios pendientes)
            var originallyAssigned = GetOriginalAssignedState(permission);

            if (isChecked && !originallyAssigned)
            {
                // Agregar permiso
                AddPendingChange(permission, true);
            }
            else if (!isChecked && originallyAssigned)
            {
                // Remover permiso
                AddPendingChange(permission, false);
            }
            else
            {
                // Volver al estado original - remover cambio pendiente
                RemovePendingChange(permission);
            }

            // Actualizar UI
            permission.IsAssigned = isChecked;
            StateHasChanged();
        }

        /// <summary>
        /// Obtener el estado original de asignación (sin cambios pendientes)
        /// </summary>
        private bool GetOriginalAssignedState(RolePermissionDto permission)
        {
            // Usar el estado original guardado desde el servidor
            if (originalPermissionStates.TryGetValue(permission.PermissionId, out var originalState))
            {
                return originalState;
            }

            // Fallback: si no tenemos el estado original, usar el estado actual
            // (esto debería ser raro, pero es una protección)
            return permission.IsAssigned;
        }

        /// <summary>
        /// Agregar cambio pendiente
        /// </summary>
        private void AddPendingChange(RolePermissionDto permission, bool isAdd)
        {
            // Primero remover cualquier cambio previo de este permiso
            RemovePendingChange(permission);

            // Agregar nuevo cambio
            if (isAdd)
            {
                pendingChanges.PermissionsToAdd.Add(new RolePermissionDto
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
                pendingChanges.PermissionsToRemove.Add(new RolePermissionDto
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
        private void RemovePendingChange(RolePermissionDto permission)
        {
            pendingChanges.PermissionsToAdd.RemoveAll(p => p.PermissionId == permission.PermissionId);
            pendingChanges.PermissionsToRemove.RemoveAll(p => p.PermissionId == permission.PermissionId);
        }

        /// <summary>
        /// Verificar si un permiso tiene cambios pendientes
        /// </summary>
        private bool HasPendingChange(RolePermissionDto permission)
        {
            return pendingChanges.PermissionsToAdd.Any(p => p.PermissionId == permission.PermissionId) ||
                   pendingChanges.PermissionsToRemove.Any(p => p.PermissionId == permission.PermissionId);
        }

        #endregion

        #region Guardar cambios

        /// <summary>
        /// Guardar todos los cambios pendientes
        /// </summary>
        private async Task SavePermissions()
        {
            if (!hasChanges) return;

            try
            {
                // Mostrar modal de confirmación con resumen de cambios
                var confirmed = await ShowConfirmationDialog();
                if (!confirmed) return;

                isLoading = true;
                StateHasChanged();

                // Crear request con IDs únicamente
                var updateRequest = new RolePermissionUpdateRequest
                {
                    RoleId = RoleId,
                    PermissionsToAdd = pendingChanges.PermissionsToAdd.Select(p => p.PermissionId).ToList(),
                    PermissionsToRemove = pendingChanges.PermissionsToRemove.Select(p => p.PermissionId).ToList()
                };

                var response = await SystemRoleService.UpdateRolePermissionsAsync(RoleId, updateRequest);

                if (response.Success)
                {
                    // Limpiar cambios pendientes y estado original
                    pendingChanges = new RolePermissionChangesSummary { RoleName = RoleName ?? "Rol" };
                    originalPermissionStates.Clear();

                    // Mostrar notificación de éxito
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Permisos Actualizados",
                        Detail = response.Message ?? "Los permisos del rol han sido actualizados exitosamente",
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
            var result = await DialogService.OpenAsync<RolePermissionsConfirmationDialog>(
                "Confirmar Cambios",
                new Dictionary<string, object>
                {
                    { "RoleName", RoleName ?? "Rol" },
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
        private string GetPermissionItemStyle(RolePermissionDto permission)
        {
            var baseStyle = "cursor: pointer;";
            
            if (HasPendingChange(permission))
            {
                baseStyle += " border-width: 2px; border-style: dashed;";
                
                if (pendingChanges.PermissionsToAdd.Any(p => p.PermissionId == permission.PermissionId))
                {
                    baseStyle += " border-color: var(--rz-info);";
                }
                else if (pendingChanges.PermissionsToRemove.Any(p => p.PermissionId == permission.PermissionId))
                {
                    baseStyle += " border-color: var(--rz-warning);";
                }
            }
            
            return baseStyle;
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