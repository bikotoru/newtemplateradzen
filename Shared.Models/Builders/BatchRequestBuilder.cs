using System.Linq.Expressions;
using Shared.Models.Requests;

namespace Shared.Models.Builders
{
    public class BatchRequestBuilder<T> where T : class
    {
        private readonly List<T> _entities;
        private readonly List<CreateRequest<T>> _requests = new();
        private GlobalBatchConfiguration<T>? _globalConfig;
        private bool _useTransaction = true;
        private bool _continueOnError = false;

        public BatchRequestBuilder(List<T> entities)
        {
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
        }

        /// <summary>
        /// Configuración global para todos los elementos del batch
        /// </summary>
        public BatchRequestBuilder<T> WithGlobalFields(params Expression<Func<T, object>>[] fields)
        {
            _globalConfig ??= new GlobalBatchConfiguration<T>();
            _globalConfig.CreateFields = fields;
            return this;
        }

        /// <summary>
        /// Incluir relaciones globalmente
        /// </summary>
        public BatchRequestBuilder<T> WithGlobalIncludes<TProperty>(params Expression<Func<T, TProperty>>[] relations)
        {
            _globalConfig ??= new GlobalBatchConfiguration<T>();
            _globalConfig.IncludeRelations = relations.Cast<Expression<Func<T, object>>>().ToArray();
            return this;
        }

        /// <summary>
        /// Auto-crear relaciones globalmente
        /// </summary>
        public BatchRequestBuilder<T> WithGlobalAutoCreate<TProperty>(params Expression<Func<T, TProperty>>[] relations)
        {
            _globalConfig ??= new GlobalBatchConfiguration<T>();
            _globalConfig.AutoCreateRelations = relations.Cast<Expression<Func<T, object>>>().ToArray();
            return this;
        }

        /// <summary>
        /// Configurar si usar transacción
        /// </summary>
        public BatchRequestBuilder<T> WithTransaction(bool useTransaction = true)
        {
            _useTransaction = useTransaction;
            return this;
        }

        /// <summary>
        /// Continuar procesando si hay errores individuales
        /// </summary>
        public BatchRequestBuilder<T> ContinueOnError(bool continueOnError = true)
        {
            _continueOnError = continueOnError;
            return this;
        }

        /// <summary>
        /// Agregar configuración específica para un elemento
        /// </summary>
        public BatchRequestBuilder<T> WithSpecificConfig(int index, Action<CreateRequestBuilder<T>> configAction)
        {
            if (index < 0 || index >= _entities.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            var builder = new CreateRequestBuilder<T>(_entities[index]);
            configAction(builder);
            
            // Asegurar que tenemos suficientes requests
            while (_requests.Count <= index)
                _requests.Add(null!);
                
            _requests[index] = builder.Build();
            return this;
        }

        public CreateBatchRequest<T> Build()
        {
            // Si no hay requests específicos, crear requests por defecto para todas las entidades
            if (!_requests.Any())
            {
                foreach (var entity in _entities)
                {
                    _requests.Add(new CreateRequest<T> { Entity = entity });
                }
            }
            else
            {
                // Llenar requests faltantes con configuración por defecto
                for (int i = 0; i < _entities.Count; i++)
                {
                    if (i >= _requests.Count || _requests[i] == null)
                    {
                        if (i >= _requests.Count)
                            _requests.Add(new CreateRequest<T> { Entity = _entities[i] });
                        else
                            _requests[i] = new CreateRequest<T> { Entity = _entities[i] };
                    }
                }
            }

            return new CreateBatchRequest<T>
            {
                Requests = _requests,
                GlobalConfiguration = _globalConfig,
                UseTransaction = _useTransaction,
                ContinueOnError = _continueOnError
            };
        }
    }

    public class BatchUpdateRequestBuilder<T> where T : class
    {
        private readonly List<T> _entities;
        private readonly List<UpdateRequest<T>> _requests = new();
        private GlobalUpdateBatchConfiguration<T>? _globalConfig;
        private bool _useTransaction = true;
        private bool _continueOnError = false;

        public BatchUpdateRequestBuilder(List<T> entities)
        {
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
        }

        /// <summary>
        /// Campos a actualizar globalmente
        /// </summary>
        public BatchUpdateRequestBuilder<T> WithGlobalUpdateFields(params Expression<Func<T, object>>[] fields)
        {
            _globalConfig ??= new GlobalUpdateBatchConfiguration<T>();
            _globalConfig.UpdateFields = fields;
            return this;
        }

        /// <summary>
        /// Where clause global
        /// </summary>
        public BatchUpdateRequestBuilder<T> WithGlobalWhere(Expression<Func<T, bool>> predicate)
        {
            _globalConfig ??= new GlobalUpdateBatchConfiguration<T>();
            _globalConfig.WhereClause = predicate;
            return this;
        }

        /// <summary>
        /// Configurar si usar transacción
        /// </summary>
        public BatchUpdateRequestBuilder<T> WithTransaction(bool useTransaction = true)
        {
            _useTransaction = useTransaction;
            return this;
        }

        /// <summary>
        /// Continuar procesando si hay errores individuales
        /// </summary>
        public BatchUpdateRequestBuilder<T> ContinueOnError(bool continueOnError = true)
        {
            _continueOnError = continueOnError;
            return this;
        }

        public UpdateBatchRequest<T> Build()
        {
            // Crear requests por defecto para todas las entidades si no hay específicos
            if (!_requests.Any())
            {
                foreach (var entity in _entities)
                {
                    _requests.Add(new UpdateRequest<T> { Entity = entity });
                }
            }

            return new UpdateBatchRequest<T>
            {
                Requests = _requests,
                GlobalConfiguration = _globalConfig,
                UseTransaction = _useTransaction,
                ContinueOnError = _continueOnError
            };
        }
    }
}