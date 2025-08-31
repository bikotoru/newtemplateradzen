using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Backend.Utils.EFInterceptors.Core;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using Shared.Models.Attributes;

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
            
            // Verificar campos con atributos [SoloCrear] y [AutoIncremental]
            var validationResult = await ValidateReadOnlyFields(entity, originalEntity);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning($"[GlobalUpdateHandler] Update blocked: {validationResult.ErrorMessage}");
                throw new InvalidOperationException(validationResult.ErrorMessage);
            }
            
            return await Task.FromResult(true);
        }
        
        private async Task<ValidationResult> ValidateReadOnlyFields<T>(T entity, T originalEntity)
        {
            var entityType = typeof(T);
            
            // Buscar MetadataType si existe
            var metadataTypeAttribute = entityType.GetCustomAttribute<MetadataTypeAttribute>();
            Type? metadataType = metadataTypeAttribute?.MetadataClassType;
            
            // Obtener todas las propiedades de la entidad
            var entityProperties = entityType.GetProperties();
            
            foreach (var property in entityProperties)
            {
                // Verificar si la propiedad tiene [SoloCrear] en la clase principal
                var soloCrearAttribute = property.GetCustomAttribute<SoloCrearAttribute>();
                var autoIncrementalAttribute = property.GetCustomAttribute<AutoIncrementalAttribute>();
                
                // Si no tiene los atributos en la clase principal, verificar en MetadataType
                if ((soloCrearAttribute == null || autoIncrementalAttribute == null) && metadataType != null)
                {
                    var metadataField = metadataType.GetField(property.Name);
                    var metadataProperty = metadataType.GetProperty(property.Name);
                    
                    soloCrearAttribute ??= metadataField?.GetCustomAttribute<SoloCrearAttribute>() 
                                         ?? metadataProperty?.GetCustomAttribute<SoloCrearAttribute>();
                    
                    autoIncrementalAttribute ??= metadataField?.GetCustomAttribute<AutoIncrementalAttribute>() 
                                               ?? metadataProperty?.GetCustomAttribute<AutoIncrementalAttribute>();
                }
                
                // Si tiene [SoloCrear] o [AutoIncremental], verificar que no haya cambiado
                if (soloCrearAttribute != null || autoIncrementalAttribute != null)
                {
                    var currentValue = property.GetValue(entity);
                    var originalValue = property.GetValue(originalEntity);
                    
                    // Comparar valores
                    var valuesAreEqual = (currentValue == null && originalValue == null) ||
                                        (currentValue != null && currentValue.Equals(originalValue));
                    
                    if (!valuesAreEqual)
                    {
                        var attributeName = soloCrearAttribute != null ? "[SoloCrear]" : "[AutoIncremental]";
                        var reason = soloCrearAttribute != null 
                            ? "no puede ser modificado después de la creación" 
                            : "es un código autogenerado y no puede ser modificado";
                            
                        return new ValidationResult
                        {
                            IsValid = false,
                            ErrorMessage = $"El campo '{property.Name}' tiene el atributo {attributeName} y {reason}. " +
                                         $"Valor original: '{originalValue}', Valor nuevo: '{currentValue}'"
                        };
                    }
                    
                    var attributeType = soloCrearAttribute != null ? "[SoloCrear]" : "[AutoIncremental]";
                    _logger.LogDebug($"[GlobalUpdateHandler] Campo {attributeType} '{property.Name}' verificado - Sin cambios");
                }
            }
            
            return new ValidationResult { IsValid = true };
        }
        
        private class ValidationResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; } = string.Empty;
        }

        public override async Task<bool> CanHandleAsync<T>(T entity)
        {
            // Por ahora, manejar todas las entidades
            return await Task.FromResult(true);
        }
    }
}