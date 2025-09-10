using System;

namespace Shared.Models.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class NoSelectAttribute : Attribute
    {
        public NoSelectAttribute() { }
    }
}