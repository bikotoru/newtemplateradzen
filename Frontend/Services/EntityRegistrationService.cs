using System.Collections.Concurrent;
using System.Reflection;
using Frontend.Modules.Admin.FormDesigner;

namespace Frontend.Services;

/// <summary>
/// Servicio para registro dinámico de entidades disponibles para campos de referencia
/// Se integra con tools/forms para detectar automáticamente nuevas entidades
/// </summary>
public class EntityRegistrationService
{
    private static readonly ConcurrentDictionary<string, EntityConfiguration> _registeredEntities = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EntityRegistrationService> _logger;

    public EntityRegistrationService(
        IServiceProvider serviceProvider,
        ILogger<EntityRegistrationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        // Registrar entidades conocidas al inicio
        RegisterKnownEntities();

        // Cargar entidades desde system_form_entities de forma diferida
        _ = Task.Run(async () =>
        {
            await Task.Delay(1000); // Esperar a que la aplicación esté lista
            await LoadEntitiesFromSystemFormEntitiesAsync();
        });
    }

    public class EntityConfiguration
    {
        public Type EntityType { get; set; } = null!;
        public Type ServiceType { get; set; } = null!;
        public string DisplayProperty { get; set; } = "Name";
        public string ValueProperty { get; set; } = "Id";
        public string[]? SearchFields { get; set; }
        public bool EnableCache { get; set; } = true;
    }

    /// <summary>
    /// Registrar entidades conocidas que ya existen en el sistema
    /// </summary>
    private void RegisterKnownEntities()
    {
        try
        {
            // ===== ENTIDADES DE DOMINIO =====
            // Aquí se registran automáticamente las entidades de dominio creadas con entity-generator.py



            // ===== ENTIDADES DEL SISTEMA =====

            // SystemUsers (múltiples alias)
            var systemUsersConfig = new EntityConfiguration
            {
                EntityType = typeof(Shared.Models.Entities.SystemEntities.SystemUsers),
                ServiceType = typeof(Frontend.Modules.Admin.SystemUsers.SystemUserService),
                DisplayProperty = "DisplayName",
                ValueProperty = "Id",
                SearchFields = new[] { "DisplayName", "Username", "Email" }
            };

            RegisterEntity("systemusers", systemUsersConfig);
            RegisterEntity("usuario", systemUsersConfig);
            RegisterEntity("user", systemUsersConfig);

            // SystemRoles
            var systemRolesConfig = new EntityConfiguration
            {
                EntityType = typeof(Shared.Models.Entities.SystemEntities.SystemRoles),
                ServiceType = typeof(Frontend.Modules.Admin.SystemRoles.SystemRoleService),
                DisplayProperty = "Name",
                ValueProperty = "Id",
                SearchFields = new[] { "Name", "Description" }
            };

            RegisterEntity("systemroles", systemRolesConfig);
            RegisterEntity("rol", systemRolesConfig);
            RegisterEntity("role", systemRolesConfig);

            _logger.LogInformation("Registered {Count} known entities", _registeredEntities.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering known entities");
        }
    }

    /// <summary>
    /// Registrar una nueva entidad manualmente
    /// </summary>
    public bool RegisterEntity(string entityKey, EntityConfiguration config)
    {
        try
        {
            var key = entityKey.ToLowerInvariant();
            _registeredEntities.AddOrUpdate(key, config, (k, v) => config);

            _logger.LogDebug("Registered entity '{EntityKey}' -> {EntityType}", entityKey, config.EntityType.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering entity '{EntityKey}'", entityKey);
            return false;
        }
    }

    /// <summary>
    /// Obtener configuración de una entidad
    /// </summary>
    public EntityConfiguration? GetEntityConfiguration(string entityKey)
    {
        var key = entityKey.ToLowerInvariant();
        return _registeredEntities.TryGetValue(key, out var config) ? config : null;
    }

    /// <summary>
    /// Obtener todas las entidades registradas
    /// </summary>
    public IReadOnlyDictionary<string, EntityConfiguration> GetAllEntities()
    {
        return _registeredEntities.AsReadOnly();
    }

    /// <summary>
    /// Crear tipo de Lookup para una entidad
    /// </summary>
    public Type? CreateLookupType(string entityKey)
    {
        var config = GetEntityConfiguration(entityKey);
        if (config == null) return null;

        try
        {
            return typeof(Frontend.Components.Base.Lookup<,>).MakeGenericType(config.EntityType, typeof(Guid?));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating lookup type for entity '{EntityKey}'", entityKey);
            return null;
        }
    }

    /// <summary>
    /// Obtener servicio para una entidad
    /// </summary>
    public object? GetEntityService(string entityKey)
    {
        var config = GetEntityConfiguration(entityKey);
        if (config == null) return null;

        try
        {
            // Intentar obtener el servicio del DI container (tanto genéricos como específicos)
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetService(config.ServiceType);

            if (service != null)
            {
                _logger.LogDebug("Retrieved service for entity: {EntityKey} of type: {ServiceType}", entityKey, config.ServiceType.Name);
                return service;
            }
            else
            {
                _logger.LogWarning("Service not found for entity: {EntityKey} of type: {ServiceType}", entityKey, config.ServiceType.Name);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service for entity '{EntityKey}'", entityKey);
            return null;
        }
    }

    /// <summary>
    /// Refrescar entidades desde system_form_entities
    /// Útil cuando se agregan nuevas entidades dinámicamente
    /// </summary>
    public async Task<bool> RefreshEntitiesFromDatabaseAsync()
    {
        try
        {
            _logger.LogInformation("Refreshing entities from system_form_entities...");

            await LoadEntitiesFromSystemFormEntitiesAsync();

            var totalEntities = _registeredEntities.Count;
            _logger.LogInformation("Refresh completed. Total registered entities: {Count}", totalEntities);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing entities from database");
            return false;
        }
    }

    /// <summary>
    /// Auto-detectar entidades basándose en convenciones
    /// Este método será llamado por tools/forms cuando genere nuevas entidades
    /// </summary>
    public async Task<bool> AutoDetectAndRegisterEntity(string entityName, string? modulePath = null)
    {
        try
        {
            _logger.LogInformation("Auto-detecting entity: {EntityName} in module: {ModulePath}", entityName, modulePath);

            // 1. Buscar el tipo de entidad
            var entityType = FindEntityType(entityName);
            if (entityType == null)
            {
                _logger.LogWarning("Entity type not found for: {EntityName}", entityName);
                return false;
            }

            // 2. Buscar el tipo de servicio
            var serviceType = FindServiceType(entityName, modulePath);
            if (serviceType == null)
            {
                _logger.LogWarning("Service type not found for: {EntityName}", entityName);
                return false;
            }

            // 3. Crear configuración automática
            var config = new EntityConfiguration
            {
                EntityType = entityType,
                ServiceType = serviceType,
                DisplayProperty = DetectDisplayProperty(entityType),
                ValueProperty = "Id",
                SearchFields = DetectSearchFields(entityType)
            };

            // 4. Registrar la entidad
            var success = RegisterEntity(entityName, config);

            if (success)
            {
                _logger.LogInformation("Successfully auto-registered entity: {EntityName}", entityName);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-detecting entity: {EntityName}", entityName);
            return false;
        }
    }

    private Type? FindEntityType(string entityName)
    {
        try
        {
            // Buscar el assembly de Shared.Models de manera genérica
            Assembly? assembly = null;
            
            // Intentar obtener el assembly de Shared.Models de diferentes formas
            try
            {
                // Opción 1: Buscar por nombre de assembly
                assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Shared.Models");
                
                // Opción 2: Si no encontramos por nombre, buscar cualquier assembly que contenga Shared.Models.Entities
                if (assembly == null)
                {
                    assembly = AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(a => a.GetTypes().Any(t => t.Namespace?.StartsWith("Shared.Models.Entities") == true));
                }
                
                // Opción 3: Como último recurso, cargar el assembly por referencia
                if (assembly == null)
                {
                    assembly = Assembly.Load("Shared.Models");
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error loading Shared.Models assembly");
                return null;
            }

            if (assembly == null)
            {
                _logger.LogWarning("Could not find Shared.Models assembly");
                return null;
            }

            // Intentar diferentes variaciones del nombre
            var entityNameTitle = char.ToUpper(entityName[0]) + entityName.Substring(1).ToLower();
            var possibleTypeNames = new[]
            {
                $"Shared.Models.Entities.{entityName}",
                $"Shared.Models.Entities.{entityNameTitle}",
                $"Shared.Models.Entities.SystemEntities.{entityName}",
                $"Shared.Models.Entities.SystemEntities.{entityNameTitle}"
            };

            foreach (var typeName in possibleTypeNames)
            {
                var entityType = assembly.GetType(typeName);
                if (entityType != null)
                {
                    _logger.LogDebug("Found entity type: {TypeName} for entity: {EntityName}", typeName, entityName);
                    return entityType;
                }
            }

            _logger.LogDebug("Entity type not found for: {EntityName}", entityName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error finding entity type for: {EntityName}", entityName);
            return null;
        }
    }

    private Type? FindServiceType(string entityName, string? modulePath)
    {
        try
        {
            // Buscar en Frontend assembly
            var assembly = Assembly.GetExecutingAssembly();

            // Intentar diferentes patrones de nombres de servicio
            var possibleServiceNames = new[]
            {
                $"Frontend.Modules.{modulePath}.{entityName}Service",
                $"Frontend.Modules.{modulePath}.{entityName}.{entityName}Service",
                $"Frontend.Services.{entityName}Service"
            };

            foreach (var serviceName in possibleServiceNames.Where(s => s != null))
            {
                var serviceType = assembly.GetType(serviceName);
                if (serviceType != null)
                {
                    return serviceType;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error finding service type for: {EntityName}", entityName);
            return null;
        }
    }

    private string DetectDisplayProperty(Type entityType)
    {
        // Buscar propiedades comunes para mostrar
        var commonDisplayProperties = new[] { "Name", "Nombre", "DisplayName", "Title", "Titulo" };

        foreach (var propName in commonDisplayProperties)
        {
            var prop = entityType.GetProperty(propName);
            if (prop != null && prop.PropertyType == typeof(string))
            {
                return propName;
            }
        }

        return "Name"; // Default fallback
    }

    private string[] DetectSearchFields(Type entityType)
    {
        var searchFields = new List<string>();

        // Buscar propiedades string que puedan ser searchables
        var searchableProperties = new[] { "Name", "Nombre", "DisplayName", "Title", "Titulo", "Description", "Descripcion", "Code", "Codigo" };

        foreach (var propName in searchableProperties)
        {
            var prop = entityType.GetProperty(propName);
            if (prop != null && prop.PropertyType == typeof(string))
            {
                searchFields.Add(propName);
            }
        }

        return searchFields.Any() ? searchFields.ToArray() : new[] { "Name" };
    }

    /// <summary>
    /// Cargar entidades desde system_form_entities table
    /// Mucho más eficiente y centralizado que archivos de configuración
    /// </summary>
    private async Task LoadEntitiesFromSystemFormEntitiesAsync()
    {
        try
        {
            _logger.LogInformation("Loading entities from system_form_entities table...");

            // Crear un scope para obtener servicios scoped
            using var scope = _serviceProvider.CreateScope();
            var systemFormEntitiesService = scope.ServiceProvider.GetRequiredService<SystemFormEntitiesService>();

            // Obtener todas las entidades activas que permiten custom fields
            var response = await systemFormEntitiesService.GetAllUnpagedAsync();

            if (!response.Success || response.Data == null)
            {
                _logger.LogWarning("Failed to load entities from system_form_entities: {Message}", response.Message);
                return;
            }

            var entities = response.Data
                .Where(e => e.Active && e.AllowCustomFields)
                .ToList();

            int loadedCount = 0;
            foreach (var formEntity in entities)
            {
                if (await TryLoadEntityFromSystemFormEntity(formEntity))
                {
                    loadedCount++;
                }
            }

            _logger.LogInformation("Successfully loaded {Count} entities from system_form_entities", loadedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading entities from system_form_entities");
        }
    }

    /// <summary>
    /// Cargar entidad desde un registro de system_form_entities
    /// </summary>
    private async Task<bool> TryLoadEntityFromSystemFormEntity(Shared.Models.Entities.SystemEntities.SystemFormEntities formEntity)
    {
        try
        {
            _logger.LogDebug("Processing entity from system_form_entities: {EntityName}", formEntity.EntityName);

            // Buscar el tipo de entidad en Shared.Models
            var entityType = FindEntityType(formEntity.EntityName);
            if (entityType == null)
            {
                _logger.LogDebug("Entity type not found for: {EntityName}", formEntity.EntityName);
                return false;
            }

            // Intentar auto-detectar el módulo y servicio
            var serviceType = await FindServiceTypeForFormEntity(formEntity);
            if (serviceType == null)
            {
                _logger.LogDebug("Service type not found for: {EntityName}", formEntity.EntityName);
                return false;
            }

            // Crear configuración automática
            var config = new EntityConfiguration
            {
                EntityType = entityType,
                ServiceType = serviceType,
                DisplayProperty = DetectDisplayProperty(entityType),
                ValueProperty = "Id",
                SearchFields = DetectSearchFields(entityType),
                EnableCache = true
            };

            // Registrar con múltiples alias
            var success = RegisterEntity(formEntity.EntityName, config);

            // También registrar variaciones comunes del nombre
            RegisterEntityAliases(formEntity.EntityName, config);

            if (success)
            {
                _logger.LogInformation("Successfully registered entity from system_form_entities: {EntityName} -> {EntityType}",
                    formEntity.EntityName, entityType.Name);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading entity from system_form_entities: {EntityName}", formEntity.EntityName);
            return false;
        }
    }

    /// <summary>
    /// Registrar alias comunes para una entidad
    /// </summary>
    private void RegisterEntityAliases(string entityName, EntityConfiguration config)
    {
        try
        {
            // Registrar en minúsculas
            RegisterEntity(entityName.ToLowerInvariant(), config);

            // Registrar nombre de tabla si es diferente
            var tableName = entityName.ToLowerInvariant();
            if (tableName != entityName.ToLowerInvariant())
            {
                RegisterEntity(tableName, config);
            }

            // Registrar variaciones comunes
            if (entityName.EndsWith("s"))
            {
                // Singular: "Users" -> "User"
                var singular = entityName.Substring(0, entityName.Length - 1);
                RegisterEntity(singular.ToLowerInvariant(), config);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error registering aliases for entity: {EntityName}", entityName);
        }
    }

    /// <summary>
    /// Buscar tipo de servicio para una entidad del FormDesigner
    /// </summary>
    private async Task<Type?> FindServiceTypeForFormEntity(Shared.Models.Entities.SystemEntities.SystemFormEntities formEntity)
    {
        // Intentar diferentes patrones comunes
        var possibleServiceNames = new[]
        {
            // Patrón: Frontend.Modules.{Category}.{EntityName}Service
            $"Frontend.Modules.{formEntity.Category}.{formEntity.EntityName}Service",
            $"Frontend.Modules.{formEntity.Category}.{formEntity.EntityName}.{formEntity.EntityName}Service",

            // Patrón: Frontend.Modules.Admin.{EntityName}Service
            $"Frontend.Modules.Admin.{formEntity.EntityName}.{formEntity.EntityName}Service",

            // Patrón: Frontend.Services.{EntityName}Service
            $"Frontend.Services.{formEntity.EntityName}Service",

            // Patrón específico para SystemEntities
            $"Frontend.Modules.Admin.{formEntity.EntityName}.{formEntity.EntityName}Service",
        };

        var assembly = Assembly.GetExecutingAssembly();

        foreach (var serviceName in possibleServiceNames.Where(s => !string.IsNullOrEmpty(s)))
        {
            try
            {
                var serviceType = assembly.GetType(serviceName);
                if (serviceType != null)
                {
                    _logger.LogDebug("Found service type: {ServiceName} for entity: {EntityName}",
                        serviceName, formEntity.EntityName);
                    return serviceType;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error checking service type: {ServiceName}", serviceName);
            }
        }

        return null;
    }
}
