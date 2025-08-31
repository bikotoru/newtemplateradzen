using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Backend.Utils.EFInterceptors.Core;

namespace Backend.Utils.EFInterceptors.Handlers.SaveHandlers
{
    public class GlobalSaveHandler : SaveHandler
    {
        private readonly ILogger<GlobalSaveHandler> _logger;

        public GlobalSaveHandler(ILogger<GlobalSaveHandler> logger)
        {
            _logger = logger;
        }

        public override async Task<bool> HandleBeforeSaveAsync(DbContext context)
        {
            _logger.LogInformation("[GlobalSaveHandler] Processing BeforeSave operation");
            
            // Aquí agregaremos la lógica personalizada que me digas
            
            // Por ahora, siempre permitir la operación
            return await Task.FromResult(true);
        }

        public override async Task<bool> HandleAfterSaveAsync(DbContext context, int affectedRows)
        {
            _logger.LogInformation($"[GlobalSaveHandler] Processing AfterSave operation (affected rows: {affectedRows})");
            
            // Aquí agregaremos la lógica personalizada que me digas
            
            // Por ahora, siempre retornar true
            return await Task.FromResult(true);
        }
    }
}