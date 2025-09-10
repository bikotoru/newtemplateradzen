using System;

namespace Shared.Models.DTOs.RolePermissions
{
    /// <summary>
    /// Request para búsqueda paginada de permisos para un rol
    /// </summary>
    public class RolePermissionSearchRequest
    {
        /// <summary>
        /// ID del rol para obtener sus permisos
        /// </summary>
        public Guid RoleId { get; set; }

        /// <summary>
        /// Término de búsqueda (busca en nombre y descripción)
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Filtrar por grupo específico
        /// </summary>
        public string? GroupKey { get; set; }

        /// <summary>
        /// Mostrar solo permisos asignados al rol (true) o todos los disponibles (false)
        /// </summary>
        public bool ShowOnlyAssigned { get; set; } = false;

        /// <summary>
        /// Página actual (empieza en 1)
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Tamaño de página
        /// </summary>
        public int PageSize { get; set; } = 20;
    }
}