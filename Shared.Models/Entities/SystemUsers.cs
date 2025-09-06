using System;
using System.Collections.Generic;

namespace Shared.Models.Entities;

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


    public virtual ICollection<SystemConfig> SystemConfigCreador { get; set; } = new List<SystemConfig>();

    public virtual ICollection<SystemConfig> SystemConfigModificador { get; set; } = new List<SystemConfig>();

    public virtual ICollection<SystemConfigValues> SystemConfigValuesCreador { get; set; } = new List<SystemConfigValues>();

    public virtual ICollection<SystemConfigValues> SystemConfigValuesModificador { get; set; } = new List<SystemConfigValues>();

    public virtual ICollection<SystemPermissions> SystemPermissionsCreador { get; set; } = new List<SystemPermissions>();

    public virtual ICollection<SystemPermissions> SystemPermissionsModificador { get; set; } = new List<SystemPermissions>();

    public virtual ICollection<SystemRoles> SystemRolesCreador { get; set; } = new List<SystemRoles>();

    public virtual ICollection<SystemRoles> SystemRolesModificador { get; set; } = new List<SystemRoles>();

    public virtual ICollection<SystemRolesPermissions> SystemRolesPermissionsCreador { get; set; } = new List<SystemRolesPermissions>();

    public virtual ICollection<SystemRolesPermissions> SystemRolesPermissionsModificador { get; set; } = new List<SystemRolesPermissions>();

    public virtual ICollection<SystemUsersPermissions> SystemUsersPermissionsCreador { get; set; } = new List<SystemUsersPermissions>();

    public virtual ICollection<SystemUsersPermissions> SystemUsersPermissionsModificador { get; set; } = new List<SystemUsersPermissions>();

    public virtual ICollection<SystemUsersPermissions> SystemUsersPermissionsSystemUsers { get; set; } = new List<SystemUsersPermissions>();

    public virtual ICollection<SystemUsersRoles> SystemUsersRolesCreador { get; set; } = new List<SystemUsersRoles>();

    public virtual ICollection<SystemUsersRoles> SystemUsersRolesModificador { get; set; } = new List<SystemUsersRoles>();

    public virtual ICollection<SystemUsersRoles> SystemUsersRolesSystemUsers { get; set; } = new List<SystemUsersRoles>();
}
