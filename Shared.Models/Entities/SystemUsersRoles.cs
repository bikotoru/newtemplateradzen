using System;
using System.Collections.Generic;

namespace Shared.Models.Entities;

public partial class SystemUsersRoles
{
    public Guid Id { get; set; }

    public Guid SystemRolesId { get; set; }

    public Guid SystemUsersId { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaModificacion { get; set; }

    public Guid? OrganizationId { get; set; }

    public Guid? CreadorId { get; set; }

    public Guid? ModificadorId { get; set; }

    public bool Active { get; set; }

    public virtual SystemUsers? Creador { get; set; }

    public virtual SystemUsers? Modificador { get; set; }

    public virtual SystemOrganization? Organization { get; set; }

    public virtual SystemRoles SystemRoles { get; set; } = null!;

    public virtual SystemUsers SystemUsers { get; set; } = null!;
}
