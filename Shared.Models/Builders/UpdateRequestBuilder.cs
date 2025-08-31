using System.Collections;
using System.Linq.Expressions;
using Shared.Models.Requests;

namespace Shared.Models.Builders
{
    public class UpdateRequestBuilder<T> where T : class
    {
        private readonly T _entity;
        private readonly List<Expression<Func<T, object>>> _updateFields = new();
        private readonly List<Expression<Func<T, object>>> _includeRelations = new();
        private readonly List<Expression<Func<T, object>>> _updateCollections = new();
        private readonly List<Expression<Func<T, object>>> _includeForResponse = new();
        private Expression<Func<T, bool>>? _whereClause;

        public UpdateRequestBuilder(T entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        /// <summary>
        /// Campos específicos a actualizar (fuertemente tipado)
        /// </summary>
        public UpdateRequestBuilder<T> UpdateFields(params Expression<Func<T, object>>[] fields)
        {
            _updateFields.AddRange(fields);
            return this;
        }

        /// <summary>
        /// Incluir relaciones para la operación (fuertemente tipado)
        /// </summary>
        public UpdateRequestBuilder<T> Including<TProperty>(Expression<Func<T, TProperty>> relation)
        {
            _includeRelations.Add(ConvertToObjectExpression(relation));
            return this;
        }

        /// <summary>
        /// Actualizar/sincronizar colecciones específicas (fuertemente tipado)
        /// </summary>
        public UpdateRequestBuilder<T> UpdateCollection<TCollection>(
            Expression<Func<T, ICollection<TCollection>>> collection)
        {
            _updateCollections.Add(ConvertToObjectExpression(collection));
            return this;
        }

        /// <summary>
        /// Incluir relaciones en el response (fuertemente tipado)
        /// </summary>
        public UpdateRequestBuilder<T> IncludeForResponse<TProperty>(Expression<Func<T, TProperty>> relation)
        {
            _includeForResponse.Add(ConvertToObjectExpression(relation));
            return this;
        }

        /// <summary>
        /// Where clause para condiciones adicionales (fuertemente tipado)
        /// </summary>
        public UpdateRequestBuilder<T> Where(Expression<Func<T, bool>> predicate)
        {
            _whereClause = predicate;
            return this;
        }

        public UpdateRequest<T> Build()
        {
            return new UpdateRequest<T>
            {
                Entity = _entity,
                UpdateFields = _updateFields.Any() ? _updateFields.ToArray() : null,
                IncludeRelations = _includeRelations.Any() ? _includeRelations.ToArray() : null,
                UpdateCollections = _updateCollections.Any() ? _updateCollections.ToArray() : null,
                IncludeForResponse = _includeForResponse.Any() ? _includeForResponse.ToArray() : null,
                WhereClause = _whereClause
            };
        }

        private Expression<Func<T, object>> ConvertToObjectExpression<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            return Expression.Lambda<Func<T, object>>(
                Expression.Convert(expression.Body, typeof(object)),
                expression.Parameters);
        }
    }
}