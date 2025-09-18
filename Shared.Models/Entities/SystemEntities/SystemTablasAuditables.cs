using System;
using System.Collections.Generic;

namespace Shared.Models.Entities.SystemEntities;

public partial class SystemTablasAuditables
{
    public Guid Id { get; set; }

    public Guid? OrganizacionId { get; set; }

    public string Tabla { get; set; } = null!;

    public bool? Activo { get; set; }

    public bool? TriggerCreado { get; set; }

    public DateTime? FechaCreacion { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public Guid? CreadoPor { get; set; }
}
