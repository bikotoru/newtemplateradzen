using System;

namespace Shared.Models.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class AutoIncrementalAttribute : Attribute
    {
        public AutoIncrementalAttribute() { }
    }
}