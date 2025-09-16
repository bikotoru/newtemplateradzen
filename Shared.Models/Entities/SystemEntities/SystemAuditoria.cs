using System;
using System.Collections.Generic;

namespace Shared.Models.Entities.SystemEntities;

public partial class SystemAuditoria
{
    public Guid Id { get; set; }

    public Guid? OrganizationId { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaModificacion { get; set; }

    public Guid? CreadorId { get; set; }

    public Guid? ModificadorId { get; set; }

    public bool Active { get; set; }

    public string Tabla { get; set; } = null!;

    public Guid RegistroId { get; set; }

    public string Action { get; set; } = null!;

    public string? Comentario { get; set; }

    public virtual SystemUsers? Creador { get; set; }

    public virtual SystemUsers? Modificador { get; set; }

    public virtual SystemOrganization? Organization { get; set; }

    public virtual ICollection<SystemAuditoriaDetalle> SystemAuditoriaDetalle { get; set; } = new List<SystemAuditoriaDetalle>();
}
