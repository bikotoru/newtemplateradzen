using System;
using System.Collections.Generic;

namespace Shared.Models.DTOs.UserRoles
{
    /// <summary>
    /// Request para actualizar roles de un usuario
    /// </summary>
    public class UserRoleUpdateRequest
    {
        /// <summary>
        /// ID del usuario
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Lista de IDs de roles a agregar
        /// </summary>
        public List<Guid> RolesToAdd { get; set; } = new();

        /// <summary>
        /// Lista de IDs de roles a remover
        /// </summary>
        public List<Guid> RolesToRemove { get; set; } = new();
    }
}