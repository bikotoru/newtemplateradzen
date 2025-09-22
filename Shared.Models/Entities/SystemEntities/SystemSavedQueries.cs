using System;
using System.Collections.Generic;

namespace Shared.Models.Entities.SystemEntities;

public partial class SystemSavedQueries
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string EntityName { get; set; } = null!;

    public string SelectedFields { get; set; } = null!;

    public string? FilterConfiguration { get; set; }

    public byte LogicalOperator { get; set; }

    public int TakeLimit { get; set; }

    public bool IsPublic { get; set; }

    public bool IsTemplate { get; set; }

    public Guid? OrganizationId { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaModificacion { get; set; }

    public Guid? CreadorId { get; set; }

    public Guid? ModificadorId { get; set; }

    public bool Active { get; set; }

    public virtual ICollection<SystemSavedQueryShares> SystemSavedQueryShares { get; set; } = new List<SystemSavedQueryShares>();
}
