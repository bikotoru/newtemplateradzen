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

    public virtual ICollection<Categoria> Categoria { get; set; } = new List<Categoria>();

    public virtual ICollection<SystemConfig> SystemConfig { get; set; } = new List<SystemConfig>();

    public virtual ICollection<SystemConfigValues> SystemConfigValues { get; set; } = new List<SystemConfigValues>();

    public virtual ICollection<SystemPermissions> SystemPermissions { get; set; } = new List<SystemPermissions>();

    public virtual ICollection<SystemRoles> SystemRoles { get; set; } = new List<SystemRoles>();

    public virtual ICollection<SystemRolesPermissions> SystemRolesPermissions { get; set; } = new List<SystemRolesPermissions>();

    public virtual ICollection<SystemUsers> SystemUsers { get; set; } = new List<SystemUsers>();

    public virtual ICollection<SystemUsersPermissions> SystemUsersPermissions { get; set; } = new List<SystemUsersPermissions>();

    public virtual ICollection<SystemUsersRoles> SystemUsersRoles { get; set; } = new List<SystemUsersRoles>();
}
