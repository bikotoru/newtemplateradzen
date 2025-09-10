using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Shared.Models.Attributes;
using Microsoft.Extensions.Logging;

namespace Backend.Utils.EFInterceptors.Core
{
    public class NoSelectQueryInterceptor : IMaterializationInterceptor
    {
        private readonly ILogger<NoSelectQueryInterceptor> _logger;
        private static readonly Dictionary<Type, PropertyInfo[]> _noSelectPropertiesCache = new();

        public NoSelectQueryInterceptor(ILogger<NoSelectQueryInterceptor> logger)
        {
            _logger = logger;
        }

        public object CreatedInstance(MaterializationInterceptionData materializationData, object entity)
        {
            ProcessEntityNoSelectFields(entity);
            return entity;
        }

        public object CreatingInstance(MaterializationInterceptionData materializationData, object entity)
        {
            ProcessEntityNoSelectFields(entity);
            return entity;
        }

        public object InitializedInstance(MaterializationInterceptionData materializationData, object entity)
        {
            ProcessEntityNoSelectFields(entity);
            return entity;
        }

        public object InitializingInstance(MaterializationInterceptionData materializationData, object entity)
        {
            ProcessEntityNoSelectFields(entity);
            return entity;
        }

        private void ProcessEntityNoSelectFields(object entity)
        {
            if (entity == null) return;

            try
            {
                var entityType = entity.GetType();
                _logger.LogDebug($"Processing entity type: {entityType.Name}");
                
                var noSelectProperties = GetNoSelectProperties(entityType);
                _logger.LogDebug($"Found {noSelectProperties.Length} NoSelect properties for {entityType.Name}");

                if (noSelectProperties.Length > 0)
                {
                    foreach (var property in noSelectProperties)
                    {
                        if (property.CanWrite)
                        {
                            _logger.LogInformation($"Setting NoSelect property {property.Name} to null in {entityType.Name}");
                            property.SetValue(entity, null);
                        }
                        else
                        {
                            _logger.LogWarning($"NoSelect property {property.Name} is not writable in {entityType.Name}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing NoSelect fields for entity: {EntityType}", entity.GetType().Name);
            }
        }

        private PropertyInfo[] GetNoSelectProperties(Type entityType)
        {
            if (_noSelectPropertiesCache.TryGetValue(entityType, out var cachedProperties))
            {
                return cachedProperties;
            }

            var properties = new List<PropertyInfo>();

            _logger.LogDebug($"Searching NoSelect properties in entity type: {entityType.FullName}");

            // Buscar directamente en la entidad
            var directProperties = entityType.GetProperties()
                .Where(p => p.GetCustomAttribute<NoSelectAttribute>() != null)
                .ToArray();
            
            _logger.LogDebug($"Found {directProperties.Length} direct NoSelect properties");
            properties.AddRange(directProperties);

            // Buscar en MetadataType si existe
            var metadataTypeAttribute = entityType.GetCustomAttribute<System.ComponentModel.DataAnnotations.MetadataTypeAttribute>();
            _logger.LogDebug($"MetadataType attribute: {(metadataTypeAttribute != null ? metadataTypeAttribute.MetadataClassType.FullName : "null")}");
            
            if (metadataTypeAttribute != null)
            {
                var metadataType = metadataTypeAttribute.MetadataClassType;
                _logger.LogDebug($"Checking metadata type: {metadataType.FullName}");
                
                var metadataFields = metadataType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .Where(f => f.GetCustomAttribute<NoSelectAttribute>() != null);
                
                var metadataProperties = metadataType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.GetCustomAttribute<NoSelectAttribute>() != null);

                _logger.LogDebug($"Found {metadataFields.Count()} NoSelect fields and {metadataProperties.Count()} NoSelect properties in metadata");

                // Para cada campo o propiedad con NoSelect en metadata, buscar la propiedad correspondiente en la entidad
                foreach (var field in metadataFields)
                {
                    _logger.LogDebug($"Processing metadata field: {field.Name}");
                    var entityProperty = entityType.GetProperty(field.Name);
                    if (entityProperty != null && !properties.Contains(entityProperty))
                    {
                        _logger.LogDebug($"Adding NoSelect property from metadata field: {field.Name}");
                        properties.Add(entityProperty);
                    }
                }

                foreach (var metaProp in metadataProperties)
                {
                    _logger.LogDebug($"Processing metadata property: {metaProp.Name}");
                    var entityProperty = entityType.GetProperty(metaProp.Name);
                    if (entityProperty != null && !properties.Contains(entityProperty))
                    {
                        _logger.LogDebug($"Adding NoSelect property from metadata property: {metaProp.Name}");
                        properties.Add(entityProperty);
                    }
                }
            }

            var result = properties.ToArray();
            _logger.LogDebug($"Total NoSelect properties found for {entityType.Name}: {result.Length}");
            
            _noSelectPropertiesCache[entityType] = result;
            return result;
        }
    }
}