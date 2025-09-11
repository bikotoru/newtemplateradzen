using System;
using System.Collections.Generic;

namespace Shared.Models.Entities;

public partial class ZToken
{
    public Guid Id { get; set; }

    public string? Data { get; set; }

    public Guid? Organizationid { get; set; }

    public bool? Refresh { get; set; }

    public bool? Logout { get; set; }

    public virtual SystemOrganization? Organization { get; set; }
}
