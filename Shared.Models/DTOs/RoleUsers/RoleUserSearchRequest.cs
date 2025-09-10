using System.ComponentModel.DataAnnotations;

namespace Shared.Models.DTOs.RoleUsers;

public class RoleUserSearchRequest
{
    [Required]
    public Guid RoleId { get; set; }
    
    public string SearchTerm { get; set; } = string.Empty;
    
    public bool ShowOnlyAssigned { get; set; } = false;
    
    public int Page { get; set; } = 1;
    
    public int PageSize { get; set; } = 10;
}