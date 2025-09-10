using System;

namespace Shared.Models.DTOs.UserPermissions
{
    /// <summary>
    /// DTO para mostrar información de permisos de un usuario
    /// Incluye información de si están asignados directamente o heredados de roles
    /// </summary>
    public class UserPermissionDto
    {
        /// <summary>
        /// ID único del permiso
        /// </summary>
        public Guid PermissionId { get; set; }

        /// <summary>
        /// Nombre del permiso (ej: "USER.CREATE", "SYSTEMROLE.EDIT")
        /// </summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Descripción detallada del permiso
        /// </summary>
        public string? Descripcion { get; set; }

        /// <summary>
        /// Clave del grupo para agrupar permisos relacionados
        /// </summary>
        public string? GroupKey { get; set; }

        /// <summary>
        /// Nombre descriptivo del grupo para mostrar en UI
        /// </summary>
        public string? GrupoNombre { get; set; }

        /// <summary>
        /// Clave de la acción específica (CREATE, READ, UPDATE, DELETE, etc.)
        /// </summary>
        public string? ActionKey { get; set; }

        /// <summary>
        /// Indica si el permiso está asignado directamente al usuario
        /// </summary>
        public bool IsDirectlyAssigned { get; set; }

        /// <summary>
        /// Indica si el permiso está heredado de algún rol del usuario
        /// </summary>
        public bool IsInheritedFromRole { get; set; }

        /// <summary>
        /// Lista de nombres de roles que otorgan este permiso
        /// </summary>
        public List<string> InheritedFromRoles { get; set; } = new();

        /// <summary>
        /// Indica si el usuario tiene este permiso (directamente o heredado)
        /// </summary>
        public bool HasPermission => IsDirectlyAssigned || IsInheritedFromRole;
    }
}