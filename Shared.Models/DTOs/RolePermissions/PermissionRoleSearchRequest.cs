using System;
using System.ComponentModel.DataAnnotations;

namespace Shared.Models.DTOs.RolePermissions
{
    /// <summary>
    /// Request para buscar roles que tienen un permiso específico
    /// </summary>
    public class PermissionRoleSearchRequest
    {
        /// <summary>
        /// ID del permiso para el cual buscar roles
        /// </summary>
        [Required]
        public Guid PermissionId { get; set; }

        /// <summary>
        /// Término de búsqueda
        /// </summary>
        public string SearchTerm { get; set; } = string.Empty;

        /// <summary>
        /// Filtrar solo roles que tienen el permiso asignado
        /// Si es false, muestra todos los roles con información de si tienen el permiso
        /// </summary>
        public bool ShowOnlyWithPermission { get; set; } = true;

        /// <summary>
        /// Filtrar solo roles activos
        /// </summary>
        public bool ShowOnlyActive { get; set; } = true;

        /// <summary>
        /// Página actual (1-based)
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Cantidad de registros por página
        /// </summary>
        public int PageSize { get; set; } = 10;
    }
}