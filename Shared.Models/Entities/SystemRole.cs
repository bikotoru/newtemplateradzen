using System;
using System.Collections.Generic;

namespace Shared.Models.Entities;

public partial class SystemRole
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public string TypeRole { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaModificacion { get; set; }

    public Guid? OrganizationId { get; set; }

    public Guid? CreadorId { get; set; }

    public Guid? ModificadorId { get; set; }

    public bool Active { get; set; }

    public virtual SystemUser? Creador { get; set; }

    public virtual SystemUser? Modificador { get; set; }

    public virtual SystemOrganization? Organization { get; set; }

    public virtual ICollection<SystemRolesPermission> SystemRolesPermissions { get; set; } = new List<SystemRolesPermission>();

    public virtual ICollection<SystemUsersRole> SystemUsersRoles { get; set; } = new List<SystemUsersRole>();
}
