using System;
using System.Collections.Generic;

namespace Shared.Models.Entities.Views;

public partial class VwAuditoriaCompleta
{
    public Guid AuditoriaId { get; set; }

    public Guid? OrganizationId { get; set; }

    public string Tabla { get; set; } = null!;

    public Guid RegistroId { get; set; }

    public string Action { get; set; } = null!;

    public DateTime FechaAuditoria { get; set; }

    public Guid? UsuarioModificacion { get; set; }

    public string Campo { get; set; } = null!;

    public string? ValorAnterior { get; set; }

    public string? NuevoValor { get; set; }

    public DateTime FechaDetalle { get; set; }
}
