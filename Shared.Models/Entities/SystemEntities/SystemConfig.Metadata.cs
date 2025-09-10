using System;
using System.ComponentModel.DataAnnotations;
using Shared.Models.Attributes;

namespace Shared.Models.Entities.SystemEntities
{
    [MetadataType(typeof(SystemConfigMetadata))]
    public partial class SystemConfig { }

    public class SystemConfigMetadata
    {
        [NoSelect]
        public string Name;
    }
}
