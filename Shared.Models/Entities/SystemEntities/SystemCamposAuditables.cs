using System;
using System.Collections.Generic;

namespace Shared.Models.Entities.SystemEntities;

public partial class SystemCamposAuditables
{
    public Guid Id { get; set; }

    public Guid? OrganizacionId { get; set; }

    public string Tabla { get; set; } = null!;

    public string Campo { get; set; } = null!;

    public bool? Activo { get; set; }

    public bool? IsCustom { get; set; }

    public DateTime? FechaCreacion { get; set; }

    public Guid? CreadoPor { get; set; }
}
