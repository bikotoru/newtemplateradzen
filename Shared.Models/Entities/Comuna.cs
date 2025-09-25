using System;
using System.Collections.Generic;

namespace Shared.Models.Entities;

/// <summary>
/// Core.Localidades
/// </summary>
public partial class Comuna
{
    public Guid Id { get; set; }

    public Guid? OrganizationId { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaModificacion { get; set; }

    public Guid? CreadorId { get; set; }

    public Guid? ModificadorId { get; set; }

    public bool Active { get; set; }

    public string? CustomFields { get; set; }

    public string? Nombre { get; set; }

    public Guid? RegionId { get; set; }

    public virtual SystemUsers? Creador { get; set; }

    public virtual SystemUsers? Modificador { get; set; }

    public virtual SystemOrganization? Organization { get; set; }

    public virtual Region? Region { get; set; }
}
