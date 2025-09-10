using System;

namespace Shared.Models.DTOs.RolePermissions
{
    /// <summary>
    /// DTO para mostrar roles que tienen un permiso específico
    /// Usado en el componente de gestión de permisos desde el formulario de permisos
    /// </summary>
    public class PermissionRoleDto
    {
        /// <summary>
        /// ID único del rol
        /// </summary>
        public Guid RoleId { get; set; }

        /// <summary>
        /// Nombre del rol
        /// </summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Descripción del rol (opcional)
        /// </summary>
        public string? Descripcion { get; set; }

        /// <summary>
        /// Indica si el rol tiene el permiso asignado
        /// </summary>
        public bool HasPermission { get; set; }

        /// <summary>
        /// Cantidad de usuarios que tienen este rol
        /// </summary>
        public int UsersCount { get; set; }

        /// <summary>
        /// Estado del rol (activo/inactivo)
        /// </summary>
        public bool Active { get; set; }
    }
}