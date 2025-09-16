using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Models.Base;

namespace Shared.Models.Entities.SystemEntities;

[Table("system_form_entities")]
public class SystemFormEntity : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string EntityName { get; set; } = "";

    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = "";

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(100)]
    public string TableName { get; set; } = "";

    public bool AllowCustomFields { get; set; } = true;

    [MaxLength(50)]
    public string? IconName { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    public int SortOrder { get; set; } = 100;
}