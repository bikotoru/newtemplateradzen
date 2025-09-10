using System.ComponentModel.DataAnnotations;

namespace Shared.Models.DTOs.RoleUsers;

public class RoleUserDto
{
    public Guid UserId { get; set; }
    
    [Required]
    public string Nombre { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public bool IsAssigned { get; set; }
    
    public DateTime? FechaCreacion { get; set; }
    
    public bool Active { get; set; }
}