using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Backend.Utils.Data;

namespace Backend.Utils.EFInterceptors.Core
{
    public class InterceptedAppDbContext : AppDbContext
    {
        private readonly EFInterceptorService? _interceptorService;
        private readonly ILogger<InterceptedAppDbContext> _logger;

        public InterceptedAppDbContext(
            DbContextOptions<AppDbContext> options, 
            EFInterceptorService? interceptorService = null,
            ILogger<InterceptedAppDbContext>? logger = null)
            : base(options)
        {
            _interceptorService = interceptorService;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<TEntity> Add<TEntity>(TEntity entity)
        {
            if (_interceptorService != null)
            {
                var task = _interceptorService.HandleAddAsync(this, entity);
                var result = task.GetAwaiter().GetResult();
                
                if (!result)
                {
                    _logger.LogWarning($"Add operation blocked by interceptor for entity {typeof(TEntity).Name}");
                    throw new InvalidOperationException($"Add operation was blocked by an interceptor for entity type {typeof(TEntity).Name}");
                }
            }

            _logger.LogDebug($"Adding entity of type {typeof(TEntity).Name}");
            return base.Add(entity);
        }

        public override Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<TEntity> Update<TEntity>(TEntity entity)
        {
            if (_interceptorService != null)
            {
                var originalEntity = Entry(entity).OriginalValues.ToObject() as TEntity ?? entity;
                var task = _interceptorService.HandleUpdateAsync(this, entity, originalEntity);
                var result = task.GetAwaiter().GetResult();
                
                if (!result)
                {
                    _logger.LogWarning($"Update operation blocked by interceptor for entity {typeof(TEntity).Name}");
                    throw new InvalidOperationException($"Update operation was blocked by an interceptor for entity type {typeof(TEntity).Name}");
                }
            }

            _logger.LogDebug($"Updating entity of type {typeof(TEntity).Name}");
            return base.Update(entity);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (_interceptorService != null)
            {
                _logger.LogDebug("Executing BeforeSave interceptors");
                var beforeResult = await _interceptorService.HandleBeforeSaveAsync(this);
                
                if (!beforeResult)
                {
                    _logger.LogWarning("SaveChanges operation blocked by BeforeSave interceptor");
                    throw new InvalidOperationException("SaveChanges operation was blocked by a BeforeSave interceptor");
                }
            }

            _logger.LogDebug("Executing SaveChangesAsync");
            var affectedRows = await base.SaveChangesAsync(cancellationToken);

            if (_interceptorService != null)
            {
                _logger.LogDebug($"Executing AfterSave interceptors (affected rows: {affectedRows})");
                var afterResult = await _interceptorService.HandleAfterSaveAsync(this, affectedRows);
                
                if (!afterResult)
                {
                    _logger.LogWarning("AfterSave interceptor returned false, but changes have already been committed");
                }
            }

            return affectedRows;
        }

        public override int SaveChanges()
        {
            if (_interceptorService != null)
            {
                _logger.LogDebug("Executing BeforeSave interceptors (sync)");
                var beforeTask = _interceptorService.HandleBeforeSaveAsync(this);
                var beforeResult = beforeTask.GetAwaiter().GetResult();
                
                if (!beforeResult)
                {
                    _logger.LogWarning("SaveChanges operation blocked by BeforeSave interceptor");
                    throw new InvalidOperationException("SaveChanges operation was blocked by a BeforeSave interceptor");
                }
            }

            _logger.LogDebug("Executing SaveChanges (sync)");
            var affectedRows = base.SaveChanges();

            if (_interceptorService != null)
            {
                _logger.LogDebug($"Executing AfterSave interceptors (sync, affected rows: {affectedRows})");
                var afterTask = _interceptorService.HandleAfterSaveAsync(this, affectedRows);
                var afterResult = afterTask.GetAwaiter().GetResult();
                
                if (!afterResult)
                {
                    _logger.LogWarning("AfterSave interceptor returned false, but changes have already been committed");
                }
            }

            return affectedRows;
        }
    }
}