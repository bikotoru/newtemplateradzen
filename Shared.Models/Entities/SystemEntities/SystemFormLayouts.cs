using System;
using System.Collections.Generic;

namespace Shared.Models.Entities.SystemEntities;

public partial class SystemFormLayouts
{
    public Guid Id { get; set; }

    public string EntityName { get; set; } = null!;

    public string FormName { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; }

    public int Version { get; set; }

    public string LayoutConfig { get; set; } = null!;

    public Guid? OrganizationId { get; set; }

    public Guid? CreadorId { get; set; }

    public Guid? ModificadorId { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaModificacion { get; set; }
}
