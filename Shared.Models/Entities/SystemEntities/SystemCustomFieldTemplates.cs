using System;
using System.Collections.Generic;

namespace Shared.Models.Entities.SystemEntities;

public partial class SystemCustomFieldTemplates
{
    public Guid Id { get; set; }

    public string TemplateName { get; set; } = null!;

    public string? Description { get; set; }

    public string? Category { get; set; }

    public string TargetEntityName { get; set; } = null!;

    public string FieldsDefinition { get; set; } = null!;

    public bool IsSystemTemplate { get; set; }

    public int UsageCount { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaModificacion { get; set; }

    public Guid? OrganizationId { get; set; }

    public Guid? CreadorId { get; set; }

    public Guid? ModificadorId { get; set; }

    public bool Active { get; set; }

    public virtual SystemUsers? Creador { get; set; }

    public virtual SystemUsers? Modificador { get; set; }

    public virtual SystemOrganization? Organization { get; set; }
}
