using System;
using System.Collections.Generic;

namespace Shared.Models.Entities;

public partial class SystemUsersPermission
{
    public Guid Id { get; set; }

    public Guid SystemPermissionsId { get; set; }

    public Guid SystemUsersId { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaModificacion { get; set; }

    public Guid? OrganizationId { get; set; }

    public Guid? CreadorId { get; set; }

    public Guid? ModificadorId { get; set; }

    public bool Active { get; set; }

    public virtual SystemUser? Creador { get; set; }

    public virtual SystemUser? Modificador { get; set; }

    public virtual SystemOrganization? Organization { get; set; }

    public virtual SystemPermission SystemPermissions { get; set; } = null!;

    public virtual SystemUser SystemUsers { get; set; } = null!;
}
