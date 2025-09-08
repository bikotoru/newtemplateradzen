using System;
using System.Collections.Generic;

namespace Shared.Models.Entities;

public partial class Venta
{
    public Guid Id { get; set; }

    public Guid? OrganizationId { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaModificacion { get; set; }

    public Guid? CreadorId { get; set; }

    public Guid? ModificadorId { get; set; }

    public bool Active { get; set; }

    public int? Numventa { get; set; }

    public int? Montototal { get; set; }

    public virtual SystemUsers? Creador { get; set; }

    public virtual SystemUsers? Modificador { get; set; }

    public virtual ICollection<NnventaProductos> NnventaProductos { get; set; } = new List<NnventaProductos>();

    public virtual SystemOrganization? Organization { get; set; }
}
