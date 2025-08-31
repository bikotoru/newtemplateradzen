namespace Shared.Models.DTOs.Auth
{
    public class SessionDataDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public List<RoleDto> Roles { get; set; } = new();
        public List<string> Permisos { get; set; } = new();
        public OrganizationDto Organization { get; set; } = new();
    }
}