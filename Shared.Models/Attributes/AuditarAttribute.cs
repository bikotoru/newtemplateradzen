using System;

namespace Shared.Models.Attributes
{
    /// <summary>
    /// Atributo para marcar campos que deben ser auditados automáticamente
    /// Los cambios en campos con este atributo se registran en system_auditoria
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class AuditarAttribute : Attribute
    {
        public AuditarAttribute() { }
    }
}