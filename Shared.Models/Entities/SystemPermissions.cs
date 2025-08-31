using System;
using System.Collections.Generic;

namespace Shared.Models.Entities;

public partial class SystemPermissions
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaModificacion { get; set; }

    public Guid? OrganizationId { get; set; }

    public Guid? CreadorId { get; set; }

    public Guid? ModificadorId { get; set; }

    public bool Active { get; set; }

    public string? ActionKey { get; set; }

    public string? GroupKey { get; set; }

    public string? GrupoNombre { get; set; }

    public virtual SystemUsers? Creador { get; set; }

    public virtual SystemUsers? Modificador { get; set; }

    public virtual SystemOrganization? Organization { get; set; }

    public virtual ICollection<SystemRolesPermissions> SystemRolesPermissions { get; set; } = new List<SystemRolesPermissions>();

    public virtual ICollection<SystemUsersPermissions> SystemUsersPermissions { get; set; } = new List<SystemUsersPermissions>();
}
