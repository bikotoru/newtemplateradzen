using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Models.Requests;
using Shared.Models.Responses;
using Shared.Models.Extensions;
using Shared.Models.QueryModels;
using Shared.Models.DTOs.Auth;
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
        public virtual async Task<T> CreateAsync(CreateRequest<T> request, SessionDataDto sessionData)
        {
            _logger.LogInformation($"Creating {typeof(T).Name}");

            try
            {
                // Inyectar automáticamente campos de auditoría y organización
                InjectCreationFields(request.Entity, sessionData);

                // Validar FK automáticamente
                await ValidateForeignKeysAsync(request.Entity, sessionData);

                // Procesar campos específicos si están definidos
                var entityToCreate = ProcessCreateFields(request);

                // Procesar relaciones automáticas
                await ProcessAutoCreateRelationsAsync(request);

                // Agregar al contexto
                var entry = await _dbSet.AddAsync(entityToCreate);
                
                // Guardar cambios
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Created {typeof(T).Name} with ID: {GetEntityId(entry.Entity)} by user {sessionData.Id}");
                
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
        public virtual async Task<T> UpdateAsync(UpdateRequest<T> request, SessionDataDto sessionData)
        {
            _logger.LogInformation($"Updating {typeof(T).Name}");

            try
            {
                // VALIDACIÓN CRÍTICA: Verificar que la entidad pertenece a la organización del usuario
                if (!ValidateEntityBelongsToOrganization(request.Entity, sessionData))
                {
                    throw new UnauthorizedAccessException($"No tiene permisos para modificar esta entidad de otra organización");
                }

                // Inyectar automáticamente campos de modificación
                InjectUpdateFields(request.Entity, sessionData);

                // Validar FK automáticamente
                await ValidateForeignKeysAsync(request.Entity, sessionData);

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
                
                _logger.LogInformation($"Updated {typeof(T).Name} with ID: {GetEntityId(entityToUpdate)} by user {sessionData.Id}");
                
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
        public virtual async Task<PagedResponse<T>> GetAllPagedAsync(int page, int pageSize, SessionDataDto sessionData)
        {
            _logger.LogInformation($"Getting paged {typeof(T).Name} - Page: {page}, PageSize: {pageSize}");

            try
            {
                var query = ApplyOrganizationFilter(_dbSet.AsQueryable(), sessionData);
                
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
        public virtual async Task<List<T>> GetAllUnpagedAsync(SessionDataDto sessionData)
        {
            _logger.LogInformation($"Getting all {typeof(T).Name} (unpaged)");

            try
            {
                var query = ApplyOrganizationFilter(_dbSet.AsQueryable(), sessionData);
                return await query.ToListAsync();
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
        public virtual async Task<T?> GetByIdAsync(Guid id, SessionDataDto sessionData)
        {
            _logger.LogInformation($"Getting {typeof(T).Name} by ID: {id}");

            try
            {
                var query = ApplyOrganizationFilter(_dbSet.AsQueryable(), sessionData);
                return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id);
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
        public virtual async Task<bool> DeleteAsync(Guid id, SessionDataDto sessionData)
        {
            _logger.LogInformation($"Deleting {typeof(T).Name} with ID: {id}");

            try
            {
                // CRÍTICO: Solo permitir eliminar entidades de su organización
                var entity = await GetByIdAsync(id, sessionData);
                if (entity == null)
                {
                    _logger.LogWarning($"{typeof(T).Name} with ID {id} not found for deletion (or not belongs to user organization)");
                    return false;
                }

                _dbSet.Remove(entity);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Deleted {typeof(T).Name} with ID: {id} by user {sessionData.Id}");
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
        public virtual async Task<BatchResponse<T>> CreateBatchAsync(CreateBatchRequest<T> batchRequest, SessionDataDto sessionData)
        {
            _logger.LogInformation($"Creating batch of {batchRequest.Requests.Count} {typeof(T).Name}");

            var successfulItems = new List<T>();
            var failedItems = new List<BatchError>();

            if (batchRequest.UseTransaction)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await ProcessBatchCreate(batchRequest, successfulItems, failedItems, sessionData);
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
                await ProcessBatchCreate(batchRequest, successfulItems, failedItems, sessionData);
            }

            return BatchResponse<T>.Create(successfulItems, failedItems);
        }

        /// <summary>
        /// Actualizar múltiples entidades
        /// </summary>
        public virtual async Task<BatchResponse<T>> UpdateBatchAsync(UpdateBatchRequest<T> batchRequest, SessionDataDto sessionData)
        {
            _logger.LogInformation($"Updating batch of {batchRequest.Requests.Count} {typeof(T).Name}");

            var successfulItems = new List<T>();
            var failedItems = new List<BatchError>();

            if (batchRequest.UseTransaction)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await ProcessBatchUpdate(batchRequest, successfulItems, failedItems, sessionData);
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
                await ProcessBatchUpdate(batchRequest, successfulItems, failedItems, sessionData);
            }

            return BatchResponse<T>.Create(successfulItems, failedItems);
        }

        #endregion

        #region Private Helper Methods

        private async Task ProcessBatchCreate(CreateBatchRequest<T> batchRequest, 
            List<T> successfulItems, List<BatchError> failedItems, SessionDataDto sessionData)
        {
            for (int i = 0; i < batchRequest.Requests.Count; i++)
            {
                try
                {
                    var request = ApplyGlobalConfigToCreate(batchRequest.Requests[i], batchRequest.GlobalConfiguration);
                    var result = await CreateAsync(request, sessionData);
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
            List<T> successfulItems, List<BatchError> failedItems, SessionDataDto sessionData)
        {
            for (int i = 0; i < batchRequest.Requests.Count; i++)
            {
                try
                {
                    var request = ApplyGlobalConfigToUpdate(batchRequest.Requests[i], batchRequest.GlobalConfiguration);
                    var result = await UpdateAsync(request, sessionData);
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

        private async Task ValidateForeignKeysAsync(T entity, SessionDataDto sessionData)
        {
            // Obtener todas las propiedades de navegación
            var entityType = _context.Model.FindEntityType(typeof(T));
            if (entityType == null) return;

            var foreignKeys = entityType.GetForeignKeys();
            var userOrganizationId = sessionData.Organization.Id;
            
            foreach (var foreignKey in foreignKeys)
            {
                var fkProperty = foreignKey.Properties.FirstOrDefault();
                if (fkProperty == null) continue;

                var propertyInfo = typeof(T).GetProperty(fkProperty.Name);
                if (propertyInfo == null) continue;

                var fkValue = propertyInfo.GetValue(entity);
                if (fkValue == null || fkValue.Equals(Guid.Empty)) continue;

                // Verificar que existe la entidad referenciada Y que pertenece a la misma organización
                var principalEntityType = foreignKey.PrincipalEntityType;
                var principalDbSet = (IQueryable<object>)typeof(DbContext)
                    .GetMethod("Set", Type.EmptyTypes)!
                    .MakeGenericMethod(principalEntityType.ClrType)
                    .Invoke(_context, null)!;

                // Verificar si la entidad referenciada tiene OrganizationId
                var hasOrganizationId = principalEntityType.FindProperty("OrganizationId") != null;
                
                bool exists;
                if (hasOrganizationId)
                {
                    // Verificar que existe Y pertenece a la misma organización
                    exists = await principalDbSet
                        .Cast<object>()
                        .AnyAsync(e => EF.Property<Guid>(e, "Id") == (Guid)fkValue && 
                                      EF.Property<Guid?>(e, "OrganizationId") == userOrganizationId);
                }
                else
                {
                    // Si no tiene OrganizationId, solo verificar que existe (entidades globales como SystemUsers)
                    exists = await principalDbSet
                        .Cast<object>()
                        .AnyAsync(e => EF.Property<Guid>(e, "Id") == (Guid)fkValue);
                }

                if (!exists)
                {
                    var message = hasOrganizationId 
                        ? $"Referenced entity {principalEntityType.ClrType.Name} with ID {fkValue} does not exist in your organization"
                        : $"Referenced entity {principalEntityType.ClrType.Name} with ID {fkValue} does not exist";
                    
                    throw new InvalidOperationException(message);
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
        public virtual async Task<List<T>> QueryAsync(QueryRequest queryRequest, SessionDataDto sessionData)
        {
            _logger.LogInformation($"Executing query for {typeof(T).Name}");

            try
            {
                var query = BuildQuery(queryRequest, sessionData);
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
        public virtual async Task<Shared.Models.QueryModels.PagedResult<T>> QueryPagedAsync(QueryRequest queryRequest, SessionDataDto sessionData)
        {
            _logger.LogInformation($"Executing paged query for {typeof(T).Name}");

            try
            {
                var baseQuery = BuildQuery(queryRequest, sessionData, skipPagination: true);
                
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
        public virtual async Task<List<object>> QuerySelectAsync(QueryRequest queryRequest, SessionDataDto sessionData)
        {
            _logger.LogInformation($"Executing select query for {typeof(T).Name}");

            try
            {
                var query = BuildQuery(queryRequest, sessionData);
                
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
        public virtual async Task<Shared.Models.QueryModels.PagedResult<object>> QuerySelectPagedAsync(QueryRequest queryRequest, SessionDataDto sessionData)
        {
            _logger.LogInformation($"Executing paged select query for {typeof(T).Name}");

            try
            {
                var baseQuery = BuildQuery(queryRequest, sessionData, skipPagination: true);
                
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

        #region Search Operations (Intelligent Search)

        /// <summary>
        /// Búsqueda inteligente por texto en campos específicos
        /// </summary>
        public virtual async Task<List<T>> SearchAsync(SearchRequest searchRequest, SessionDataDto sessionData)
        {
            _logger.LogInformation($"Executing search for {typeof(T).Name} with term: {searchRequest.SearchTerm}");

            try
            {
                var query = BuildSearchQuery(searchRequest, sessionData);
                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing search for {typeof(T).Name}");
                throw;
            }
        }

        /// <summary>
        /// Búsqueda inteligente con paginación
        /// </summary>
        public virtual async Task<Shared.Models.QueryModels.PagedResult<T>> SearchPagedAsync(SearchRequest searchRequest, SessionDataDto sessionData)
        {
            _logger.LogInformation($"Executing paged search for {typeof(T).Name} with term: {searchRequest.SearchTerm}");

            try
            {
                var baseQuery = BuildSearchQuery(searchRequest, sessionData, skipPagination: true);
                
                // Contar total sin paginación
                var totalCount = await baseQuery.CountAsync();
                
                // Aplicar paginación
                var query = baseQuery;
                if (searchRequest.Skip.HasValue)
                    query = query.Skip(searchRequest.Skip.Value);
                if (searchRequest.Take.HasValue)
                    query = query.Take(searchRequest.Take.Value);
                
                var data = await query.ToListAsync();
                
                var page = searchRequest.Skip.HasValue && searchRequest.Take.HasValue 
                    ? (searchRequest.Skip.Value / searchRequest.Take.Value) + 1 
                    : 1;
                var pageSize = searchRequest.Take ?? totalCount;

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
                _logger.LogError(ex, $"Error executing paged search for {typeof(T).Name}");
                throw;
            }
        }

        /// <summary>
        /// Ejecutar búsqueda inteligente con Select personalizado
        /// </summary>
        public virtual async Task<List<object>> SearchSelectAsync(SearchRequest searchRequest, SessionDataDto sessionData)
        {
            try
            {
                _logger.LogInformation($"Executing search select for {typeof(T).Name} with term: {searchRequest.SearchTerm}");

                // Construir query base
                var query = BuildSearchQuery(searchRequest, sessionData);

                // Aplicar Select si está especificado en BaseQuery
                if (!string.IsNullOrWhiteSpace(searchRequest.BaseQuery?.Select))
                {
                    return await query.Select(searchRequest.BaseQuery.Select).ToDynamicListAsync<object>();
                }

                // Si no hay Select, convertir T a object
                var results = await query.ToListAsync();
                return results.Cast<object>().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing search select for {typeof(T).Name}");
                throw;
            }
        }

        /// <summary>
        /// Ejecutar búsqueda inteligente con Select personalizado y paginación
        /// </summary>
        public virtual async Task<Shared.Models.QueryModels.PagedResult<object>> SearchSelectPagedAsync(SearchRequest searchRequest, SessionDataDto sessionData)
        {
            try
            {
                _logger.LogInformation($"Executing paged search select for {typeof(T).Name} with term: {searchRequest.SearchTerm}");

                // Construir query base
                var query = BuildSearchQuery(searchRequest, sessionData);

                // Obtener total count antes de aplicar paginación
                int totalCount = await query.CountAsync();

                // Aplicar paginación si se especifica
                if (searchRequest.Skip.HasValue && searchRequest.Skip.Value > 0)
                {
                    query = query.Skip(searchRequest.Skip.Value);
                }

                if (searchRequest.Take.HasValue && searchRequest.Take.Value > 0)
                {
                    query = query.Take(searchRequest.Take.Value);
                }

                List<object> data;

                // Aplicar Select si está especificado en BaseQuery
                if (!string.IsNullOrWhiteSpace(searchRequest.BaseQuery?.Select))
                {
                    data = await query.Select(searchRequest.BaseQuery.Select).ToDynamicListAsync<object>();
                }
                else
                {
                    // Si no hay Select, convertir T a object
                    var results = await query.ToListAsync();
                    data = results.Cast<object>().ToList();
                }

                // Calcular información de paginación
                var page = searchRequest.Skip.HasValue && searchRequest.Take.HasValue && searchRequest.Take.Value > 0
                    ? (searchRequest.Skip.Value / searchRequest.Take.Value) + 1
                    : 1;
                var pageSize = searchRequest.Take ?? totalCount;

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
                _logger.LogError(ex, $"Error executing paged search select for {typeof(T).Name}");
                throw;
            }
        }

        #endregion

        #region Private Query Building Methods

        private IQueryable<T> BuildQuery(QueryRequest queryRequest, SessionDataDto sessionData, bool skipPagination = false)
        {
            IQueryable<T> query = _dbSet;

            // CRÍTICO: Aplicar filtro de organización PRIMERO (antes de cualquier otra cosa)
            query = ApplyOrganizationFilter(query, sessionData);

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

        private IQueryable<T> BuildSearchQuery(SearchRequest searchRequest, SessionDataDto sessionData, bool skipPagination = false)
        {
            IQueryable<T> query = _dbSet;

            // CRÍTICO: Aplicar filtro de organización PRIMERO (antes de cualquier otra cosa)
            query = ApplyOrganizationFilter(query, sessionData);

            // 1. Aplicar el BaseQuery completo si existe (filtros, includes, ordenamiento base)
            if (searchRequest.BaseQuery != null)
            {
                // Aplicar includes del BaseQuery
                if (searchRequest.BaseQuery.Include != null && searchRequest.BaseQuery.Include.Any())
                {
                    foreach (var include in searchRequest.BaseQuery.Include)
                    {
                        if (!string.IsNullOrEmpty(include))
                        {
                            query = query.Include(include);
                        }
                    }
                }

                // Aplicar filtros del BaseQuery
                if (!string.IsNullOrEmpty(searchRequest.BaseQuery.Filter))
                {
                    query = query.Where(searchRequest.BaseQuery.Filter);
                }

                // Aplicar ordenamiento del BaseQuery (puede ser sobrescrito más adelante)
                if (!string.IsNullOrEmpty(searchRequest.BaseQuery.OrderBy))
                {
                    query = query.OrderBy(searchRequest.BaseQuery.OrderBy);
                }
            }

            // 2. Aplicar includes adicionales del SearchRequest (se combinan con los del BaseQuery)
            if (searchRequest.Include != null && searchRequest.Include.Any())
            {
                foreach (var include in searchRequest.Include)
                {
                    if (!string.IsNullOrEmpty(include))
                    {
                        query = query.Include(include);
                    }
                }
            }

            // 3. Construir y aplicar la búsqueda inteligente como condición ADICIONAL
            if (!string.IsNullOrWhiteSpace(searchRequest.SearchTerm))
            {
                var searchConditions = BuildSearchConditions(searchRequest.SearchTerm, searchRequest.SearchFields);
                if (!string.IsNullOrEmpty(searchConditions))
                {
                    // La búsqueda se combina con AND al BaseQuery existente
                    query = query.Where(searchConditions, searchRequest.SearchTerm.ToLower());
                }
            }

            // 4. Aplicar ordenamiento específico del SearchRequest (sobrescribe el del BaseQuery)
            if (!string.IsNullOrEmpty(searchRequest.OrderBy))
            {
                query = query.OrderBy(searchRequest.OrderBy);
            }
            else if (searchRequest.BaseQuery == null || string.IsNullOrEmpty(searchRequest.BaseQuery.OrderBy))
            {
                // Solo aplicar ordenamiento por defecto si no hay ninguno
                var firstProperty = typeof(T).GetProperties().FirstOrDefault();
                if (firstProperty != null)
                {
                    query = query.OrderBy(firstProperty.Name);
                }
            }

            // 5. Aplicar paginación del SearchRequest (sobrescribe la del BaseQuery)
            if (!skipPagination)
            {
                if (searchRequest.Skip.HasValue)
                    query = query.Skip(searchRequest.Skip.Value);
                else if (searchRequest.BaseQuery?.Skip.HasValue == true)
                    query = query.Skip(searchRequest.BaseQuery.Skip.Value);

                if (searchRequest.Take.HasValue)
                    query = query.Take(searchRequest.Take.Value);
                else if (searchRequest.BaseQuery?.Take.HasValue == true)
                    query = query.Take(searchRequest.BaseQuery.Take.Value);
            }

            return query;
        }

        private string BuildSearchConditions(string searchTerm, string[] searchFields)
        {
            var conditions = new List<string>();
            var properties = typeof(T).GetProperties();

            // Si no se especifican campos, usar campos de texto por defecto
            var fieldsToSearch = searchFields?.Any() == true ? searchFields : GetDefaultSearchFields(properties);

            // Escapar caracteres especiales en el término de búsqueda
            var escapedSearchTerm = searchTerm.Replace("\"", "\\\"").Replace("'", "\\'");

            foreach (var fieldName in fieldsToSearch)
            {
                var property = properties.FirstOrDefault(p => p.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
                if (property == null) continue;

                // Determinar el tipo de búsqueda según el tipo de propiedad
                if (property.PropertyType == typeof(string))
                {
                    // Para strings: contains case-insensitive con null check - usando @0 parameter
                    conditions.Add($"({property.Name} != null && {property.Name}.ToLower().Contains(@0))");
                }
                else if (IsNumericType(property.PropertyType))
                {
                    // Para números: convertir a string y buscar parcialmente
                    if (decimal.TryParse(searchTerm, out _))
                    {
                        conditions.Add($"{property.Name}.ToString().Contains(@0)");
                    }
                }
                else if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
                {
                    // Para fechas: buscar por string representation para mayor flexibilidad
                    conditions.Add($"({property.Name} != null && {property.Name}.ToString().Contains(@0))");
                }
            }

            return conditions.Any() ? $"({string.Join(" || ", conditions)})" : string.Empty;
        }

        private string[] GetDefaultSearchFields(PropertyInfo[] properties)
        {
            // Buscar campos por defecto: strings y campos que contengan "nombre", "descripcion", "codigo", etc.
            var defaultFields = new List<string>();

            foreach (var prop in properties)
            {
                var propName = prop.Name.ToLower();
                
                if (prop.PropertyType == typeof(string) && (
                    propName.Contains("nombre") ||
                    propName.Contains("name") ||
                    propName.Contains("descripcion") ||
                    propName.Contains("description") ||
                    propName.Contains("codigo") ||
                    propName.Contains("code") ||
                    propName.Contains("titulo") ||
                    propName.Contains("title")))
                {
                    defaultFields.Add(prop.Name);
                }
                else if (IsNumericType(prop.PropertyType) && (
                    propName.Contains("folio") ||
                    propName.Contains("numero") ||
                    propName.Contains("number") ||
                    propName.Contains("id")))
                {
                    defaultFields.Add(prop.Name);
                }
            }

            // Si no encuentra campos específicos, usar todos los strings
            if (!defaultFields.Any())
            {
                defaultFields.AddRange(properties
                    .Where(p => p.PropertyType == typeof(string))
                    .Select(p => p.Name));
            }

            return defaultFields.ToArray();
        }

        private bool IsNumericType(Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
            
            return underlyingType == typeof(int) ||
                   underlyingType == typeof(long) ||
                   underlyingType == typeof(decimal) ||
                   underlyingType == typeof(double) ||
                   underlyingType == typeof(float) ||
                   underlyingType == typeof(short) ||
                   underlyingType == typeof(byte);
        }

        #endregion

        #region Multi-Tenancy and Audit Methods

        /// <summary>
        /// Aplica filtro automático de OrganizationId a todas las consultas
        /// </summary>
        private IQueryable<T> ApplyOrganizationFilter(IQueryable<T> query, SessionDataDto sessionData)
        {
            var entityType = typeof(T);
            var organizationIdProperty = entityType.GetProperty("OrganizationId");
            
            if (organizationIdProperty != null)
            {
                var userOrganizationId = sessionData.Organization.Id;
                query = query.Where(e => EF.Property<Guid?>(e, "OrganizationId") == userOrganizationId);
                
                _logger.LogDebug("Aplicado filtro de organización {OrgId} para {EntityType}", userOrganizationId, typeof(T).Name);
            }
            
            return query;
        }

        /// <summary>
        /// Inyecta automáticamente campos de creación y organización
        /// </summary>
        private void InjectCreationFields(T entity, SessionDataDto sessionData)
        {
            var now = DateTime.UtcNow;
            var userId = sessionData.Id;
            var organizationId = sessionData.Organization.Id;

            var entityType = typeof(T);
            
            SetPropertyIfExists(entityType, entity, "Id", Guid.NewGuid());
            SetPropertyIfExists(entityType, entity, "OrganizationId", organizationId);
            SetPropertyIfExists(entityType, entity, "CreadorId", userId);
            SetPropertyIfExists(entityType, entity, "ModificadorId", userId);
            SetPropertyIfExists(entityType, entity, "FechaCreacion", now);
            SetPropertyIfExists(entityType, entity, "FechaModificacion", now);
            SetPropertyIfExists(entityType, entity, "Active", true);

            _logger.LogDebug("Inyectados campos de creación para {EntityType} - Usuario: {UserId}, Organización: {OrgId}", 
                typeof(T).Name, userId, organizationId);
        }

        /// <summary>
        /// Inyecta automáticamente campos de modificación
        /// </summary>
        private void InjectUpdateFields(T entity, SessionDataDto sessionData)
        {
            var now = DateTime.UtcNow;
            var userId = sessionData.Id;

            var entityType = typeof(T);
            
            SetPropertyIfExists(entityType, entity, "ModificadorId", userId);
            SetPropertyIfExists(entityType, entity, "FechaModificacion", now);

            _logger.LogDebug("Inyectados campos de modificación para {EntityType} - Usuario: {UserId}", 
                typeof(T).Name, userId);
        }

        /// <summary>
        /// Valida que la entidad pertenezca a la organización del usuario
        /// </summary>
        private bool ValidateEntityBelongsToOrganization(T entity, SessionDataDto sessionData)
        {
            var entityType = typeof(T);
            var organizationIdProperty = entityType.GetProperty("OrganizationId");
            
            if (organizationIdProperty != null)
            {
                var entityOrgId = organizationIdProperty.GetValue(entity) as Guid?;
                var userOrgId = sessionData.Organization.Id;
                
                var belongs = entityOrgId == userOrgId;
                
                if (!belongs)
                {
                    _logger.LogWarning("Intento de acceso cruzado de organización detectado - Usuario {UserId} (Org: {UserOrgId}) intentando acceder a entidad con OrganizationId: {EntityOrgId}", 
                        sessionData.Id, userOrgId, entityOrgId);
                }
                
                return belongs;
            }
            
            return true; // Si no tiene OrganizationId, permitir (para compatibilidad)
        }

        /// <summary>
        /// Establece una propiedad si existe en la entidad usando reflexión
        /// </summary>
        private void SetPropertyIfExists(Type entityType, T entity, string propertyName, object value)
        {
            var property = entityType.GetProperty(propertyName);
            if (property != null && property.CanWrite)
            {
                // Manejar tipos nullable
                if (property.PropertyType.IsGenericType && 
                    property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    if (value != null)
                    {
                        property.SetValue(entity, value);
                    }
                }
                else
                {
                    property.SetValue(entity, value);
                }
            }
        }

        #endregion
    }
}