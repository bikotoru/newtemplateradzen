namespace Shared.Models.DTOs.Auth
{
    /// <summary>
    /// Estructura del token antes de encriptar
    /// </summary>
    public class EncryptedTokenDto
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Expired { get; set; }
        public string OrganizationId { get; set; } = string.Empty;
    }
}