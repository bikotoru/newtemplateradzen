using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Models.Requests;
using Shared.Models.Responses;
using Shared.Models.Extensions;
using Shared.Models.QueryModels;
using System.Reflection;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core;

namespace Backend.Utils.Services
{
    public class BaseQueryService<T> where T : class
    {
        protected readonly DbContext _context;
        protected readonly ILogger<BaseQueryService<T>> _logger;
        protected readonly DbSet<T> _dbSet;

        public BaseQueryService(DbContext context, ILogger<BaseQueryService<T>> logger)
        {
            _context = context;
            _logger = logger;
            _dbSet = _context.Set<T>();
        }

        #region Individual Operations

        /// <summary>
        /// Crear una entidad individual
        /// </summary>
        public virtual async Task<T> CreateAsync(CreateRequest<T> request)
        {
            _logger.LogInformation($"Creating {typeof(T).Name}");

            try
            {
                // Validar FK automáticamente
                await ValidateForeignKeysAsync(request.Entity);

                // Procesar campos específicos si están definidos
                var entityToCreate = ProcessCreateFields(request);

                // Procesar relaciones automáticas
                await ProcessAutoCreateRelationsAsync(request);

                // Agregar al contexto
                var entry = await _dbSet.AddAsync(entityToCreate);
                
                // Guardar cambios
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Created {typeof(T).Name} with ID: {GetEntityId(entry.Entity)}");
                
                return entry.Entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating {typeof(T).Name}");
                throw;
            }
        }

        /// <summary>
        /// Actualizar una entidad individual
        /// </summary>
        public virtual async Task<T> UpdateAsync(UpdateRequest<T> request)
        {
            _logger.LogInformation($"Updating {typeof(T).Name}");

            try
            {
                // Validar FK automáticamente
                await ValidateForeignKeysAsync(request.Entity);

                // Procesar campos específicos si están definidos
                var entityToUpdate = ProcessUpdateFields(request);

                // Marcar como modificado
                _context.Entry(entityToUpdate).State = EntityState.Modified;

                // Aplicar where clause si existe
                if (request.WhereClause != null)
                {
                    // Verificar que la entidad cumple la condición
                    var compiledWhere = request.WhereClause.Compile();
                    if (!compiledWhere(entityToUpdate))
                    {
                        throw new InvalidOperationException("Entity doesn't match the WHERE clause condition");
                    }
                }

                // Guardar cambios
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Updated {typeof(T).Name} with ID: {GetEntityId(entityToUpdate)}");
                
                return entityToUpdate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating {typeof(T).Name}");
                throw;
            }
        }

        /// <summary>
        /// Obtener todos los registros con paginación
        /// </summary>
        public virtual async Task<PagedResponse<T>> GetAllPagedAsync(int page = 1, int pageSize = 10)
        {
            _logger.LogInformation($"Getting paged {typeof(T).Name} - Page: {page}, PageSize: {pageSize}");

            try
            {
                var query = _dbSet.AsQueryable();
                
                var totalCount = await query.CountAsync();
                var data = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return PagedResponse<T>.Create(data, page, pageSize, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting paged {typeof(T).Name}");
                throw;
            }
        }

        /// <summary>
        /// Obtener todos los registros sin paginación (solo si se requiere explícitamente)
        /// </summary>
        public virtual async Task<List<T>> GetAllUnpagedAsync()
        {
            _logger.LogInformation($"Getting all {typeof(T).Name} (unpaged)");

            try
            {
                return await _dbSet.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting all {typeof(T).Name}");
                throw;
            }
        }

        /// <summary>
        /// Obtener por ID
        /// </summary>
        public virtual async Task<T?> GetByIdAsync(Guid id)
        {
            _logger.LogInformation($"Getting {typeof(T).Name} by ID: {id}");

            try
            {
                return await _dbSet.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting {typeof(T).Name} by ID: {id}");
                throw;
            }
        }

        /// <summary>
        /// Eliminar por ID
        /// </summary>
        public virtual async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation($"Deleting {typeof(T).Name} with ID: {id}");

            try
            {
                var entity = await _dbSet.FindAsync(id);
                if (entity == null)
                {
                    _logger.LogWarning($"{typeof(T).Name} with ID {id} not found for deletion");
                    return false;
                }

                _dbSet.Remove(entity);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Deleted {typeof(T).Name} with ID: {id}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting {typeof(T).Name} with ID: {id}");
                throw;
            }
        }

        #endregion

        #region Batch Operations

        /// <summary>
        /// Crear múltiples entidades
        /// </summary>
        public virtual async Task<BatchResponse<T>> CreateBatchAsync(CreateBatchRequest<T> batchRequest)
        {
            _logger.LogInformation($"Creating batch of {batchRequest.Requests.Count} {typeof(T).Name}");

            var successfulItems = new List<T>();
            var failedItems = new List<BatchError>();

            if (batchRequest.UseTransaction)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await ProcessBatchCreate(batchRequest, successfulItems, failedItems);
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, $"Error in batch create transaction for {typeof(T).Name}");
                    throw;
                }
            }
            else
            {
                await ProcessBatchCreate(batchRequest, successfulItems, failedItems);
            }

            return BatchResponse<T>.Create(successfulItems, failedItems);
        }

        /// <summary>
        /// Actualizar múltiples entidades
        /// </summary>
        public virtual async Task<BatchResponse<T>> UpdateBatchAsync(UpdateBatchRequest<T> batchRequest)
        {
            _logger.LogInformation($"Updating batch of {batchRequest.Requests.Count} {typeof(T).Name}");

            var successfulItems = new List<T>();
            var failedItems = new List<BatchError>();

            if (batchRequest.UseTransaction)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await ProcessBatchUpdate(batchRequest, successfulItems, failedItems);
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, $"Error in batch update transaction for {typeof(T).Name}");
                    throw;
                }
            }
            else
            {
                await ProcessBatchUpdate(batchRequest, successfulItems, failedItems);
            }

            return BatchResponse<T>.Create(successfulItems, failedItems);
        }

        #endregion

        #region Private Helper Methods

        private async Task ProcessBatchCreate(CreateBatchRequest<T> batchRequest, 
            List<T> successfulItems, List<BatchError> failedItems)
        {
            for (int i = 0; i < batchRequest.Requests.Count; i++)
            {
                try
                {
                    var request = ApplyGlobalConfigToCreate(batchRequest.Requests[i], batchRequest.GlobalConfiguration);
                    var result = await CreateAsync(request);
                    successfulItems.Add(result);
                }
                catch (Exception ex)
                {
                    failedItems.Add(new BatchError
                    {
                        Index = i,
                        Error = ex.Message,
                        Item = batchRequest.Requests[i].Entity
                    });

                    if (!batchRequest.ContinueOnError)
                    {
                        throw new InvalidOperationException($"Batch create failed at index {i}: {ex.Message}", ex);
                    }
                }
            }
        }

        private async Task ProcessBatchUpdate(UpdateBatchRequest<T> batchRequest,
            List<T> successfulItems, List<BatchError> failedItems)
        {
            for (int i = 0; i < batchRequest.Requests.Count; i++)
            {
                try
                {
                    var request = ApplyGlobalConfigToUpdate(batchRequest.Requests[i], batchRequest.GlobalConfiguration);
                    var result = await UpdateAsync(request);
                    successfulItems.Add(result);
                }
                catch (Exception ex)
                {
                    failedItems.Add(new BatchError
                    {
                        Index = i,
                        Error = ex.Message,
                        Item = batchRequest.Requests[i].Entity
                    });

                    if (!batchRequest.ContinueOnError)
                    {
                        throw new InvalidOperationException($"Batch update failed at index {i}: {ex.Message}", ex);
                    }
                }
            }
        }

        private CreateRequest<T> ApplyGlobalConfigToCreate(CreateRequest<T> request, GlobalBatchConfiguration<T>? globalConfig)
        {
            if (globalConfig == null) return request;

            return new CreateRequest<T>
            {
                Entity = request.Entity,
                CreateFields = request.CreateFields ?? globalConfig.CreateFields,
                IncludeRelations = request.IncludeRelations ?? globalConfig.IncludeRelations,
                IncludeCollections = request.IncludeCollections ?? globalConfig.IncludeCollections,
                AutoCreateRelations = request.AutoCreateRelations ?? globalConfig.AutoCreateRelations,
                AutoCreateCollections = request.AutoCreateCollections ?? globalConfig.AutoCreateCollections
            };
        }

        private UpdateRequest<T> ApplyGlobalConfigToUpdate(UpdateRequest<T> request, GlobalUpdateBatchConfiguration<T>? globalConfig)
        {
            if (globalConfig == null) return request;

            return new UpdateRequest<T>
            {
                Entity = request.Entity,
                UpdateFields = request.UpdateFields ?? globalConfig.UpdateFields,
                IncludeRelations = request.IncludeRelations ?? globalConfig.IncludeRelations,
                UpdateCollections = request.UpdateCollections ?? globalConfig.UpdateCollections,
                WhereClause = request.WhereClause ?? globalConfig.WhereClause,
                IncludeForResponse = request.IncludeForResponse ?? globalConfig.IncludeForResponse
            };
        }

        private T ProcessCreateFields(CreateRequest<T> request)
        {
            if (request.CreateFields == null || !request.CreateFields.Any())
            {
                return request.Entity; // Usar todos los campos
            }

            // Si hay campos específicos, crear una nueva instancia solo con esos campos
            var newEntity = Activator.CreateInstance<T>();
            var propertyNames = request.CreateFields.GetPropertyNames();
            
            foreach (var propertyName in propertyNames)
            {
                var property = typeof(T).GetProperty(propertyName);
                if (property != null && property.CanWrite)
                {
                    var value = property.GetValue(request.Entity);
                    property.SetValue(newEntity, value);
                }
            }

            return newEntity;
        }

        private T ProcessUpdateFields(UpdateRequest<T> request)
        {
            if (request.UpdateFields == null || !request.UpdateFields.Any())
            {
                return request.Entity; // Actualizar todos los campos
            }

            // Si hay campos específicos, solo actualizar esos campos
            var existingEntity = _context.Entry(request.Entity).Entity;
            var propertyNames = request.UpdateFields.GetPropertyNames();
            
            foreach (var propertyName in propertyNames)
            {
                var property = typeof(T).GetProperty(propertyName);
                if (property != null && property.CanWrite)
                {
                    var value = property.GetValue(request.Entity);
                    property.SetValue(existingEntity, value);
                }
            }

            return existingEntity;
        }

        private async Task ValidateForeignKeysAsync(T entity)
        {
            // Obtener todas las propiedades de navegación
            var entityType = _context.Model.FindEntityType(typeof(T));
            if (entityType == null) return;

            var foreignKeys = entityType.GetForeignKeys();
            
            foreach (var foreignKey in foreignKeys)
            {
                var fkProperty = foreignKey.Properties.FirstOrDefault();
                if (fkProperty == null) continue;

                var propertyInfo = typeof(T).GetProperty(fkProperty.Name);
                if (propertyInfo == null) continue;

                var fkValue = propertyInfo.GetValue(entity);
                if (fkValue == null || fkValue.Equals(Guid.Empty)) continue;

                // Verificar que existe la entidad referenciada
                var principalEntityType = foreignKey.PrincipalEntityType;
                var principalDbSet = (IQueryable<object>)typeof(DbContext)
                    .GetMethod("Set", Type.EmptyTypes)!
                    .MakeGenericMethod(principalEntityType.ClrType)
                    .Invoke(_context, null)!;
                
                var exists = await principalDbSet
                    .Cast<object>()
                    .AnyAsync(e => EF.Property<Guid>(e, "Id") == (Guid)fkValue);

                if (!exists)
                {
                    throw new InvalidOperationException(
                        $"Referenced entity {principalEntityType.ClrType.Name} with ID {fkValue} does not exist");
                }
            }
        }

        private async Task ProcessAutoCreateRelationsAsync(CreateRequest<T> request)
        {
            if (request.AutoCreateRelations == null || !request.AutoCreateRelations.Any())
                return;

            // Implementar lógica de auto-creación de relaciones
            // Por ahora placeholder - se puede extender según necesidades específicas
            await Task.CompletedTask;
        }

        private object GetEntityId(T entity)
        {
            var idProperty = typeof(T).GetProperty("Id");
            return idProperty?.GetValue(entity) ?? "Unknown";
        }

        #endregion

        #region Query Operations (Dynamic Queries)

        /// <summary>
        /// Ejecutar query dinámica
        /// </summary>
        public virtual async Task<List<T>> QueryAsync(QueryRequest queryRequest)
        {
            _logger.LogInformation($"Executing query for {typeof(T).Name}");

            try
            {
                var query = BuildQuery(queryRequest);
                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing query for {typeof(T).Name}");
                throw;
            }
        }

        /// <summary>
        /// Ejecutar query dinámica con paginación
        /// </summary>
        public virtual async Task<Shared.Models.QueryModels.PagedResult<T>> QueryPagedAsync(QueryRequest queryRequest)
        {
            _logger.LogInformation($"Executing paged query for {typeof(T).Name}");

            try
            {
                var baseQuery = BuildQuery(queryRequest, skipPagination: true);
                
                // Contar total sin paginación
                var totalCount = await baseQuery.CountAsync();
                
                // Aplicar paginación
                var query = baseQuery;
                if (queryRequest.Skip.HasValue)
                    query = query.Skip(queryRequest.Skip.Value);
                if (queryRequest.Take.HasValue)
                    query = query.Take(queryRequest.Take.Value);
                
                var data = await query.ToListAsync();
                
                var page = queryRequest.Skip.HasValue && queryRequest.Take.HasValue 
                    ? (queryRequest.Skip.Value / queryRequest.Take.Value) + 1 
                    : 1;
                var pageSize = queryRequest.Take ?? totalCount;

                return new Shared.Models.QueryModels.PagedResult<T>
                {
                    Data = data,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing paged query for {typeof(T).Name}");
                throw;
            }
        }

        /// <summary>
        /// Ejecutar query con select personalizado
        /// </summary>
        public virtual async Task<List<object>> QuerySelectAsync(QueryRequest queryRequest)
        {
            _logger.LogInformation($"Executing select query for {typeof(T).Name}");

            try
            {
                var query = BuildQuery(queryRequest);
                
                if (!string.IsNullOrEmpty(queryRequest.Select))
                {
                    return await query.Select(queryRequest.Select).ToDynamicListAsync<object>();
                }
                
                // Si no hay select, retornar como objetos
                var result = await query.ToListAsync();
                return result.Cast<object>().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing select query for {typeof(T).Name}");
                throw;
            }
        }

        /// <summary>
        /// Ejecutar query con select personalizado y paginación
        /// </summary>
        public virtual async Task<Shared.Models.QueryModels.PagedResult<object>> QuerySelectPagedAsync(QueryRequest queryRequest)
        {
            _logger.LogInformation($"Executing paged select query for {typeof(T).Name}");

            try
            {
                var baseQuery = BuildQuery(queryRequest, skipPagination: true);
                
                // Contar total sin paginación ni select
                var totalCount = await baseQuery.CountAsync();
                
                // Aplicar paginación
                var query = baseQuery;
                if (queryRequest.Skip.HasValue)
                    query = query.Skip(queryRequest.Skip.Value);
                if (queryRequest.Take.HasValue)
                    query = query.Take(queryRequest.Take.Value);

                List<object> data;
                if (!string.IsNullOrEmpty(queryRequest.Select))
                {
                    data = await query.Select(queryRequest.Select).ToDynamicListAsync<object>();
                }
                else
                {
                    var result = await query.ToListAsync();
                    data = result.Cast<object>().ToList();
                }
                
                var page = queryRequest.Skip.HasValue && queryRequest.Take.HasValue 
                    ? (queryRequest.Skip.Value / queryRequest.Take.Value) + 1 
                    : 1;
                var pageSize = queryRequest.Take ?? totalCount;

                return new Shared.Models.QueryModels.PagedResult<object>
                {
                    Data = data,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing paged select query for {typeof(T).Name}");
                throw;
            }
        }

        #endregion

        #region Private Query Building Methods

        private IQueryable<T> BuildQuery(QueryRequest queryRequest, bool skipPagination = false)
        {
            IQueryable<T> query = _dbSet;

            // Aplicar includes
            if (queryRequest.Include != null && queryRequest.Include.Any())
            {
                foreach (var include in queryRequest.Include)
                {
                    if (!string.IsNullOrEmpty(include))
                    {
                        query = query.Include(include);
                    }
                }
            }

            // Aplicar filtros
            if (!string.IsNullOrEmpty(queryRequest.Filter))
            {
                query = query.Where(queryRequest.Filter);
            }

            // Aplicar ordenamiento
            if (!string.IsNullOrEmpty(queryRequest.OrderBy))
            {
                query = query.OrderBy(queryRequest.OrderBy);
            }

            // Aplicar paginación solo si no se especifica saltarla
            if (!skipPagination)
            {
                if (queryRequest.Skip.HasValue)
                    query = query.Skip(queryRequest.Skip.Value);
                if (queryRequest.Take.HasValue)
                    query = query.Take(queryRequest.Take.Value);
            }

            return query;
        }

        #endregion
    }
}