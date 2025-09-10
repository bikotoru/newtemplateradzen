using System;
using System.ComponentModel.DataAnnotations;

namespace Shared.Models.DTOs.UserPermissions
{
    /// <summary>
    /// Request para buscar usuarios que tienen un permiso específico
    /// </summary>
    public class PermissionUserSearchRequest
    {
        /// <summary>
        /// ID del permiso para el cual buscar usuarios
        /// </summary>
        [Required]
        public Guid PermissionId { get; set; }

        /// <summary>
        /// Término de búsqueda
        /// </summary>
        public string SearchTerm { get; set; } = string.Empty;

        /// <summary>
        /// Filtrar solo usuarios que tienen el permiso asignado directamente
        /// Si es false, muestra todos los usuarios con información de si tienen el permiso
        /// </summary>
        public bool ShowOnlyWithPermission { get; set; } = true;

        /// <summary>
        /// Filtrar solo usuarios con asignación directa (excluye heredados de roles)
        /// </summary>
        public bool ShowOnlyDirectAssignments { get; set; } = false;

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