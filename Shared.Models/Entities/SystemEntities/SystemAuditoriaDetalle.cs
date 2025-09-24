using System;
using System.Collections.Generic;

namespace Shared.Models.Entities.SystemEntities;

public partial class SystemAuditoriaDetalle
{
    public Guid Id { get; set; }

    public Guid? OrganizationId { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaModificacion { get; set; }

    public Guid? CreadorId { get; set; }

    public Guid? ModificadorId { get; set; }

    public bool Active { get; set; }

    public Guid AuditoriaId { get; set; }

    public string Campo { get; set; } = null!;

    public string? ValorAnterior { get; set; }

    public string? NuevoValor { get; set; }

    public virtual SystemAuditoria Auditoria { get; set; } = null!;

    public virtual SystemUsers? Creador { get; set; }

    public virtual SystemUsers? Modificador { get; set; }

    public virtual SystemOrganization? Organization { get; set; }
}
