using System;
using System.Collections.Generic;

namespace Shared.Models.Entities.SystemEntities;

public partial class SystemSavedQueryShares
{
    public Guid Id { get; set; }

    public Guid SavedQueryId { get; set; }

    public Guid? SharedWithUserId { get; set; }

    public Guid? SharedWithRoleId { get; set; }

    public Guid? SharedWithOrganizationId { get; set; }

    public bool CanView { get; set; }

    public bool CanEdit { get; set; }

    public bool CanExecute { get; set; }

    public bool CanShare { get; set; }

    public Guid? OrganizationId { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaModificacion { get; set; }

    public Guid? CreadorId { get; set; }

    public Guid? ModificadorId { get; set; }

    public bool Active { get; set; }

    public virtual SystemSavedQueries SavedQuery { get; set; } = null!;
}
