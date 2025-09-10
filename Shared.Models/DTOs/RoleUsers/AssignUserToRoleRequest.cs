using System.ComponentModel.DataAnnotations;

namespace Shared.Models.DTOs.RoleUsers;

public class AssignUserToRoleRequest
{
    [Required]
    public Guid RoleId { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
}