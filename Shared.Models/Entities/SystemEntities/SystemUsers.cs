using System;
using System.Collections.Generic;

namespace Shared.Models.Entities.SystemEntities;

public partial class SystemUsers
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? CustomData { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaModificacion { get; set; }

    public Guid? OrganizationId { get; set; }

    public Guid? CreadorId { get; set; }

    public Guid? ModificadorId { get; set; }

    public bool Active { get; set; }

    public virtual SystemUsers? Creador { get; set; }

    public virtual ICollection<SystemUsers> InverseCreador { get; set; } = new List<SystemUsers>();

    public virtual ICollection<SystemUsers> InverseModificador { get; set; } = new List<SystemUsers>();

    public virtual SystemUsers? Modificador { get; set; }

    public virtual SystemOrganization? Organization { get; set; }

    public virtual ICollection<Region> RegionCreador { get; set; } = new List<Region>();

    public virtual ICollection<Region> RegionModificador { get; set; } = new List<Region>();

    public virtual ICollection<SystemAuditoria> SystemAuditoriaCreador { get; set; } = new List<SystemAuditoria>();

    public virtual ICollection<SystemAuditoriaDetalle> SystemAuditoriaDetalleCreador { get; set; } = new List<SystemAuditoriaDetalle>();

    public virtual ICollection<SystemAuditoriaDetalle> SystemAuditoriaDetalleModificador { get; set; } = new List<SystemAuditoriaDetalle>();

    public virtual ICollection<SystemAuditoria> SystemAuditoriaModificador { get; set; } = new List<SystemAuditoria>();

    public virtual ICollection<SystemCustomFieldAuditLog> SystemCustomFieldAuditLog { get; set; } = new List<SystemCustomFieldAuditLog>();

    public virtual ICollection<SystemCustomFieldDefinitions> SystemCustomFieldDefinitionsCreador { get; set; } = new List<SystemCustomFieldDefinitions>();

    public virtual ICollection<SystemCustomFieldDefinitions> SystemCustomFieldDefinitionsModificador { get; set; } = new List<SystemCustomFieldDefinitions>();

    public virtual ICollection<SystemCustomFieldTemplates> SystemCustomFieldTemplatesCreador { get; set; } = new List<SystemCustomFieldTemplates>();

    public virtual ICollection<SystemCustomFieldTemplates> SystemCustomFieldTemplatesModificador { get; set; } = new List<SystemCustomFieldTemplates>();

    public virtual ICollection<SystemFormEntities> SystemFormEntitiesCreador { get; set; } = new List<SystemFormEntities>();

    public virtual ICollection<SystemFormEntities> SystemFormEntitiesModificador { get; set; } = new List<SystemFormEntities>();

    public virtual ICollection<SystemFormLayouts> SystemFormLayoutsCreador { get; set; } = new List<SystemFormLayouts>();

    public virtual ICollection<SystemFormLayouts> SystemFormLayoutsModificador { get; set; } = new List<SystemFormLayouts>();

    public virtual ICollection<SystemPermissions> SystemPermissionsCreador { get; set; } = new List<SystemPermissions>();

    public virtual ICollection<SystemPermissions> SystemPermissionsModificador { get; set; } = new List<SystemPermissions>();

    public virtual ICollection<SystemRoles> SystemRolesCreador { get; set; } = new List<SystemRoles>();

    public virtual ICollection<SystemRoles> SystemRolesModificador { get; set; } = new List<SystemRoles>();

    public virtual ICollection<SystemRolesPermissions> SystemRolesPermissionsCreador { get; set; } = new List<SystemRolesPermissions>();

    public virtual ICollection<SystemRolesPermissions> SystemRolesPermissionsModificador { get; set; } = new List<SystemRolesPermissions>();

    public virtual ICollection<SystemSavedQueries> SystemSavedQueriesCreador { get; set; } = new List<SystemSavedQueries>();

    public virtual ICollection<SystemSavedQueries> SystemSavedQueriesModificador { get; set; } = new List<SystemSavedQueries>();

    public virtual ICollection<SystemSavedQueryShares> SystemSavedQuerySharesCreador { get; set; } = new List<SystemSavedQueryShares>();

    public virtual ICollection<SystemSavedQueryShares> SystemSavedQuerySharesModificador { get; set; } = new List<SystemSavedQueryShares>();

    public virtual ICollection<SystemSavedQueryShares> SystemSavedQuerySharesSharedWithUser { get; set; } = new List<SystemSavedQueryShares>();

    public virtual ICollection<SystemUsersPermissions> SystemUsersPermissionsCreador { get; set; } = new List<SystemUsersPermissions>();

    public virtual ICollection<SystemUsersPermissions> SystemUsersPermissionsModificador { get; set; } = new List<SystemUsersPermissions>();

    public virtual ICollection<SystemUsersPermissions> SystemUsersPermissionsSystemUsers { get; set; } = new List<SystemUsersPermissions>();

    public virtual ICollection<SystemUsersRoles> SystemUsersRolesCreador { get; set; } = new List<SystemUsersRoles>();

    public virtual ICollection<SystemUsersRoles> SystemUsersRolesModificador { get; set; } = new List<SystemUsersRoles>();

    public virtual ICollection<SystemUsersRoles> SystemUsersRolesSystemUsers { get; set; } = new List<SystemUsersRoles>();
}
