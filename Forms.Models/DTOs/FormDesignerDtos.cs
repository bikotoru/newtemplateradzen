using System.ComponentModel.DataAnnotations;
using Forms.Models.Configurations;

namespace Forms.Models.DTOs;

// DTOs para el diseñador visual de formularios
public class FormEntityDto
{
    public Guid Id { get; set; }
    public string EntityName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Description { get; set; }
    public string? IconName { get; set; }
    public string? Category { get; set; }
    public bool AllowCustomFields { get; set; }
    public bool IsActive { get; set; }
}

public class FormFieldItemDto
{
    public Guid? Id { get; set; } // null si es un campo del sistema
    public string FieldName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string FieldType { get; set; } = "";
    public string? Description { get; set; }
    public bool IsRequired { get; set; }
    public bool IsSystemField { get; set; } // true para campos nativos de la entidad
    public bool IsCustomField { get; set; } // true para campos personalizados
    public string? IconName { get; set; }
    public string Category { get; set; } = ""; // "System", "Custom", "Related", etc.
}

public class FormSectionDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public int GridSize { get; set; } = 12; // 4, 6, 8, 12 columnas
    public int SortOrder { get; set; }
    public bool IsCollapsible { get; set; } = true;
    public bool IsExpanded { get; set; } = true;
    public List<FormFieldLayoutDto> Fields { get; set; } = new();
}

public class FormFieldLayoutDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FieldName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string FieldType { get; set; } = "";
    public int GridSize { get; set; } = 6; // 4, 6, 8, 12 columnas
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; }
    public bool IsReadOnly { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsSystemField { get; set; }
    public string? Conditions { get; set; } // JSON con condiciones de visibilidad
    public string? Description { get; set; }
    public string? DefaultValue { get; set; }

    // Configuraciones avanzadas para campos personalizados
    public ValidationConfig? ValidationConfig { get; set; }
    public UIConfig? UIConfig { get; set; }
}

public class FormLayoutDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EntityName { get; set; } = "";
    public string FormName { get; set; } = "";
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public List<FormSectionDto> Sections { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? OrganizationId { get; set; }
}

public class SaveFormLayoutRequest
{
    [Required]
    public string EntityName { get; set; } = "";

    [Required]
    [MaxLength(200)]
    public string FormName { get; set; } = "";

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsDefault { get; set; }

    [Required]
    public List<FormSectionDto> Sections { get; set; } = new();
}

public class GetAvailableFieldsRequest
{
    [Required]
    public string EntityName { get; set; } = "";
    public Guid? OrganizationId { get; set; }
}

public class GetAvailableFieldsResponse
{
    public List<FormFieldItemDto> SystemFields { get; set; } = new();
    public List<FormFieldItemDto> CustomFields { get; set; } = new();
    public List<FormFieldItemDto> RelatedFields { get; set; } = new();
}