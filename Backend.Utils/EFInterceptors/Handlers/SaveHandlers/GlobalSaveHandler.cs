using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Backend.Utils.EFInterceptors.Core;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using Shared.Models.Attributes;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Backend.Utils.EFInterceptors.Handlers.SaveHandlers
{
    public class GlobalSaveHandler : SaveHandler
    {
        private readonly ILogger<GlobalSaveHandler> _logger;
        
        // Cache permanente para configuraciones de system_config
        private static readonly Dictionary<string, Guid> _configCache = new();
        private static readonly object _cacheLock = new();

        public GlobalSaveHandler(ILogger<GlobalSaveHandler> logger)
        {
            _logger = logger;
        }

        public override async Task<bool> HandleBeforeSaveAsync(DbContext context)
        {
            _logger.LogInformation("[GlobalSaveHandler] Processing BeforeSave operation");
            
            // Procesar campos AutoIncremental
            await ProcessAutoIncrementalFields(context);
            
            return await Task.FromResult(true);
        }

        public override async Task<bool> HandleAfterSaveAsync(DbContext context, int affectedRows)
        {
            _logger.LogInformation($"[GlobalSaveHandler] Processing AfterSave operation (affected rows: {affectedRows})");
            
            // Aquí agregaremos la lógica personalizada que me digas
            
            // Por ahora, siempre retornar true
            return await Task.FromResult(true);
        }

        private async Task ProcessAutoIncrementalFields(DbContext context)
        {
            // Obtener todas las entidades que se van a crear (Added)
            var addedEntities = context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added)
                .ToList();

            if (!addedEntities.Any())
            {
                _logger.LogDebug("[GlobalSaveHandler] No hay entidades nuevas para procesar");
                return;
            }

            // Agrupar por tabla y OrganizationId para procesar en lotes
            var entitiesGrouped = new Dictionary<string, List<(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, PropertyInfo[] autoFields, Guid orgId)>>();

            foreach (var entry in addedEntities)
            {
                var autoIncrementalFields = GetAutoIncrementalFields(entry.Entity);
                if (!autoIncrementalFields.Any()) continue;

                var orgId = GetOrganizationId(entry.Entity);
                if (orgId == Guid.Empty)
                {
                    throw new InvalidOperationException(
                        $"No se puede generar código autoincremental para {entry.Entity.GetType().Name}: OrganizationId es requerido");
                }

                var tableName = entry.Entity.GetType().Name.ToLowerInvariant();
                var groupKey = $"{tableName}_{orgId}";

                if (!entitiesGrouped.ContainsKey(groupKey))
                {
                    entitiesGrouped[groupKey] = new List<(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry, PropertyInfo[], Guid)>();
                }

                entitiesGrouped[groupKey].Add((entry, autoIncrementalFields, orgId));
            }

            // Procesar cada grupo
            foreach (var group in entitiesGrouped)
            {
                await ProcessEntityGroup(context, group.Value);
            }
        }

        private PropertyInfo[] GetAutoIncrementalFields(object entity)
        {
            var entityType = entity.GetType();
            var autoFields = new List<PropertyInfo>();

            // Buscar MetadataType si existe
            var metadataTypeAttribute = entityType.GetCustomAttribute<MetadataTypeAttribute>();
            Type? metadataType = metadataTypeAttribute?.MetadataClassType;

            var properties = entityType.GetProperties();
            
            foreach (var property in properties)
            {
                // Verificar atributo en la clase principal
                var autoAttribute = property.GetCustomAttribute<AutoIncrementalAttribute>();
                
                // Si no está en la clase principal, verificar en MetadataType
                if (autoAttribute == null && metadataType != null)
                {
                    var metadataField = metadataType.GetField(property.Name);
                    var metadataProperty = metadataType.GetProperty(property.Name);
                    
                    autoAttribute = metadataField?.GetCustomAttribute<AutoIncrementalAttribute>() 
                                  ?? metadataProperty?.GetCustomAttribute<AutoIncrementalAttribute>();
                }

                if (autoAttribute != null)
                {
                    autoFields.Add(property);
                }
            }

            return autoFields.ToArray();
        }

        private Guid GetOrganizationId(object entity)
        {
            var orgProperty = entity.GetType().GetProperty("OrganizationId");
            if (orgProperty?.GetValue(entity) is Guid orgId)
            {
                return orgId;
            }
            return Guid.Empty;
        }

        private async Task ProcessEntityGroup(DbContext context, List<(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, PropertyInfo[] autoFields, Guid orgId)> entityGroup)
        {
            if (!entityGroup.Any()) return;

            var firstEntity = entityGroup.First();
            var tableName = firstEntity.entry.Entity.GetType().Name.ToLowerInvariant();
            var orgId = firstEntity.orgId;

            _logger.LogInformation($"[GlobalSaveHandler] Procesando {entityGroup.Count} entidades de tipo {tableName} para OrganizationId: {orgId}");

            // Procesar cada campo AutoIncremental
            foreach (var field in firstEntity.autoFields)
            {
                await ProcessAutoIncrementalField(context, entityGroup, tableName, field, orgId);
            }
        }

        private async Task ProcessAutoIncrementalField(DbContext context, 
            List<(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, PropertyInfo[] autoFields, Guid orgId)> entityGroup, 
            string tableName, PropertyInfo field, Guid orgId)
        {
            var fieldName = field.Name.ToLowerInvariant();
            var suffixKey = $"{tableName}.{fieldName}.suffix";
            var numberKey = $"{tableName}.{fieldName}.number";

            // Obtener configuraciones (con cache)
            var (suffixConfigId, numberConfigId) = await GetFieldConfiguration(context, suffixKey, numberKey);

            // Obtener prefijo si existe
            var prefix = await GetFieldPrefix(context, suffixConfigId, orgId);

            // Incrementar contador atómicamente y obtener el nuevo valor
            var startNumber = await IncrementCounter(context, numberConfigId, orgId, entityGroup.Count);

            // Generar códigos secuenciales
            for (int i = 0; i < entityGroup.Count; i++)
            {
                var currentNumber = startNumber + i;
                var code = GenerateCode(prefix, currentNumber);
                
                // Asignar el código a la entidad
                field.SetValue(entityGroup[i].entry.Entity, code);
                
                _logger.LogDebug($"[GlobalSaveHandler] Asignado código '{code}' a {tableName}.{fieldName}");
            }
        }

        private async Task<(Guid suffixConfigId, Guid numberConfigId)> GetFieldConfiguration(DbContext context, string suffixKey, string numberKey)
        {
            Guid suffixConfigId = Guid.Empty;
            Guid numberConfigId = Guid.Empty;
            bool needsDbQuery = false;

            lock (_cacheLock)
            {
                _configCache.TryGetValue(suffixKey, out suffixConfigId);
                _configCache.TryGetValue(numberKey, out numberConfigId);
                
                needsDbQuery = suffixConfigId == Guid.Empty || numberConfigId == Guid.Empty;
            }

            // Si no están en cache, consultar base de datos
            if (needsDbQuery)
            {
                var connectionString = context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT Id, Field 
                    FROM system_config 
                    WHERE Field IN (@suffixKey, @numberKey) AND Active = 1";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@suffixKey", suffixKey);
                command.Parameters.AddWithValue("@numberKey", numberKey);

                using var reader = await command.ExecuteReaderAsync();
                var configResults = new List<(Guid id, string field)>();
                
                while (await reader.ReadAsync())
                {
                    var id = reader.GetGuid("Id");
                    var field = reader.GetString("Field");
                    configResults.Add((id, field));
                }
                
                // Actualizar cache después de obtener todos los resultados
                lock (_cacheLock)
                {
                    foreach (var (id, field) in configResults)
                    {
                        _configCache[field] = id;

                        if (field == suffixKey) suffixConfigId = id;
                        if (field == numberKey) numberConfigId = id;
                    }
                }
            }

            if (numberConfigId == Guid.Empty)
            {
                throw new InvalidOperationException($"No se encontró configuración para campo autoincremental: {numberKey}");
            }

            return (suffixConfigId, numberConfigId);
        }

        private async Task<string?> GetFieldPrefix(DbContext context, Guid suffixConfigId, Guid orgId)
        {
            if (suffixConfigId == Guid.Empty) return null;

            var connectionString = context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT Value 
                FROM system_config_values 
                WHERE SystemConfigId = @configId AND OrganizationId = @orgId AND Active = 1";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@configId", suffixConfigId);
            command.Parameters.AddWithValue("@orgId", orgId);

            var result = await command.ExecuteScalarAsync();
            return result?.ToString();
        }

        private async Task<int> IncrementCounter(DbContext context, Guid numberConfigId, Guid orgId, int incrementBy)
        {
            var connectionString = context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Intentar UPDATE primero
            var updateQuery = @"
                UPDATE system_config_values 
                SET Value = CAST(ISNULL(Value, '0') AS INT) + @increment,
                    FechaModificacion = GETUTCDATE()
                OUTPUT INSERTED.Value
                WHERE SystemConfigId = @configId AND OrganizationId = @orgId AND Active = 1";

            using var updateCommand = new SqlCommand(updateQuery, connection);
            updateCommand.Parameters.AddWithValue("@configId", numberConfigId);
            updateCommand.Parameters.AddWithValue("@orgId", orgId);
            updateCommand.Parameters.AddWithValue("@increment", incrementBy);

            var result = await updateCommand.ExecuteScalarAsync();

            if (result != null)
            {
                var newValue = Convert.ToInt32(result);
                _logger.LogDebug($"[GlobalSaveHandler] Contador incrementado a: {newValue}");
                return newValue - incrementBy + 1; // Retornar el primer número del rango
            }

            // Si no existe, crear el registro
            var insertQuery = @"
                INSERT INTO system_config_values 
                (Id, SystemConfigId, Value, OrganizationId, FechaCreacion, FechaModificacion, Active)
                OUTPUT INSERTED.Value
                VALUES (NEWID(), @configId, @increment, @orgId, GETUTCDATE(), GETUTCDATE(), 1)";

            using var insertCommand = new SqlCommand(insertQuery, connection);
            insertCommand.Parameters.AddWithValue("@configId", numberConfigId);
            insertCommand.Parameters.AddWithValue("@orgId", orgId);
            insertCommand.Parameters.AddWithValue("@increment", incrementBy);

            var insertResult = await insertCommand.ExecuteScalarAsync();
            var insertedValue = Convert.ToInt32(insertResult!);
            
            _logger.LogDebug($"[GlobalSaveHandler] Contador creado con valor: {insertedValue}");
            return 1; // Primer número del rango cuando se crea por primera vez
        }

        private string GenerateCode(string? prefix, int number)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                return number.ToString();
            }

            var randomLetters = GenerateRandomLetters();
            return $"{prefix}-{number:D10}-{randomLetters}";
        }

        private string GenerateRandomLetters()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 2)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}