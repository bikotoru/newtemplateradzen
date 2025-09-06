using System;

namespace Shared.Models.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class SoloCrearAttribute : Attribute
    {
        public SoloCrearAttribute() { }
    }
}