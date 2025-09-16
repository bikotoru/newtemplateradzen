using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Backend.Utils.EFInterceptors.Core;
using Backend.Utils.Security;
using Shared.Models.Attributes;
using Shared.Models.DTOs.Auth;

namespace Backend.Utils.EFInterceptors.Handlers.SaveHandlers
{
    public class FieldPermissionSaveHandler : SaveHandler
    {
        private readonly ILogger<FieldPermissionSaveHandler> _logger;
        private readonly IServiceProvider _serviceProvider;
        private static readonly Dictionary<Type, PropertyInfo[]> _fieldPermissionPropertiesCache = new();

        public FieldPermissionSaveHandler(ILogger<FieldPermissionSaveHandler> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public override async Task<bool> HandleBeforeSaveAsync(DbContext context)
        {
            try
            {
                _logger.LogWarning("🔥 DEBUG: FieldPermissionSaveHandler INICIADO");

                // Obtener permisos del usuario actual
                var userPermissions = await GetCurrentUserPermissionsAsync();
                if (userPermissions == null)
                {
                    userPermissions = new List<string>(); // Usuario sin permisos = lista vacía
                }

                var entries = context.ChangeTracker.Entries()
                    .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                    .ToList();

                _logger.LogWarning($"🔥 DEBUG: Encontradas {entries.Count} entidades para validar");

                foreach (var entry in entries)
                {
                    _logger.LogWarning($"🔥 DEBUG: Entidad {entry.Entity.GetType().Name} - Estado: {entry.State}");
                }

                foreach (var entry in entries)
                {
                    var entity = entry.Entity;
                    var entityType = entity.GetType();
                    
                    var fieldPermissionProperties = GetFieldPermissionProperties(entityType);
                    _logger.LogWarning($"🔥 DEBUG: Entidad {entityType.Name} tiene {fieldPermissionProperties.Length} campos FieldPermission");
                    
                    if (fieldPermissionProperties.Length == 0) continue;

                    foreach (var property in fieldPermissionProperties)
                    {
                        var fieldPermissionAttr = GetFieldPermissionAttribute(property);
                        if (fieldPermissionAttr == null) continue;

                        string? requiredPermission = null;
                        
                        // Determinar qué permiso se necesita
                        if (entry.State == EntityState.Added && !string.IsNullOrEmpty(fieldPermissionAttr.CREATE))
                        {
                            requiredPermission = fieldPermissionAttr.CREATE;
                            _logger.LogWarning($"🔥 DEBUG: Campo {property.Name} - CREATE requiere: {requiredPermission}");
                        }
                        else if (entry.State == EntityState.Modified && !string.IsNullOrEmpty(fieldPermissionAttr.UPDATE))
                        {
                            // Verificar si el campo fue realmente modificado
                            var originalValue = entry.OriginalValues[property.Name];
                            var currentValue = entry.CurrentValues[property.Name];
                            
                            _logger.LogWarning($"🔥 DEBUG: Campo {property.Name} - Valor original: '{originalValue}' vs actual: '{currentValue}'");
                            
                            if (!Equals(originalValue, currentValue))
                            {
                                requiredPermission = fieldPermissionAttr.UPDATE;
                                _logger.LogWarning($"🔥 DEBUG: Campo {property.Name} FUE MODIFICADO - UPDATE requiere: {requiredPermission}");
                            }
                            else
                            {
                                _logger.LogWarning($"🔥 DEBUG: Campo {property.Name} NO fue modificado - sin validación");
                            }
                        }

                        // Validar permiso si se requiere
                        if (!string.IsNullOrEmpty(requiredPermission))
                        {
                            _logger.LogWarning($"🔥 DEBUG: Validando permiso {requiredPermission}");
                            _logger.LogWarning($"🔥 DEBUG: Usuario tiene permisos: [{string.Join(", ", userPermissions)}]");
                            
                            if (!userPermissions.Contains(requiredPermission))
                            {
                                var operation = entry.State == EntityState.Added ? "crear" : "actualizar";
                                var errorMessage = $"🚨 ACCESO DENEGADO: No tiene permisos para {operation} el campo '{property.Name}' de {entityType.Name}. Permiso requerido: {requiredPermission}";
                                
                                _logger.LogError(errorMessage);
                                throw new UnauthorizedAccessException(errorMessage);
                            }
                            else
                            {
                                _logger.LogWarning($"✅ DEBUG: Permiso {requiredPermission} VALIDADO OK");
                            }
                        }
                    }
                }

                _logger.LogWarning("🔥 DEBUG: FieldPermissionSaveHandler COMPLETADO SIN ERRORES");
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("🔥 DEBUG: FieldPermissionSaveHandler - LANZANDO UnauthorizedAccessException");
                throw; // Re-lanzar excepciones de autorización
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 DEBUG: FieldPermissionSaveHandler - ERROR INESPERADO");
                return true; // Permitir continuar en caso de error de validación
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

        private async Task<List<string>?> GetCurrentUserPermissionsAsync()
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
                var permissions = await currentUserService.GetCurrentUserPermissionsAsync();
                return permissions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo permisos del usuario - operaciones bloqueadas");
                return new List<string>(); // Error = sin permisos
            }
        }

        public override async Task<bool> HandleAfterSaveAsync(DbContext context, int affectedRows)
        {
            // No necesitamos procesar nada después de guardar para FieldPermissions
            return true;
        }
    }
}