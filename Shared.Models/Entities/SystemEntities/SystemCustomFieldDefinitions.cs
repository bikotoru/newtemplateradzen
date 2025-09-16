using System;
using System.Collections.Generic;

namespace Shared.Models.Entities.SystemEntities;

public partial class SystemCustomFieldDefinitions
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

    public string? ValidationConfig { get; set; }

    public string? Uiconfig { get; set; }

    public string? ConditionsConfig { get; set; }

    public string? PermissionCreate { get; set; }

    public string? PermissionUpdate { get; set; }

    public string? PermissionView { get; set; }

    public bool IsEnabled { get; set; }

    public int Version { get; set; }

    public string? Tags { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaModificacion { get; set; }

    public Guid? OrganizationId { get; set; }

    public Guid? CreadorId { get; set; }

    public Guid? ModificadorId { get; set; }

    public bool Active { get; set; }

    public virtual ICollection<SystemCustomFieldAuditLog> SystemCustomFieldAuditLog { get; set; } = new List<SystemCustomFieldAuditLog>();
}
