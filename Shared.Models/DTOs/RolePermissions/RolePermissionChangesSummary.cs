using System;
using System.Collections.Generic;

namespace Shared.Models.DTOs.RolePermissions
{
    /// <summary>
    /// DTO para mostrar el resumen de cambios antes de aplicar
    /// Usado en el modal de confirmación
    /// </summary>
    public class RolePermissionChangesSummary
    {
        /// <summary>
        /// Nombre del rol que se está modificando
        /// </summary>
        public string RoleName { get; set; } = string.Empty;

        /// <summary>
        /// Lista de permisos que se van a agregar
        /// </summary>
        public List<RolePermissionDto> PermissionsToAdd { get; set; } = new();

        /// <summary>
        /// Lista de permisos que se van a remover
        /// </summary>
        public List<RolePermissionDto> PermissionsToRemove { get; set; } = new();

        /// <summary>
        /// Total de cambios (agregados + removidos)
        /// </summary>
        public int TotalChanges => PermissionsToAdd.Count + PermissionsToRemove.Count;

        /// <summary>
        /// Indica si hay cambios para aplicar
        /// </summary>
        public bool HasChanges => TotalChanges > 0;
    }
}