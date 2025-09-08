using System;
using System.Collections.Generic;

namespace Shared.Models.Entities;

public partial class Marca
{
    public Guid Id { get; set; }

    public Guid? OrganizationId { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaModificacion { get; set; }

    public Guid? CreadorId { get; set; }

    public Guid? ModificadorId { get; set; }

    public bool Active { get; set; }

    public string? Nombre { get; set; }

    public string? Codigointerno { get; set; }

    public virtual SystemUsers? Creador { get; set; }

    public virtual SystemUsers? Modificador { get; set; }

    public virtual SystemOrganization? Organization { get; set; }

    public virtual ICollection<Producto> Producto { get; set; } = new List<Producto>();
}
