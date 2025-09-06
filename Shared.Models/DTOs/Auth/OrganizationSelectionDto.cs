using System.ComponentModel.DataAnnotations;

namespace Shared.Models.DTOs.Auth
{
    public class OrganizationSelectionDto
    {
        [Required(ErrorMessage = "El token temporal es requerido")]
        public string TemporaryToken { get; set; } = string.Empty;

        [Required(ErrorMessage = "El ID de la organización es requerido")]
        public string OrganizationId { get; set; } = string.Empty;
    }
}