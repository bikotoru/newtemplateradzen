using System.Linq.Expressions;
using Shared.Models.Requests;

namespace Shared.Models.Builders
{
    public class CreateRequestBuilder<T> where T : class
    {
        private readonly T _entity;
        private readonly List<Expression<Func<T, object>>> _createFields = new();
        private readonly List<Expression<Func<T, object>>> _includeRelations = new();
        private readonly List<Expression<Func<T, object>>> _includeCollections = new();
        private readonly List<Expression<Func<T, object>>> _autoCreateRelations = new();
        private readonly List<Expression<Func<T, object>>> _autoCreateCollections = new();

        public CreateRequestBuilder(T entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        /// <summary>
        /// Seleccionar campos específicos a crear (fuertemente tipado)
        /// </summary>
        public CreateRequestBuilder<T> WithFields(params Expression<Func<T, object>>[] fields)
        {
            _createFields.AddRange(fields);
            return this;
        }

        /// <summary>
        /// Incluir relaciones 1:1 / 1:N (fuertemente tipado)
        /// </summary>
        public CreateRequestBuilder<T> Including<TProperty>(Expression<Func<T, TProperty>> relation)
        {
            _includeRelations.Add(ConvertToObjectExpression(relation));
            return this;
        }

        /// <summary>
        /// Incluir colecciones N:N (fuertemente tipado)
        /// </summary>
        public CreateRequestBuilder<T> IncludingCollection<TCollection>(
            Expression<Func<T, ICollection<TCollection>>> collection)
        {
            _includeCollections.Add(ConvertToObjectExpression(collection));
            return this;
        }

        /// <summary>
        /// Auto-crear relaciones faltantes (fuertemente tipado)
        /// </summary>
        public CreateRequestBuilder<T> WithAutoCreate<TProperty>(
            Expression<Func<T, TProperty>> relation) where TProperty : class
        {
            _autoCreateRelations.Add(ConvertToObjectExpression(relation));
            return this;
        }

        /// <summary>
        /// Auto-crear elementos en colecciones (fuertemente tipado)
        /// </summary>
        public CreateRequestBuilder<T> WithAutoCreateCollection<TCollection>(
            Expression<Func<T, ICollection<TCollection>>> collection) where TCollection : class
        {
            _autoCreateCollections.Add(ConvertToObjectExpression(collection));
            return this;
        }

        public CreateRequest<T> Build()
        {
            return new CreateRequest<T>
            {
                Entity = _entity,
                CreateFields = _createFields.Any() ? _createFields.ToArray() : null,
                IncludeRelations = _includeRelations.Any() ? _includeRelations.ToArray() : null,
                IncludeCollections = _includeCollections.Any() ? _includeCollections.ToArray() : null,
                AutoCreateRelations = _autoCreateRelations.Any() ? _autoCreateRelations.ToArray() : null,
                AutoCreateCollections = _autoCreateCollections.Any() ? _autoCreateCollections.ToArray() : null
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