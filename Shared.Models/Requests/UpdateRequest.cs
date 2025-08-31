using System.Linq.Expressions;

namespace Shared.Models.Requests
{
    public class UpdateRequest<T> : BaseRequest<T> where T : class
    {
        /// <summary>
        /// Campos específicos a actualizar (fuertemente tipado)
        /// Si es null, se actualizan todos los campos
        /// </summary>
        public Expression<Func<T, object>>[]? UpdateFields { get; set; }
        
        /// <summary>
        /// Relaciones N:N (ICollection) a actualizar/sincronizar
        /// </summary>
        public Expression<Func<T, object>>[]? UpdateCollections { get; set; }
        
        /// <summary>
        /// Where clause para condiciones adicionales
        /// </summary>
        public Expression<Func<T, bool>>? WhereClause { get; set; }
        
        /// <summary>
        /// Incluir relaciones en el response
        /// </summary>
        public Expression<Func<T, object>>[]? IncludeForResponse { get; set; }
    }
}