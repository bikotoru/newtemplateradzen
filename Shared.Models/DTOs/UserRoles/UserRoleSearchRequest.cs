using System;
using Shared.Models.QueryModels;

namespace Shared.Models.DTOs.UserRoles
{
    /// <summary>
    /// Request para buscar roles de un usuario
    /// </summary>
    public class UserRoleSearchRequest : QueryRequest
    {
        /// <summary>
        /// ID del usuario
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Mostrar solo roles activos
        /// </summary>
        public bool ShowOnlyActive { get; set; } = true;
    }
}