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