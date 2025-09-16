using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Backend.Utils.Data;
using Shared.Models.DTOs.Auth;

namespace Backend.Utils.EFInterceptors.Core
{
    public class InterceptedAppDbContext : AppDbContext
    {
        private readonly EFInterceptorService? _interceptorService;
        private readonly ILogger<InterceptedAppDbContext> _logger;
        
        // Almacenar información del usuario actual
        public SessionDataDto? CurrentUser { get; set; }

        public InterceptedAppDbContext(
            DbContextOptions<AppDbContext> options, 
            EFInterceptorService? interceptorService = null,
            ILogger<InterceptedAppDbContext>? logger = null)
            : base(options)
        {
            _interceptorService = interceptorService;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Log para debugging
            _logger.LogInformation("🟡 [InterceptedAppDbContext] Constructor - Context creado con opciones");
            
            // Verificar si las opciones tienen interceptors
            var extension = options.FindExtension<Microsoft.EntityFrameworkCore.Infrastructure.CoreOptionsExtension>();
            if (extension?.Interceptors?.Any() == true)
            {
                _logger.LogInformation($"✅ [InterceptedAppDbContext] {extension.Interceptors.Count()} interceptors encontrados en opciones");
            }
            else
            {
                _logger.LogWarning("❌ [InterceptedAppDbContext] NO HAY INTERCEPTORS en las opciones del contexto!");
            }
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

            _logger.LogInformation($"[InterceptedAppDbContext] Adding entity of type {typeof(TEntity).Name}");
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

            _logger.LogInformation($"[InterceptedAppDbContext] Updating entity of type {typeof(TEntity).Name}");
            return base.Update(entity);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (_interceptorService != null)
            {
                _logger.LogInformation("📌 [InterceptedAppDbContext] Executing BeforeSave interceptors (handlers)");
                var beforeResult = await _interceptorService.HandleBeforeSaveAsync(this);
                
                if (!beforeResult)
                {
                    _logger.LogWarning("SaveChanges operation blocked by BeforeSave interceptor");
                    throw new InvalidOperationException("SaveChanges operation was blocked by a BeforeSave interceptor");
                }
            }

            _logger.LogInformation("🔵 [InterceptedAppDbContext] Llamando base.SaveChangesAsync() - Los interceptors deberían ejecutarse AQUÍ");
            var affectedRows = await base.SaveChangesAsync(cancellationToken);

            if (_interceptorService != null)
            {
                _logger.LogInformation($"📌 [InterceptedAppDbContext] Executing AfterSave interceptors (handlers) - affected rows: {affectedRows}");
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
                _logger.LogInformation("📌 [InterceptedAppDbContext] Executing BeforeSave interceptors (sync/handlers)");
                var beforeTask = _interceptorService.HandleBeforeSaveAsync(this);
                var beforeResult = beforeTask.GetAwaiter().GetResult();
                
                if (!beforeResult)
                {
                    _logger.LogWarning("SaveChanges operation blocked by BeforeSave interceptor");
                    throw new InvalidOperationException("SaveChanges operation was blocked by a BeforeSave interceptor");
                }
            }

            _logger.LogInformation("🔵 [InterceptedAppDbContext] Executing SaveChanges (sync) - Los interceptors deberían ejecutarse AQUÍ");
            var affectedRows = base.SaveChanges();

            if (_interceptorService != null)
            {
                _logger.LogInformation($"📌 [InterceptedAppDbContext] Executing AfterSave interceptors (sync/handlers) - affected rows: {affectedRows}");
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