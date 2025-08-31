using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Backend.Utils.EFInterceptors.Core;

namespace Backend.Utils.EFInterceptors.Handlers.UpdateHandlers
{
    public class GlobalUpdateHandler : UpdateHandler
    {
        private readonly ILogger<GlobalUpdateHandler> _logger;

        public GlobalUpdateHandler(ILogger<GlobalUpdateHandler> logger)
        {
            _logger = logger;
        }

        public override async Task<bool> HandleUpdateAsync<T>(DbContext context, T entity, T originalEntity)
        {
            _logger.LogInformation($"[GlobalUpdateHandler] Processing Update operation for entity type: {typeof(T).Name}");
            
            // Aquí agregaremos la lógica personalizada que me digas
            
            // Por ahora, siempre permitir la operación
            return await Task.FromResult(true);
        }

        public override async Task<bool> CanHandleAsync<T>(T entity)
        {
            // Por ahora, manejar todas las entidades
            return await Task.FromResult(true);
        }
    }
}