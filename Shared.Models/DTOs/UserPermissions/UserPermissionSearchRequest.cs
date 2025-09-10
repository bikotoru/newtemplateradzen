using System;
using Shared.Models.QueryModels;

namespace Shared.Models.DTOs.UserPermissions
{
    /// <summary>
    /// Request para buscar y filtrar permisos de un usuario
    /// Extiende QueryRequest para incluir paginación y búsqueda básica
    /// </summary>
    public class UserPermissionSearchRequest : QueryRequest
    {
        /// <summary>
        /// ID del usuario cuyos permisos se están consultando
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Filtro por grupo de permisos (opcional)
        /// </summary>
        public string? GroupKey { get; set; }

        /// <summary>
        /// Si es true, muestra solo permisos asignados directamente al usuario
        /// Si es false, muestra todos los permisos (directos + heredados)
        /// </summary>
        public bool ShowOnlyDirectlyAssigned { get; set; } = false;

        /// <summary>
        /// Si es true, muestra solo permisos que el usuario tiene (directos o heredados)
        /// Si es false, muestra todos los permisos disponibles
        /// </summary>
        public bool ShowOnlyUserPermissions { get; set; } = false;
    }
}