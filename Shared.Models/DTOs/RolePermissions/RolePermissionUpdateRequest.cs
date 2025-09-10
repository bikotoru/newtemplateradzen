using System;
using System.Collections.Generic;

namespace Shared.Models.DTOs.RolePermissions
{
    /// <summary>
    /// Request para actualizar permisos de un rol
    /// Contiene los cambios a realizar (agregar/remover)
    /// </summary>
    public class RolePermissionUpdateRequest
    {
        /// <summary>
        /// ID del rol que se está actualizando
        /// </summary>
        public Guid RoleId { get; set; }

        /// <summary>
        /// Lista de IDs de permisos a agregar al rol
        /// </summary>
        public List<Guid> PermissionsToAdd { get; set; } = new();

        /// <summary>
        /// Lista de IDs de permisos a remover del rol
        /// </summary>
        public List<Guid> PermissionsToRemove { get; set; } = new();
    }
}