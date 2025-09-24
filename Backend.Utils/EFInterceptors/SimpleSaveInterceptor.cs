using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Shared.Models.Attributes;
using Backend.Utils.Security;
using Backend.Utils.Extensions;

namespace Backend.Utils.EFInterceptors
{
    public class SimpleSaveInterceptor : SaveChangesInterceptor
    {
        private readonly ILogger<SimpleSaveInterceptor> _logger;
        private readonly IServiceProvider _serviceProvider;
        private static readonly Dictionary<Type, PropertyInfo[]> _fieldPermissionPropertiesCache = new();

        public SimpleSaveInterceptor(ILogger<SimpleSaveInterceptor> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            _logger.LogInformation("🔥🔥 [SimpleSaveInterceptor] SavingChanges INTERCEPTADO!!!");
            ProcessFieldPermissions(eventData);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("🔥🔥 [SimpleSaveInterceptor] SavingChangesAsync INTERCEPTADO!!!");
            try
            {
                ProcessFieldPermissions(eventData);
                return base.SavingChangesAsync(eventData, result, cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                throw; // Re-lanzar excepciones de autorización
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando FieldPermissions");
                return base.SavingChangesAsync(eventData, result, cancellationToken);
            }
        }

        private void ProcessFieldPermissions(DbContextEventData eventData)
        {
            var context = eventData.Context;
            if (context == null) return;

            // Verificar si hay una operación Force activa
            if (ForceOperationContext.IsForced)
            {
                var forceInfo = ForceOperationContext.Current;
                _logger.LogWarning($"🚀 FORCE MODE: Saltando validaciones FieldPermission - Razón: {forceInfo?.Reason ?? "No especificada"}");
                return; // Skip all field permission validations
            }

            // Obtener permisos del usuario actual
            var userPermissions = GetCurrentUserPermissionsAsync().GetAwaiter().GetResult();
            if (userPermissions == null)
            {
                userPermissions = new List<string>(); // Sin permisos
            }

            var entries = context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                .ToList();

            foreach (var entry in entries)
            {
                var entity = entry.Entity;
                var entityType = entity.GetType();
                
                var fieldPermissionProperties = GetFieldPermissionProperties(entityType);
                if (fieldPermissionProperties.Length == 0) continue;

                foreach (var property in fieldPermissionProperties)
                {
                    var fieldPermissionAttr = GetFieldPermissionAttribute(property);
                    if (fieldPermissionAttr == null) continue;

                    string? requiredPermission = null;
                    
                    // Para entidades ADDED, validar CREATE si está definido
                    if (entry.State == EntityState.Added && !string.IsNullOrEmpty(fieldPermissionAttr.CREATE))
                    {
                        requiredPermission = fieldPermissionAttr.CREATE;
                    }
                    // Para entidades MODIFIED, validar UPDATE si está definido
                    else if (entry.State == EntityState.Modified && !string.IsNullOrEmpty(fieldPermissionAttr.UPDATE))
                    {
                        requiredPermission = fieldPermissionAttr.UPDATE;
                    }

                    // Validar permiso si se requiere
                    if (!string.IsNullOrEmpty(requiredPermission))
                    {
                        if (!userPermissions.Contains(requiredPermission))
                        {
                            if (entry.State == EntityState.Added)
                            {
                                // Para CREATE: Bloquear completamente
                                var errorMessage = $"🚨 ACCESO DENEGADO: No tiene permisos para crear el campo '{property.Name}' de {entityType.Name}. Permiso requerido: {requiredPermission}";
                                _logger.LogError(errorMessage);
                                throw new UnauthorizedAccessException(errorMessage);
                            }
                            else if (entry.State == EntityState.Modified)
                            {
                                // Para UPDATE: Omitir el campo completamente del changetracker
                                entry.Property(property.Name).IsModified = false;
                                _logger.LogWarning($"🔒 CAMPO OMITIDO: Campo '{property.Name}' omitido del UPDATE. Permiso requerido: {requiredPermission}");
                            }
                        }
                    }
                }
            }
        }

        private PropertyInfo[] GetFieldPermissionProperties(Type entityType)
        {
            if (_fieldPermissionPropertiesCache.TryGetValue(entityType, out var cachedProperties))
            {
                return cachedProperties;
            }

            var properties = new List<PropertyInfo>();

            // Buscar directamente en la entidad
            var directProperties = entityType.GetProperties()
                .Where(p => p.GetCustomAttribute<FieldPermissionAttribute>() != null)
                .ToArray();

            properties.AddRange(directProperties);

            // Buscar en MetadataType si existe
            var metadataTypeAttribute = entityType.GetCustomAttribute<System.ComponentModel.DataAnnotations.MetadataTypeAttribute>();
            if (metadataTypeAttribute != null)
            {
                var metadataType = metadataTypeAttribute.MetadataClassType;

                var metadataFields = metadataType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .Where(f => f.GetCustomAttribute<FieldPermissionAttribute>() != null);

                var metadataProperties = metadataType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.GetCustomAttribute<FieldPermissionAttribute>() != null);

                // Para cada campo o propiedad con FieldPermission en metadata, buscar la propiedad correspondiente en la entidad
                foreach (var field in metadataFields)
                {
                    var entityProperty = entityType.GetProperty(field.Name);
                    if (entityProperty != null && !properties.Contains(entityProperty))
                    {
                        properties.Add(entityProperty);
                    }
                }

                foreach (var metaProp in metadataProperties)
                {
                    var entityProperty = entityType.GetProperty(metaProp.Name);
                    if (entityProperty != null && !properties.Contains(entityProperty))
                    {
                        properties.Add(entityProperty);
                    }
                }
            }

            var result = properties.ToArray();
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
                var currentUserService = scope.ServiceProvider.GetService<ICurrentUserService>();

                if (currentUserService == null)
                {
                    return new List<string>(); // Sin permisos
                }

                var permissions = await currentUserService.GetCurrentUserPermissionsAsync();
                return permissions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo permisos del usuario actual");
                return new List<string>(); // Error = sin permisos
            }
        }

    }
}