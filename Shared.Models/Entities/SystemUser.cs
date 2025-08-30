using System;
using System.Collections.Generic;

namespace Shared.Models.Entities;

public partial class SystemUser
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

    public virtual SystemUser? Creador { get; set; }

    public virtual ICollection<SystemUser> InverseCreador { get; set; } = new List<SystemUser>();

    public virtual ICollection<SystemUser> InverseModificador { get; set; } = new List<SystemUser>();

    public virtual SystemUser? Modificador { get; set; }

    public virtual SystemOrganization? Organization { get; set; }

    public virtual ICollection<SystemPermission> SystemPermissionCreadors { get; set; } = new List<SystemPermission>();

    public virtual ICollection<SystemPermission> SystemPermissionModificadors { get; set; } = new List<SystemPermission>();

    public virtual ICollection<SystemRole> SystemRoleCreadors { get; set; } = new List<SystemRole>();

    public virtual ICollection<SystemRole> SystemRoleModificadors { get; set; } = new List<SystemRole>();

    public virtual ICollection<SystemRolesPermission> SystemRolesPermissionCreadors { get; set; } = new List<SystemRolesPermission>();

    public virtual ICollection<SystemRolesPermission> SystemRolesPermissionModificadors { get; set; } = new List<SystemRolesPermission>();

    public virtual ICollection<SystemUsersPermission> SystemUsersPermissionCreadors { get; set; } = new List<SystemUsersPermission>();

    public virtual ICollection<SystemUsersPermission> SystemUsersPermissionModificadors { get; set; } = new List<SystemUsersPermission>();

    public virtual ICollection<SystemUsersPermission> SystemUsersPermissionSystemUsers { get; set; } = new List<SystemUsersPermission>();

    public virtual ICollection<SystemUsersRole> SystemUsersRoleCreadors { get; set; } = new List<SystemUsersRole>();

    public virtual ICollection<SystemUsersRole> SystemUsersRoleModificadors { get; set; } = new List<SystemUsersRole>();

    public virtual ICollection<SystemUsersRole> SystemUsersRoleSystemUsers { get; set; } = new List<SystemUsersRole>();
}
