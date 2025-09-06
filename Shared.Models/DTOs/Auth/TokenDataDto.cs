namespace Shared.Models.DTOs.Auth
{
    public class TokenDataDto
    {
        public string Id { get; set; } = string.Empty;
        public string OrganizationId { get; set; } = string.Empty;
        public DateTime Expired { get; set; }
    }
}