using System;

namespace Shared.Models.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class FieldPermissionAttribute : Attribute
    {
        public string? CREATE { get; set; }
        public string? UPDATE { get; set; }
        public string? VIEW { get; set; }

        public FieldPermissionAttribute() { }
    }
}