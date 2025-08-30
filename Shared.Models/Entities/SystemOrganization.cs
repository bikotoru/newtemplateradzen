using System;
using System.Collections.Generic;

namespace Shared.Models.Entities;

public partial class SystemOrganization
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Rut { get; set; }

    public string? CustomData { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaModificacion { get; set; }

    public bool Active { get; set; }

    public virtual ICollection<SystemPermission> SystemPermissions { get; set; } = new List<SystemPermission>();

    public virtual ICollection<SystemRole> SystemRoles { get; set; } = new List<SystemRole>();

    public virtual ICollection<SystemRolesPermission> SystemRolesPermissions { get; set; } = new List<SystemRolesPermission>();

    public virtual ICollection<SystemUser> SystemUsers { get; set; } = new List<SystemUser>();

    public virtual ICollection<SystemUsersPermission> SystemUsersPermissions { get; set; } = new List<SystemUsersPermission>();

    public virtual ICollection<SystemUsersRole> SystemUsersRoles { get; set; } = new List<SystemUsersRole>();
}
