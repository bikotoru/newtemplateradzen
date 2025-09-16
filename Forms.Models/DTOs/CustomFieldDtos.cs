using Forms.Models.Configurations;
using Forms.Models.Enums;

namespace Forms.Models.DTOs;

public class CustomFieldDefinitionDto
{
    public Guid Id { get; set; }
    public string EntityName { get; set; } = null!;
    public string FieldName { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? Description { get; set; }
    public string FieldType { get; set; } = null!;
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; } = true;

    // Configuraciones tipadas
    public ValidationConfig? ValidationConfig { get; set; }
    public UIConfig? UIConfig { get; set; }

    // Metadatos
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaModificacion { get; set; }
    public Guid? OrganizationId { get; set; }

    // Propiedades computadas
    public FieldType FieldTypeEnum => FieldTypeExtensions.FromString(FieldType);
    public string FieldTypeDisplay => FieldTypeEnum.GetDisplayName();
    public string FieldTypeDescription => FieldTypeEnum.GetDescription();
    public bool HasOptions => FieldTypeEnum.HasOptions();
    public bool IsNumeric => FieldTypeEnum.IsNumeric();
    public bool IsText => FieldTypeEnum.IsText();
}

public class CreateCustomFieldRequest
{
    public string EntityName { get; set; } = null!;
    public string FieldName { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? Description { get; set; }
    public string FieldType { get; set; } = null!;
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public int SortOrder { get; set; }

    // Configuraciones avanzadas
    public ValidationConfig? ValidationConfig { get; set; }
    public UIConfig? UIConfig { get; set; }

    // Metadatos
    public Guid? OrganizationId { get; set; }
}

public class UpdateCustomFieldRequest
{
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public bool? IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsEnabled { get; set; }

    // Configuraciones avanzadas
    public ValidationConfig? ValidationConfig { get; set; }
    public UIConfig? UIConfig { get; set; }
}

public class CustomFieldValidationRequest
{
    public string EntityName { get; set; } = null!;
    public Guid? OrganizationId { get; set; }
    public Dictionary<string, object> Values { get; set; } = new();
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class FieldConditionEvaluationRequest
{
    public string EntityName { get; set; } = null!;
    public Guid? OrganizationId { get; set; }
    public Dictionary<string, object?> FieldValues { get; set; } = new();
}

