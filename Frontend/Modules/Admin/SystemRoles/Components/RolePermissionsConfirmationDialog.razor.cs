using Microsoft.AspNetCore.Components;
using Shared.Models.DTOs.RolePermissions;
using Radzen;
using System.Collections.Generic;
using System.Linq;

namespace Frontend.Modules.Admin.SystemRoles.Components
{
    /// <summary>
    /// Modal de confirmación para cambios en permisos de roles
    /// Muestra un resumen detallado antes de aplicar los cambios
    /// </summary>
    public partial class RolePermissionsConfirmationDialog : ComponentBase
    {
        #region Inyecciones de dependencias

        [Inject] private DialogService DialogService { get; set; } = null!;

        #endregion

        #region Parámetros

        /// <summary>
        /// Nombre del rol que se está modificando
        /// </summary>
        [Parameter] public string RoleName { get; set; } = string.Empty;

        /// <summary>
        /// Lista de permisos que se van a agregar
        /// </summary>
        [Parameter] public List<RolePermissionDto> PermissionsToAdd { get; set; } = new();

        /// <summary>
        /// Lista de permisos que se van a remover
        /// </summary>
        [Parameter] public List<RolePermissionDto> PermissionsToRemove { get; set; } = new();

        #endregion

        #region Propiedades calculadas

        /// <summary>
        /// Total de cambios (agregados + removidos)
        /// </summary>
        private int TotalChanges => PermissionsToAdd.Count + PermissionsToRemove.Count;

        /// <summary>
        /// Indica si hay cambios para aplicar
        /// </summary>
        private bool HasChanges => TotalChanges > 0;

        /// <summary>
        /// Indica si hay cambios críticos (permisos del sistema o administración)
        /// </summary>
        private bool HasCriticalChanges => 
            PermissionsToAdd.Any(IsCriticalPermission) || 
            PermissionsToRemove.Any(IsCriticalPermission);

        #endregion

        #region Métodos privados

        /// <summary>
        /// Determina si un permiso es crítico (sistema o administración)
        /// </summary>
        private bool IsCriticalPermission(RolePermissionDto permission)
        {
            if (permission.GroupKey == null) return false;

            var criticalGroups = new[] { "SYSTEMUSER", "SYSTEMROLE", "SYSTEMPERMISSION" };
            return criticalGroups.Any(group => permission.GroupKey.StartsWith(group, StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        #region Eventos de la interfaz

        /// <summary>
        /// Confirmar cambios y cerrar modal
        /// </summary>
        private void Confirm()
        {
            DialogService.Close(true);
        }

        /// <summary>
        /// Cancelar cambios y cerrar modal
        /// </summary>
        private void Cancel()
        {
            DialogService.Close(false);
        }

        #endregion
    }
}