using System;
using System.Collections.Generic;

namespace Shared.Models.Entities;

public partial class NnventaProductos
{
    public Guid Id { get; set; }

    public Guid? OrganizationId { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaModificacion { get; set; }

    public Guid? CreadorId { get; set; }

    public Guid? ModificadorId { get; set; }

    public bool Active { get; set; }

    public int? Cantidad { get; set; }

    public int? Precioneto { get; set; }

    public int? Descuentopeso { get; set; }

    public decimal? Descuentoporcentaje { get; set; }

    public int? Montototal { get; set; }

    public Guid? VentaId { get; set; }

    public Guid? ProductoId { get; set; }

    public virtual SystemUsers? Creador { get; set; }

    public virtual SystemUsers? Modificador { get; set; }

    public virtual SystemOrganization? Organization { get; set; }

    public virtual Producto? Producto { get; set; }

    public virtual Venta? Venta { get; set; }
}
