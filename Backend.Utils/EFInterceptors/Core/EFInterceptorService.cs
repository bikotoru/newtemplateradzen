using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Backend.Utils.EFInterceptors.Core
{
    public class EFInterceptorService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EFInterceptorService> _logger;
        private readonly List<AddHandler> _addHandlers;
        private readonly List<UpdateHandler> _updateHandlers;
        private readonly List<SaveHandler> _saveHandlers;

        public EFInterceptorService(IServiceProvider serviceProvider, ILogger<EFInterceptorService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _addHandlers = new List<AddHandler>();
            _updateHandlers = new List<UpdateHandler>();
            _saveHandlers = new List<SaveHandler>();
            
            LoadHandlers();
        }

        private void LoadHandlers()
        {
            var addHandlers = _serviceProvider.GetServices<AddHandler>();
            var updateHandlers = _serviceProvider.GetServices<UpdateHandler>();
            var saveHandlers = _serviceProvider.GetServices<SaveHandler>();

            _addHandlers.AddRange(addHandlers);
            _updateHandlers.AddRange(updateHandlers);
            _saveHandlers.AddRange(saveHandlers);

            _logger.LogInformation($"Loaded {_addHandlers.Count} AddHandlers, {_updateHandlers.Count} UpdateHandlers, {_saveHandlers.Count} SaveHandlers");
        }

        public async Task<bool> HandleAddAsync<T>(DbContext context, T entity) where T : class
        {
            _logger.LogDebug($"Processing Add for entity type: {typeof(T).Name}");
            
            foreach (var handler in _addHandlers)
            {
                if (await handler.CanHandleAsync(entity))
                {
                    var result = await handler.HandleAddAsync(context, entity);
                    if (!result)
                    {
                        _logger.LogWarning($"AddHandler {handler.GetType().Name} returned false for entity {typeof(T).Name}");
                        return false;
                    }
                }
            }
            return true;
        }

        public async Task<bool> HandleUpdateAsync<T>(DbContext context, T entity, T originalEntity) where T : class
        {
            _logger.LogDebug($"Processing Update for entity type: {typeof(T).Name}");
            
            foreach (var handler in _updateHandlers)
            {
                if (await handler.CanHandleAsync(entity))
                {
                    var result = await handler.HandleUpdateAsync(context, entity, originalEntity);
                    if (!result)
                    {
                        _logger.LogWarning($"UpdateHandler {handler.GetType().Name} returned false for entity {typeof(T).Name}");
                        return false;
                    }
                }
            }
            return true;
        }

        public async Task<bool> HandleBeforeSaveAsync(DbContext context)
        {
            _logger.LogDebug("Processing BeforeSave handlers");
            
            foreach (var handler in _saveHandlers)
            {
                var result = await handler.HandleBeforeSaveAsync(context);
                if (!result)
                {
                    _logger.LogWarning($"SaveHandler {handler.GetType().Name} returned false in BeforeSave");
                    return false;
                }
            }
            return true;
        }

        public async Task<bool> HandleAfterSaveAsync(DbContext context, int affectedRows)
        {
            _logger.LogDebug($"Processing AfterSave handlers (affected rows: {affectedRows})");
            
            foreach (var handler in _saveHandlers)
            {
                var result = await handler.HandleAfterSaveAsync(context, affectedRows);
                if (!result)
                {
                    _logger.LogWarning($"SaveHandler {handler.GetType().Name} returned false in AfterSave");
                    return false;
                }
            }
            return true;
        }
    }
}