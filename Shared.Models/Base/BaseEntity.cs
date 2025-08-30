using System;

namespace Shared.Models.Base
{
    public class BaseEntity
    {
        public Guid Id { get; set; }
        public Guid? OrganizationId { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaModificacion { get; set; }
        public Guid CreadorId { get; set; }
        public Guid ModificadorId { get; set; }
        public bool Active { get; set; }
    }
}