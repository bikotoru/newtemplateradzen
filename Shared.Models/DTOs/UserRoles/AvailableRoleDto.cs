using System;
using System.Collections.Generic;

namespace Shared.Models.DTOs.UserRoles
{
    /// <summary>
    /// DTO que representa un rol disponible para asignar a un usuario
    /// </summary>
    public class AvailableRoleDto
    {
        /// <summary>
        /// ID del rol
        /// </summary>
        public Guid RoleId { get; set; }

        /// <summary>
        /// Nombre del rol
        /// </summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Descripción del rol
        /// </summary>
        public string? Descripcion { get; set; }

        /// <summary>
        /// Fecha de creación del rol
        /// </summary>
        public DateTime FechaCreacion { get; set; }

        /// <summary>
        /// Indica si el rol está activo
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Indica si el usuario ya tiene este rol asignado
        /// </summary>
        public bool YaAsignado { get; set; }

        /// <summary>
        /// Cantidad de permisos que otorga este rol
        /// </summary>
        public int CantidadPermisos { get; set; }

        /// <summary>
        /// Lista de nombres de permisos que otorga este rol
        /// </summary>
        public List<string> PermisosNombres { get; set; } = new();

        /// <summary>
        /// Lista de acciones de permisos que otorga este rol
        /// </summary>
        public List<string> PermisosAcciones { get; set; } = new();
    }
}