using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Backend.Utils.EFInterceptors.Core;

namespace Backend.Utils.EFInterceptors.Handlers.AddHandlers
{
    public class GlobalAddHandler : AddHandler
    {
        private readonly ILogger<GlobalAddHandler> _logger;

        public GlobalAddHandler(ILogger<GlobalAddHandler> logger)
        {
            _logger = logger;
        }

        public override async Task<bool> HandleAddAsync<T>(DbContext context, T entity)
        {
            _logger.LogInformation($"[GlobalAddHandler] Processing Add operation for entity type: {typeof(T).Name}");
            
            // Actualizar automáticamente FechaCreacion y FechaModificacion
            UpdateCreationAndModificationDates(entity);
            
            // Por ahora, siempre permitir la operación
            return await Task.FromResult(true);
        }

        private void UpdateCreationAndModificationDates<T>(T entity)
        {
            var entityType = typeof(T);
            var currentDateTime = DateTime.UtcNow;
            
            // Buscar y actualizar FechaCreacion
            var fechaCreacionProperty = entityType.GetProperty("FechaCreacion");
            if (fechaCreacionProperty != null && fechaCreacionProperty.PropertyType == typeof(DateTime))
            {
                fechaCreacionProperty.SetValue(entity, currentDateTime);
                _logger.LogDebug($"[GlobalAddHandler] FechaCreacion establecida automáticamente a: {currentDateTime}");
            }
            
            // Buscar y actualizar FechaModificacion
            var fechaModificacionProperty = entityType.GetProperty("FechaModificacion");
            if (fechaModificacionProperty != null && fechaModificacionProperty.PropertyType == typeof(DateTime))
            {
                fechaModificacionProperty.SetValue(entity, currentDateTime);
                _logger.LogDebug($"[GlobalAddHandler] FechaModificacion establecida automáticamente a: {currentDateTime}");
            }
            
            if (fechaCreacionProperty == null && fechaModificacionProperty == null)
            {
                _logger.LogDebug($"[GlobalAddHandler] No se encontraron propiedades de fecha en {entityType.Name}");
            }
        }

        public override async Task<bool> CanHandleAsync<T>(T entity)
        {
            // Por ahora, manejar todas las entidades
            return await Task.FromResult(true);
        }
    }
}