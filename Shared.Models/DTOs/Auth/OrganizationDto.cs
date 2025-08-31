namespace Shared.Models.DTOs.Auth
{
    public class OrganizationDto
    {
        public Guid Id { get; set; } = Guid.Empty;
        public string Nombre { get; set; } = string.Empty;
    }
}