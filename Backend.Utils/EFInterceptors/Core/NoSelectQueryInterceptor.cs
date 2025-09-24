using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Shared.Models.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Backend.Utils.Security;
using Microsoft.Extensions.DependencyInjection;
using Backend.Utils.Extensions;

namespace Backend.Utils.EFInterceptors.Core
{
    public class NoSelectQueryInterceptor : IMaterializationInterceptor
    {
        private readonly ILogger<NoSelectQueryInterceptor> _logger;
        private readonly IServiceProvider _serviceProvider;
        private static readonly Dictionary<Type, PropertyInfo[]> _noSelectPropertiesCache = new();
        private static readonly Dictionary<Type, PropertyInfo[]> _fieldPermissionPropertiesCache = new();

        public NoSelectQueryInterceptor(ILogger<NoSelectQueryInterceptor> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public object CreatedInstance(MaterializationInterceptionData materializationData, object entity)
        {
            ProcessEntityNoSelectFields(entity);
            ProcessEntityFieldPermissions(entity);
            return entity;
        }

        public object CreatingInstance(MaterializationInterceptionData materializationData, object entity)
        {
            ProcessEntityNoSelectFields(entity);
            ProcessEntityFieldPermissions(entity);
            return entity;
        }

        public object InitializedInstance(MaterializationInterceptionData materializationData, object entity)
        {
            ProcessEntityNoSelectFields(entity);
            ProcessEntityFieldPermissions(entity);
            return entity;
        }

        public object InitializingInstance(MaterializationInterceptionData materializationData, object entity)
        {
            ProcessEntityNoSelectFields(entity);
            ProcessEntityFieldPermissions(entity);
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

        private void ProcessEntityFieldPermissions(object entity)
        {
            if (entity == null) return;

            // Verificar si hay una operación Force activa
            if (ForceOperationContext.IsForced)
            {
                var forceInfo = ForceOperationContext.Current;
                _logger.LogDebug($"🚀 FORCE MODE: Saltando ocultación de campos FieldPermission - Razón: {forceInfo?.Reason ?? "No especificada"}");
                return; // Skip all field permission processing
            }

            try
            {
                var entityType = entity.GetType();
                _logger.LogDebug($"Processing FieldPermission for entity type: {entityType.Name}");

                var fieldPermissionProperties = GetFieldPermissionProperties(entityType);
                _logger.LogDebug($"Found {fieldPermissionProperties.Length} FieldPermission properties for {entityType.Name}");

                if (fieldPermissionProperties.Length > 0)
                {
                    // Obtener contexto del usuario actual
                    var userPermissions = GetCurrentUserPermissions(entity);
                    if (userPermissions == null)
                    {
                        _logger.LogWarning("No se pudo obtener permisos del usuario actual, ocultando todos los campos protegidos");
                        userPermissions = new List<string>(); // Usuario sin permisos = lista vacía
                    }

                    foreach (var property in fieldPermissionProperties)
                    {
                        if (property.CanWrite)
                        {
                            var fieldPermissionAttr = GetFieldPermissionAttribute(property);
                            if (fieldPermissionAttr?.VIEW != null)
                            {
                                if (!userPermissions.Contains(fieldPermissionAttr.VIEW))
                                {
                                    _logger.LogDebug($"Campo {property.Name} oculto - sin permiso {fieldPermissionAttr.VIEW}");
                                    property.SetValue(entity, null);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing FieldPermission fields for entity: {EntityType}", entity.GetType().Name);
            }
        }

        private PropertyInfo[] GetFieldPermissionProperties(Type entityType)
        {
            if (_fieldPermissionPropertiesCache.TryGetValue(entityType, out var cachedProperties))
            {
                return cachedProperties;
            }

            var properties = new List<PropertyInfo>();

            _logger.LogDebug($"Searching FieldPermission properties in entity type: {entityType.FullName}");

            // Buscar directamente en la entidad
            var directProperties = entityType.GetProperties()
                .Where(p => p.GetCustomAttribute<FieldPermissionAttribute>() != null)
                .ToArray();

            _logger.LogDebug($"Found {directProperties.Length} direct FieldPermission properties");
            properties.AddRange(directProperties);

            // Buscar en MetadataType si existe
            var metadataTypeAttribute = entityType.GetCustomAttribute<System.ComponentModel.DataAnnotations.MetadataTypeAttribute>();
            _logger.LogDebug($"MetadataType attribute: {(metadataTypeAttribute != null ? metadataTypeAttribute.MetadataClassType.FullName : "null")}");

            if (metadataTypeAttribute != null)
            {
                var metadataType = metadataTypeAttribute.MetadataClassType;
                _logger.LogDebug($"Checking metadata type: {metadataType.FullName}");

                var metadataFields = metadataType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .Where(f => f.GetCustomAttribute<FieldPermissionAttribute>() != null);

                var metadataProperties = metadataType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.GetCustomAttribute<FieldPermissionAttribute>() != null);

                _logger.LogDebug($"Found {metadataFields.Count()} FieldPermission fields and {metadataProperties.Count()} FieldPermission properties in metadata");

                // Para cada campo o propiedad con FieldPermission en metadata, buscar la propiedad correspondiente en la entidad
                foreach (var field in metadataFields)
                {
                    _logger.LogDebug($"Processing metadata field: {field.Name}");
                    var entityProperty = entityType.GetProperty(field.Name);
                    if (entityProperty != null && !properties.Contains(entityProperty))
                    {
                        _logger.LogDebug($"Adding FieldPermission property from metadata field: {field.Name}");
                        properties.Add(entityProperty);
                    }
                }

                foreach (var metaProp in metadataProperties)
                {
                    _logger.LogDebug($"Processing metadata property: {metaProp.Name}");
                    var entityProperty = entityType.GetProperty(metaProp.Name);
                    if (entityProperty != null && !properties.Contains(entityProperty))
                    {
                        _logger.LogDebug($"Adding FieldPermission property from metadata property: {metaProp.Name}");
                        properties.Add(entityProperty);
                    }
                }
            }

            var result = properties.ToArray();
            _logger.LogDebug($"Total FieldPermission properties found for {entityType.Name}: {result.Length}");

            _fieldPermissionPropertiesCache[entityType] = result;
            return result;
        }

        private FieldPermissionAttribute? GetFieldPermissionAttribute(PropertyInfo property)
        {
            // Buscar en la propiedad directamente
            var directAttr = property.GetCustomAttribute<FieldPermissionAttribute>();
            if (directAttr != null) return directAttr;

            // Buscar en MetadataType
            var entityType = property.DeclaringType;
            var metadataTypeAttribute = entityType?.GetCustomAttribute<System.ComponentModel.DataAnnotations.MetadataTypeAttribute>();
            if (metadataTypeAttribute != null)
            {
                var metadataType = metadataTypeAttribute.MetadataClassType;
                
                // Buscar en campo de metadata
                var metadataField = metadataType.GetField(property.Name, BindingFlags.Public | BindingFlags.Instance);
                if (metadataField != null)
                {
                    var fieldAttr = metadataField.GetCustomAttribute<FieldPermissionAttribute>();
                    if (fieldAttr != null) return fieldAttr;
                }

                // Buscar en propiedad de metadata
                var metadataProperty = metadataType.GetProperty(property.Name, BindingFlags.Public | BindingFlags.Instance);
                if (metadataProperty != null)
                {
                    var propAttr = metadataProperty.GetCustomAttribute<FieldPermissionAttribute>();
                    if (propAttr != null) return propAttr;
                }
            }

            return null;
        }

        private List<string>? GetCurrentUserPermissions(object entity)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var currentUserService = scope.ServiceProvider.GetService<Backend.Utils.Security.ICurrentUserService>();
                
                if (currentUserService == null)
                {
                    return new List<string>(); // Sin permisos
                }

                // Usar servicio thread-safe scoped
                var permissions = currentUserService.GetCurrentUserPermissionsAsync().GetAwaiter().GetResult();
                return permissions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo permisos del usuario - sin acceso a campos protegidos");
                return new List<string>(); // Error = sin permisos
            }
        }
        
        private DbContext? GetDbContextFromEntity(object entity)
        {
            try
            {
                // Intentar obtener el contexto desde el ChangeTracker de la entidad
                var entityType = entity.GetType();
                var property = entityType.GetProperty("EntityState");
                
                // Esto es complejo de obtener directamente, por ahora retornamos null
                // El patrón correcto sería pasar el contexto como parámetro
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}