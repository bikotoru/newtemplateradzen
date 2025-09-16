using System;
using System.Collections.Generic;

namespace Shared.Models.Entities.SystemEntities;

public partial class SystemCustomFieldAuditLog
{
    public Guid Id { get; set; }

    public Guid CustomFieldDefinitionId { get; set; }

    public string EntityName { get; set; } = null!;

    public string FieldName { get; set; } = null!;

    public string ChangeType { get; set; } = null!;

    public string? OldDefinition { get; set; }

    public string? NewDefinition { get; set; }

    public string? ChangedProperties { get; set; }

    public string? ChangeReason { get; set; }

    public string? ImpactAssessment { get; set; }

    public DateTime FechaCreacion { get; set; }

    public Guid? OrganizationId { get; set; }

    public Guid? CreadorId { get; set; }

    public virtual SystemCustomFieldDefinitions CustomFieldDefinition { get; set; } = null!;
}
