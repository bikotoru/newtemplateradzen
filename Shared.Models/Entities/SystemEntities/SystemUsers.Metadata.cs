using System;
using System.ComponentModel.DataAnnotations;
using Shared.Models.Attributes;

namespace Shared.Models.Entities.SystemEntities
{
    [MetadataType(typeof(SystemUsersMetadata))]
    public partial class SystemUsers { }

    public class SystemUsersMetadata
    {
        [NoSelect]
        public string Password;
    }
}