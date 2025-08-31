using System.Collections;
using System.Linq.Expressions;

namespace Shared.Models.Requests
{
    public class UpdateBatchRequest<T> where T : class
    {
        /// <summary>
        /// Lista de requests individuales para actualizar
        /// </summary>
        public List<UpdateRequest<T>> Requests { get; set; } = new();
        
        /// <summary>
        /// Configuración global que se aplica a todos los requests si no tienen configuración específica
        /// </summary>
        public GlobalUpdateBatchConfiguration<T>? GlobalConfiguration { get; set; }
        
        /// <summary>
        /// Procesar en transacción (por defecto true)
        /// </summary>
        public bool UseTransaction { get; set; } = true;
        
        /// <summary>
        /// Continuar si hay errores individuales (por defecto false)
        /// </summary>
        public bool ContinueOnError { get; set; } = false;
    }
    
    public class GlobalUpdateBatchConfiguration<T> where T : class
    {
        public Expression<Func<T, object>>[]? UpdateFields { get; set; }
        public Expression<Func<T, object>>[]? IncludeRelations { get; set; }
        public Expression<Func<T, object>>[]? UpdateCollections { get; set; }
        public Expression<Func<T, bool>>? WhereClause { get; set; }
        public Expression<Func<T, object>>[]? IncludeForResponse { get; set; }
    }
}