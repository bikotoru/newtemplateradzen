using System;

namespace Shared.Models.DTOs.UserPermissions
{
    /// <summary>
    /// DTO para mostrar usuarios que tienen un permiso específico
    /// Usado en el componente de gestión de permisos desde el formulario de permisos
    /// </summary>
    public class PermissionUserDto
    {
        /// <summary>
        /// ID único del usuario
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Nombre del usuario
        /// </summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Email del usuario (opcional)
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Indica si el usuario tiene el permiso asignado directamente
        /// </summary>
        public bool IsDirectlyAssigned { get; set; }

        /// <summary>
        /// Indica si el usuario tiene el permiso heredado de algún rol
        /// </summary>
        public bool IsInheritedFromRole { get; set; }

        /// <summary>
        /// Lista de nombres de roles que otorgan este permiso al usuario
        /// </summary>
        public List<string> InheritedFromRoles { get; set; } = new();

        /// <summary>
        /// Indica si el usuario tiene este permiso (directamente o heredado)
        /// </summary>
        public bool HasPermission => IsDirectlyAssigned || IsInheritedFromRole;
    }
}