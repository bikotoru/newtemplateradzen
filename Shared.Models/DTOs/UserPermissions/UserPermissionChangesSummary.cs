using System.Collections.Generic;

namespace Shared.Models.DTOs.UserPermissions
{
    /// <summary>
    /// Resumen de cambios pendientes en permisos de usuario
    /// Utilizado para mostrar en la UI antes de aplicar los cambios
    /// </summary>
    public class UserPermissionChangesSummary
    {
        /// <summary>
        /// Nombre del usuario para mostrar en confirmaciones
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Lista de permisos que se van a agregar directamente al usuario
        /// </summary>
        public List<UserPermissionDto> PermissionsToAdd { get; set; } = new();

        /// <summary>
        /// Lista de permisos que se van a remover directamente del usuario
        /// </summary>
        public List<UserPermissionDto> PermissionsToRemove { get; set; } = new();

        /// <summary>
        /// Indica si hay cambios pendientes
        /// </summary>
        public bool HasChanges => PermissionsToAdd.Count > 0 || PermissionsToRemove.Count > 0;

        /// <summary>
        /// Número total de cambios
        /// </summary>
        public int TotalChanges => PermissionsToAdd.Count + PermissionsToRemove.Count;
    }
}