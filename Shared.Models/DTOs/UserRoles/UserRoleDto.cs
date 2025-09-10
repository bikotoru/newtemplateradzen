using System;
using System.Collections.Generic;

namespace Shared.Models.DTOs.UserRoles
{
    /// <summary>
    /// DTO que representa un rol asignado a un usuario
    /// </summary>
    public class UserRoleDto
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
        /// Usuario que creó el rol
        /// </summary>
        public string? CreadoPor { get; set; }

        /// <summary>
        /// Indica si el rol está activo
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Fecha cuando fue asignado al usuario
        /// </summary>
        public DateTime FechaAsignacion { get; set; }

        /// <summary>
        /// Usuario que asignó el rol
        /// </summary>
        public string? AsignadoPor { get; set; }

        /// <summary>
        /// Cantidad de permisos que otorga este rol
        /// </summary>
        public int CantidadPermisos { get; set; }

        /// <summary>
        /// Lista de permisos que otorga este rol
        /// </summary>
        public List<string> Permisos { get; set; } = new();
    }
}