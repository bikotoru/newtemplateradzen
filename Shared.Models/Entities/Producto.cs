using System;
using System.Collections.Generic;

namespace Shared.Models.Entities;

public partial class Producto
{
    public Guid Id { get; set; }

    public Guid? OrganizationId { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaModificacion { get; set; }

    public Guid? CreadorId { get; set; }

    public Guid? ModificadorId { get; set; }

    public bool Active { get; set; }

    public string? Nombre { get; set; }

    public string? Codigosku { get; set; }

    public int? Precioventa { get; set; }

    public int? Preciocompra { get; set; }

    public Guid? MarcaId { get; set; }

    public Guid? CategoriaId { get; set; }

    public virtual Categoria? Categoria { get; set; }

    public virtual SystemUsers? Creador { get; set; }

    public virtual Marca? Marca { get; set; }

    public virtual SystemUsers? Modificador { get; set; }

    public virtual ICollection<NnventaProductos> NnventaProductos { get; set; } = new List<NnventaProductos>();

    public virtual SystemOrganization? Organization { get; set; }
}
