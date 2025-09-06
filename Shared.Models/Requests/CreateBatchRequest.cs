using System.Collections;
using System.Linq.Expressions;

namespace Shared.Models.Requests
{
    public class CreateBatchRequest<T> where T : class
    {
        /// <summary>
        /// Lista de requests individuales para crear
        /// </summary>
        public List<CreateRequest<T>> Requests { get; set; } = new();
        
        /// <summary>
        /// Configuración global que se aplica a todos los requests si no tienen configuración específica
        /// </summary>
        public GlobalBatchConfiguration<T>? GlobalConfiguration { get; set; }
        
        /// <summary>
        /// Procesar en transacción (por defecto true)
        /// </summary>
        public bool UseTransaction { get; set; } = true;
        
        /// <summary>
        /// Continuar si hay errores individuales (por defecto false)
        /// </summary>
        public bool ContinueOnError { get; set; } = false;
    }
    
    public class GlobalBatchConfiguration<T> where T : class
    {
        public Expression<Func<T, object>>[]? CreateFields { get; set; }
        public Expression<Func<T, object>>[]? IncludeRelations { get; set; }
        public Expression<Func<T, object>>[]? IncludeCollections { get; set; }
        public Expression<Func<T, object>>[]? AutoCreateRelations { get; set; }
        public Expression<Func<T, object>>[]? AutoCreateCollections { get; set; }
    }
}