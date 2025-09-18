using System;
using System.Collections.Generic;

namespace Shared.Models.Entities.SystemEntities;

public partial class SystemConfigValues
{
    public Guid Id { get; set; }

    public Guid SystemConfigId { get; set; }

    public string Value { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaModificacion { get; set; }

    public Guid? OrganizationId { get; set; }

    public Guid? CreadorId { get; set; }

    public Guid? ModificadorId { get; set; }

    public bool Active { get; set; }

    public virtual SystemConfig SystemConfig { get; set; } = null!;
}
