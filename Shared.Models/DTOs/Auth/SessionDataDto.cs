namespace Shared.Models.DTOs.Auth
{
    public class SessionDataDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public Guid Id { get; set; } = Guid.Empty;
        public List<RoleDto> Roles { get; set; } = new();
        public List<string> Permisos { get; set; } = new();
        public OrganizationDto Organization { get; set; } = new();
        
        // Propiedades de conveniencia para acceso directo
        public Guid? OrganizationId => Organization?.Id;
        public string? OrganizationName => Organization?.Nombre;
    }
}