using System;
using System.Collections.Generic;

namespace Shared.Models.DTOs.UserPermissions
{
    /// <summary>
    /// Request para actualizar permisos directos de un usuario
    /// Contiene los cambios a realizar (agregar/remover permisos directos)
    /// </summary>
    public class UserPermissionUpdateRequest
    {
        /// <summary>
        /// ID del usuario que se está actualizando
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Lista de IDs de permisos a agregar directamente al usuario
        /// </summary>
        public List<Guid> PermissionsToAdd { get; set; } = new();

        /// <summary>
        /// Lista de IDs de permisos a remover directamente del usuario
        /// </summary>
        public List<Guid> PermissionsToRemove { get; set; } = new();
    }
}