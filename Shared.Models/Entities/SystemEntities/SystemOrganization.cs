using System;
using System.Collections.Generic;

namespace Shared.Models.Entities.SystemEntities;

public partial class SystemOrganization
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Rut { get; set; }

    public string? CustomData { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaModificacion { get; set; }

    public bool Active { get; set; }

    public virtual ICollection<Region> Region { get; set; } = new List<Region>();

    public virtual ICollection<SystemAuditoria> SystemAuditoria { get; set; } = new List<SystemAuditoria>();

    public virtual ICollection<SystemAuditoriaDetalle> SystemAuditoriaDetalle { get; set; } = new List<SystemAuditoriaDetalle>();

    public virtual ICollection<SystemCustomFieldAuditLog> SystemCustomFieldAuditLog { get; set; } = new List<SystemCustomFieldAuditLog>();

    public virtual ICollection<SystemCustomFieldDefinitions> SystemCustomFieldDefinitions { get; set; } = new List<SystemCustomFieldDefinitions>();

    public virtual ICollection<SystemCustomFieldTemplates> SystemCustomFieldTemplates { get; set; } = new List<SystemCustomFieldTemplates>();

    public virtual ICollection<SystemFormEntities> SystemFormEntities { get; set; } = new List<SystemFormEntities>();

    public virtual ICollection<SystemPermissions> SystemPermissions { get; set; } = new List<SystemPermissions>();

    public virtual ICollection<SystemRoles> SystemRoles { get; set; } = new List<SystemRoles>();

    public virtual ICollection<SystemRolesPermissions> SystemRolesPermissions { get; set; } = new List<SystemRolesPermissions>();

    public virtual ICollection<SystemUsers> SystemUsers { get; set; } = new List<SystemUsers>();

    public virtual ICollection<SystemUsersPermissions> SystemUsersPermissions { get; set; } = new List<SystemUsersPermissions>();

    public virtual ICollection<SystemUsersRoles> SystemUsersRoles { get; set; } = new List<SystemUsersRoles>();

    public virtual ICollection<ZToken> ZToken { get; set; } = new List<ZToken>();
}
