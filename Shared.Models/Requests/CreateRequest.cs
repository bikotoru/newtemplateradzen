using System.Collections;
using System.Linq.Expressions;

namespace Shared.Models.Requests
{
    public class CreateRequest<T> : BaseRequest<T> where T : class
    {
        /// <summary>
        /// Campos específicos a crear (fuertemente tipado)
        /// Si es null, se crean todos los campos
        /// </summary>
        public Expression<Func<T, object>>[]? CreateFields { get; set; }
        
        /// <summary>
        /// Relaciones N:N (ICollection) a crear/asociar
        /// </summary>
        public Expression<Func<T, object>>[]? IncludeCollections { get; set; }
        
        /// <summary>
        /// Crear relaciones automáticamente si no existen
        /// </summary>
        public Expression<Func<T, object>>[]? AutoCreateRelations { get; set; }
        
        /// <summary>
        /// Crear elementos en colecciones automáticamente
        /// </summary>
        public Expression<Func<T, object>>[]? AutoCreateCollections { get; set; }
    }
}