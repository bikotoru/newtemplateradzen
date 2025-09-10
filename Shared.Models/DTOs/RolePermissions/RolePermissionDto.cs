using System;

namespace Shared.Models.DTOs.RolePermissions
{
    /// <summary>
    /// DTO para mostrar permisos con su estado de asignación a un rol
    /// Usado en el componente de gestión de permisos de roles
    /// </summary>
    public class RolePermissionDto
    {
        /// <summary>
        /// ID único del permiso
        /// </summary>
        public Guid PermissionId { get; set; }

        /// <summary>
        /// Nombre del permiso (ej: "SYSTEMUSER.CREATE")
        /// </summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Descripción detallada del permiso
        /// </summary>
        public string? Descripcion { get; set; }

        /// <summary>
        /// Clave de agrupación (ej: "SYSTEMUSER")
        /// </summary>
        public string? GroupKey { get; set; }

        /// <summary>
        /// Nombre del grupo para mostrar (ej: "SystemUser")
        /// </summary>
        public string? GrupoNombre { get; set; }

        /// <summary>
        /// Indica si el permiso está asignado al rol actual
        /// </summary>
        public bool IsAssigned { get; set; }

        /// <summary>
        /// ActionKey del permiso para validaciones
        /// </summary>
        public string? ActionKey { get; set; }
    }
}