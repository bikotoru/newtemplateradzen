using System;
using System.Collections.Generic;

namespace Shared.Models.Entities;

public partial class SystemConfig
{
    public Guid Id { get; set; }

    public string Field { get; set; } = null!;

    public string TypeField { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaModificacion { get; set; }

    public Guid? OrganizationId { get; set; }

    public Guid? CreadorId { get; set; }

    public Guid? ModificadorId { get; set; }

    public bool Active { get; set; }

    public virtual SystemUsers? Creador { get; set; }

    public virtual SystemUsers? Modificador { get; set; }

    public virtual SystemOrganization? Organization { get; set; }

    public virtual ICollection<SystemConfigValues> SystemConfigValues { get; set; } = new List<SystemConfigValues>();
}
