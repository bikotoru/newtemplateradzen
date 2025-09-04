using Microsoft.AspNetCore.Components;
using Radzen;
using System.Reflection;
using System.Collections;

namespace Frontend.Components.Base.Tables;

/// <summary>
/// Partial class de EntityTable que maneja la lógica de filtros híbridos
/// </summary>
public partial class EntityTable<T>
{
    #region Filter Mode Logic
    
    /// <summary>
    /// Determina el FilterMode efectivo del grid basado en la configuración híbrida
    /// </summary>
    private FilterMode GetEffectiveFilterMode()
    {
        // Si los filtros híbridos están deshabilitados, usar el modo por defecto
        if (!EnableHybridFilters)
            return DefaultFilterMode;
            
        // Si hay al menos una columna configurada para usar CheckBoxFilter, 
        // el grid debe usar FilterMode.CheckBoxList para soportarlo
        if (ColumnConfigs?.Any(c => c.UseCheckBoxFilter || c.ColumnFilterMode == FilterMode.CheckBoxList) == true)
            return FilterMode.CheckBoxList;
            
        return DefaultFilterMode;
    }
    
    /// <summary>
    /// Determina el PopupRenderMode basado en el FilterMode efectivo
    /// </summary>
    private PopupRenderMode GetFilterPopupRenderMode()
    {
        var effectiveMode = GetEffectiveFilterMode();
        
        // CheckBoxList requiere OnDemand para cargar datos dinámicamente
        return effectiveMode == FilterMode.CheckBoxList 
            ? PopupRenderMode.OnDemand 
            : PopupRenderMode.Initial;
    }
    
    /// <summary>
    /// Obtiene el FilterMode específico para una columna
    /// </summary>
    private FilterMode GetColumnFilterMode(ColumnConfig<T> column)
    {
        // Si los filtros híbridos están deshabilitados, usar el modo del grid
        if (!EnableHybridFilters)
            return GetEffectiveFilterMode();
            
        // Si la columna tiene FilterMode específico, usarlo
        if (column.ColumnFilterMode.HasValue)
            return column.ColumnFilterMode.Value;
            
        // Si la columna está marcada para usar CheckBoxFilter, usar CheckBoxList
        if (column.UseCheckBoxFilter)
            return FilterMode.CheckBoxList;
            
        // Para otros casos, determinar automáticamente basado en el tipo de propiedad
        return GetAutoFilterModeForProperty(column.Property);
    }
    
    /// <summary>
    /// Determina automáticamente el FilterMode más apropiado para una propiedad
    /// </summary>
    private FilterMode GetAutoFilterModeForProperty(string propertyName)
    {
        try
        {
            var propertyInfo = GetPropertyInfo(propertyName);
            if (propertyInfo == null) return DefaultFilterMode;
            
            var propertyType = propertyInfo.PropertyType;
            
            // Para tipos nullable, obtener el tipo subyacente
            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                propertyType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            }
            
            // Tipos que funcionan mejor con CheckBoxList
            if (propertyType.IsEnum ||
                propertyType == typeof(bool) ||
                propertyType == typeof(string))
            {
                // Para strings, solo usar CheckBoxList si hay pocas opciones únicas
                // (esto se puede mejorar con lógica más sofisticada)
                return propertyType == typeof(string) ? DefaultFilterMode : FilterMode.CheckBoxList;
            }
            
            // Tipos numéricos y fechas funcionan mejor con filtros tradicionales
            return DefaultFilterMode;
        }
        catch
        {
            return DefaultFilterMode;
        }
    }
    
    /// <summary>
    /// Obtiene la configuración de virtualización para una columna
    /// </summary>
    private bool GetColumnVirtualization(ColumnConfig<T> column)
    {
        // Si no es checkbox filter, la virtualización no aplica
        var filterMode = GetColumnFilterMode(column);
        if (filterMode != FilterMode.CheckBoxList)
            return true; // Valor por defecto
            
        // Usar configuración específica de la columna si existe
        return column.CheckboxFilterOptions?.EnableVirtualization ?? true;
    }
    
    #endregion
    
    #region Property Reflection Helpers
    
    /// <summary>
    /// Obtiene información de reflexión para una propiedad (incluyendo propiedades navegacionales)
    /// </summary>
    private PropertyInfo? GetPropertyInfo(string propertyPath)
    {
        try
        {
            var type = typeof(T);
            var properties = propertyPath.Split('.');
            
            PropertyInfo? currentProperty = null;
            var currentType = type;
            
            foreach (var propertyName in properties)
            {
                currentProperty = currentType.GetProperty(propertyName);
                if (currentProperty == null) return null;
                
                currentType = currentProperty.PropertyType;
                
                // Si es nullable, obtener el tipo subyacente
                if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    currentType = Nullable.GetUnderlyingType(currentType) ?? currentType;
                }
            }
            
            return currentProperty;
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Determina si una propiedad es navegacional (relación)
    /// </summary>
    private bool IsNavigationProperty(string propertyPath)
    {
        return propertyPath.Contains('.');
    }
    
    /// <summary>
    /// Extrae el nombre de la entidad relacionada de una propiedad navegacional
    /// Ejemplo: "Category.Name" → "Category"
    /// </summary>
    private string? GetNavigationPropertyName(string propertyPath)
    {
        if (!IsNavigationProperty(propertyPath)) return null;
        
        var parts = propertyPath.Split('.');
        return parts[0];
    }
    
    /// <summary>
    /// Extrae el nombre de la propiedad de display de una propiedad navegacional
    /// Ejemplo: "Category.Name" → "Name"
    /// </summary>
    private string? GetNavigationDisplayProperty(string propertyPath)
    {
        if (!IsNavigationProperty(propertyPath)) return null;
        
        var parts = propertyPath.Split('.');
        return parts[1];
    }
    
    #endregion
    
    #region LoadColumnFilterData Handler
    
    /// <summary>
    /// Handler principal para cargar datos de filtros de checkbox
    /// </summary>
    private async Task HandleLoadColumnFilterData(DataGridLoadColumnFilterDataEventArgs<T> args)
    {
        Console.WriteLine($"🔥 HandleLoadColumnFilterData called for column: {args.Column?.GetFilterProperty()}");
        Console.WriteLine($"🔥 FilterMode: {GetEffectiveFilterMode()}");
        Console.WriteLine($"🔥 EnableHybridFilters: {EnableHybridFilters}");
        
        try
        {
            // 1. Si el usuario proporcionó un handler personalizado, usarlo
            if (OnLoadColumnFilterData.HasDelegate)
            {
                await OnLoadColumnFilterData.InvokeAsync(args);
                return;
            }
            
            // 2. Auto-generar datos usando el sistema interno
            await LoadFilterDataInternal(args);
        }
        catch (Exception ex)
        {
            // Log error pero no fallar el componente
            Console.WriteLine($"Error loading filter data for column {args.Column?.GetFilterProperty()}: {ex.Message}");
            
            // Proporcionar datos vacíos como fallback
            args.Data = Enumerable.Empty<object>();
            args.Count = 0;
        }
        finally
        {
            // Seguridad final: asegurar que args.Data nunca sea null
            Console.WriteLine($"🟣 FINALLY - args.Data is null: {args.Data == null}, args.Count: {args.Count}");
            if (args.Data == null)
            {
                Console.WriteLine($"🔴 FINALLY - Setting args.Data to empty collection");
                args.Data = Enumerable.Empty<object>();
                args.Count = 0;
            }
            else
            {
                Console.WriteLine($"🟢 FINALLY - args.Data type: {args.Data.GetType().Name}, Count: {args.Count}");
            }
        }
    }
    
    /// <summary>
    /// Lógica interna para cargar datos de filtro
    /// </summary>
    private async Task LoadFilterDataInternal(DataGridLoadColumnFilterDataEventArgs<T> args)
    {
        Console.WriteLine($"🟡 LoadFilterDataInternal START - Column: {args.Column?.GetFilterProperty()}");
        
        if (apiService == null || args.Column == null)
        {
            Console.WriteLine($"🔴 Early return - apiService: {apiService != null}, Column: {args.Column != null}");
            args.Data = Enumerable.Empty<object>();
            args.Count = 0;
            return;
        }
        
        var propertyPath = args.Column.GetFilterProperty();
        Console.WriteLine($"🟡 PropertyPath extracted: '{propertyPath}'");
        
        if (string.IsNullOrEmpty(propertyPath))
        {
            Console.WriteLine($"🔴 PropertyPath is null/empty");
            args.Data = Enumerable.Empty<object>();
            args.Count = 0;
            return;
        }
        
        var columnConfig = ColumnConfigs?.FirstOrDefault(c => c.Property == propertyPath);
        
        // Solo procesar si la columna está configurada para checkbox filter
        var effectiveFilterMode = GetColumnFilterMode(columnConfig ?? new ColumnConfig<T> { Property = propertyPath });
        Console.WriteLine($"🟡 ColumnConfig found: {columnConfig != null}, UseCheckBoxFilter: {columnConfig?.UseCheckBoxFilter}, EffectiveFilterMode: {effectiveFilterMode}");
        
        if (columnConfig?.UseCheckBoxFilter != true && effectiveFilterMode != FilterMode.CheckBoxList)
        {
            Console.WriteLine($"🔴 Column not configured for checkbox filter - returning empty data");
            // Asegurar que args.Data nunca sea null para evitar errores en Radzen
            args.Data = Enumerable.Empty<object>();
            args.Count = 0;
            return;
        }
        
        Console.WriteLine($"🟢 Column IS configured for checkbox filter - proceeding...");
        
        // Verificar cache si está habilitado
        if (EnableFilterCache)
        {
            var cachedData = await GetCachedFilterData(propertyPath, args.Filter);
            if (cachedData.HasValue)
            {
                ApplyCachedDataToArgs(args, cachedData.Value, columnConfig);
                return;
            }
        }
        
        // Cargar datos desde la base de datos
        Console.WriteLine($"🟡 About to load distinct values from database...");
        await LoadDistinctValuesFromDatabase(args, propertyPath, columnConfig);
        Console.WriteLine($"🟡 Loaded data - Count: {args.Count}, Data type: {args.Data?.GetType()?.Name ?? "null"}");
        
        // Guardar en cache si está habilitado
        if (EnableFilterCache && args.Data != null)
        {
            await CacheFilterData(propertyPath, args.Filter, args.Data, args.Count);
        }
        
        Console.WriteLine($"🟢 LoadFilterDataInternal COMPLETE");
    }
    
    #endregion
    
    #region Cache Management
    
    /// <summary>
    /// Obtiene datos de filtro desde el cache
    /// </summary>
    private async Task<(IEnumerable<object> data, int count)?> GetCachedFilterData(string propertyPath, string? filter)
    {
        await cacheSemaphore.WaitAsync();
        try
        {
            var cacheKey = GetCacheKey(propertyPath, filter);
            
            if (filterCache.TryGetValue(cacheKey, out var cached))
            {
                // Verificar si no ha expirado
                if (DateTime.Now - cached.timestamp < FilterCacheTimeout)
                {
                    if (cached.data is (IEnumerable<object> data, int count))
                    {
                        return (data, count);
                    }
                }
                else
                {
                    // Eliminar entrada expirada
                    filterCache.Remove(cacheKey);
                }
            }
            
            return null;
        }
        finally
        {
            cacheSemaphore.Release();
        }
    }
    
    /// <summary>
    /// Guarda datos de filtro en el cache
    /// </summary>
    private async Task CacheFilterData(string propertyPath, string? filter, IEnumerable data, int count)
    {
        await cacheSemaphore.WaitAsync();
        try
        {
            var cacheKey = GetCacheKey(propertyPath, filter);
            filterCache[cacheKey] = (DateTime.Now, (data, count));
            
            // Limpiar cache antiguo si hay muchas entradas
            if (filterCache.Count > 100)
            {
                var oldEntries = filterCache.Where(kvp => DateTime.Now - kvp.Value.timestamp > FilterCacheTimeout).ToList();
                foreach (var entry in oldEntries)
                {
                    filterCache.Remove(entry.Key);
                }
            }
        }
        finally
        {
            cacheSemaphore.Release();
        }
    }
    
    /// <summary>
    /// Genera clave de cache única para una propiedad y filtro
    /// </summary>
    private string GetCacheKey(string propertyPath, string? filter)
    {
        return $"{typeof(T).Name}_{propertyPath}_{filter ?? ""}";
    }
    
    /// <summary>
    /// Aplica datos cached a los argumentos del evento
    /// </summary>
    private void ApplyCachedDataToArgs(DataGridLoadColumnFilterDataEventArgs<T> args, 
        (IEnumerable<object> data, int count) cachedData, ColumnConfig<T>? columnConfig)
    {
        var (data, totalCount) = cachedData;
        
        // Aplicar paginación si es necesario
        var enumerable = data;
        
        if (args.Skip.HasValue && args.Skip > 0)
        {
            enumerable = enumerable.Skip(args.Skip.Value);
        }
        
        if (args.Top.HasValue && args.Top > 0)
        {
            enumerable = enumerable.Take(args.Top.Value);
        }
        
        args.Data = enumerable;
        args.Count = totalCount;
    }
    
    #endregion
    
    #region Database Loading
    
    /// <summary>
    /// Carga valores únicos desde la base de datos
    /// </summary>
    private async Task LoadDistinctValuesFromDatabase(DataGridLoadColumnFilterDataEventArgs<T> args, 
        string propertyPath, ColumnConfig<T>? columnConfig)
    {
        if (apiService == null)
        {
            args.Data = Enumerable.Empty<object>();
            args.Count = 0;
            return;
        }
        
        try
        {
            // Determinar si es una propiedad navegacional
            if (IsNavigationProperty(propertyPath))
            {
                await LoadNavigationPropertyDistinctValues(args, propertyPath, columnConfig);
            }
            else
            {
                await LoadSimplePropertyDistinctValues(args, propertyPath, columnConfig);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading distinct values for {propertyPath}: {ex.Message}");
            args.Data = Enumerable.Empty<object>();
            args.Count = 0;
        }
    }
    
    /// <summary>
    /// Carga valores únicos para propiedades simples (no navegacionales)
    /// </summary>
    private async Task LoadSimplePropertyDistinctValues(DataGridLoadColumnFilterDataEventArgs<T> args, 
        string propertyPath, ColumnConfig<T>? columnConfig)
    {
        Console.WriteLine($"🟦 LoadSimplePropertyDistinctValues START - Property: {propertyPath}");
        
        // Crear query base
        var query = apiService!.Query();
        
        // TODO: Implementar carga real desde base de datos
        // Por ahora, proporcionar datos mock para permitir compilación
        var mockValues = new List<object>();
        
        // Generar datos mock basados en la propiedad
        // Radzen espera objetos con la propiedad que coincida con el nombre de la columna
        if (propertyPath.Contains("Active"))
        {
            // Para columnas booleanas, crear objetos con el nombre de propiedad correcto
            var activeValues = new List<object>
            {
                CreateDynamicObject(propertyPath, true),
                CreateDynamicObject(propertyPath, false)
            };
            mockValues.AddRange(activeValues);
        }
        else if (propertyPath.Contains("Nombre"))
        {
            // Para columnas de nombre, crear objetos con el nombre de propiedad correcto
            var nombreValues = new List<object>
            {
                CreateDynamicObject(propertyPath, "Categoría A"),
                CreateDynamicObject(propertyPath, "Categoría B"),
                CreateDynamicObject(propertyPath, "Categoría C")
            };
            mockValues.AddRange(nombreValues);
        }
        else
        {
            // Para otras propiedades, usar valores genéricos
            var genericValues = new List<object>
            {
                CreateDynamicObject(propertyPath, "Valor 1"),
                CreateDynamicObject(propertyPath, "Valor 2")
            };
            mockValues.AddRange(genericValues);
        }
        
        // Aplicar filtro si existe
        if (!string.IsNullOrEmpty(args.Filter))
        {
            mockValues = mockValues.Where(v => v.ToString()?.ToLower().Contains(args.Filter.ToLower()) ?? false).ToList();
        }
        
        args.Data = mockValues ?? Enumerable.Empty<object>();
        args.Count = mockValues?.Count ?? 0;
    }
    
    /// <summary>
    /// Extrae valor de propiedad de un objeto dinámico
    /// </summary>
    private object? GetPropertyValueFromDynamic(object? dynamicObject, string propertyName)
    {
        if (dynamicObject == null) return null;
        
        try
        {
            // Si es JsonElement (común en respuestas de API)
            if (dynamicObject is System.Text.Json.JsonElement jsonElement)
            {
                if (jsonElement.TryGetProperty(propertyName, out var prop))
                {
                    return prop.ValueKind switch
                    {
                        System.Text.Json.JsonValueKind.String => prop.GetString(),
                        System.Text.Json.JsonValueKind.Number => prop.GetDecimal(),
                        System.Text.Json.JsonValueKind.True => true,
                        System.Text.Json.JsonValueKind.False => false,
                        System.Text.Json.JsonValueKind.Null => null,
                        _ => prop.ToString()
                    };
                }
            }
            
            // Intentar con reflexión como fallback
            var type = dynamicObject.GetType();
            var property = type.GetProperty(propertyName);
            return property?.GetValue(dynamicObject);
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Carga valores únicos para propiedades navegacionales (relaciones)
    /// </summary>
    private async Task LoadNavigationPropertyDistinctValues(DataGridLoadColumnFilterDataEventArgs<T> args, 
        string propertyPath, ColumnConfig<T>? columnConfig)
    {
        // Extraer partes de la propiedad navegacional
        var navigationProperty = GetNavigationPropertyName(propertyPath);
        var displayProperty = GetNavigationDisplayProperty(propertyPath);
        
        if (string.IsNullOrEmpty(navigationProperty) || string.IsNullOrEmpty(displayProperty))
        {
            await LoadSimplePropertyDistinctValues(args, propertyPath, columnConfig);
            return;
        }
        
        // TODO: Implementar carga real desde base de datos para propiedades navegacionales
        // Por ahora, proporcionar datos mock para permitir compilación
        var mockValues = new List<object>();
        
        if (displayProperty?.Contains("Nombre") == true)
        {
            var orgValues = new List<object>
            {
                CreateDynamicObject(propertyPath, "Organización Alpha"),
                CreateDynamicObject(propertyPath, "Organización Beta"),
                CreateDynamicObject(propertyPath, "Organización Gamma")
            };
            mockValues.AddRange(orgValues);
        }
        else if (displayProperty?.Contains("UserName") == true)
        {
            var userValues = new List<object>
            {
                CreateDynamicObject(propertyPath, "admin"),
                CreateDynamicObject(propertyPath, "user1"),
                CreateDynamicObject(propertyPath, "user2")
            };
            mockValues.AddRange(userValues);
        }
        else
        {
            var genericValues = new List<object>
            {
                CreateDynamicObject(propertyPath, "Relación X"),
                CreateDynamicObject(propertyPath, "Relación Y")
            };
            mockValues.AddRange(genericValues);
        }
        
        // Aplicar filtro si existe
        if (!string.IsNullOrEmpty(args.Filter))
        {
            Console.WriteLine($"🟦 Applying filter: '{args.Filter}'");
            mockValues = mockValues.Where(v => v.ToString()?.ToLower().Contains(args.Filter.ToLower()) ?? false).ToList();
        }
        
        Console.WriteLine($"🟦 Mock values generated: {mockValues?.Count ?? 0} items");
        if (mockValues != null)
        {
            foreach (var item in mockValues.Take(3)) 
            {
                Console.WriteLine($"🟦 Mock item type: {item.GetType().Name}, Value: {item}");
            }
        }
        
        args.Data = mockValues ?? Enumerable.Empty<object>();
        args.Count = mockValues?.Count ?? 0;
        
        Console.WriteLine($"🟢 LoadSimplePropertyDistinctValues COMPLETE - Final args.Data type: {args.Data?.GetType()?.Name}, Count: {args.Count}");
    }
    
    #endregion
    
    #region Dynamic Object Creation
    
    /// <summary>
    /// Crea un objeto dinámico con la propiedad especificada y su valor
    /// Necesario para que Radzen pueda acceder a las propiedades por nombre
    /// </summary>
    private object CreateDynamicObject(string propertyName, object value)
    {
        Console.WriteLine($"🟨 CreateDynamicObject START - Property: {propertyName}, Value: {value}");
        
        try
        {
            // Crear una instancia mock del tipo T usando Activator
            var mockEntity = Activator.CreateInstance<T>();
            
            // Para propiedades navegacionales, usar solo la parte final
            var finalPropertyName = propertyName.Contains('.') 
                ? propertyName.Split('.').Last() 
                : propertyName;
            
            // Obtener información de la propiedad usando reflexión
            var propertyInfo = GetPropertyInfo(propertyName);
            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                // Convertir el valor al tipo correcto de la propiedad
                var targetType = propertyInfo.PropertyType;
                
                // Manejar tipos nullable
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
                }
                
                // Convertir el valor al tipo apropiado
                object? convertedValue = null;
                if (value != null)
                {
                    if (targetType == typeof(string))
                        convertedValue = value.ToString();
                    else if (targetType == typeof(bool))
                        convertedValue = Convert.ToBoolean(value);
                    else if (targetType == typeof(DateTime))
                        convertedValue = value is DateTime dt ? dt : DateTime.Now;
                    else
                        convertedValue = Convert.ChangeType(value, targetType);
                }
                
                // Establecer el valor en la propiedad
                propertyInfo.SetValue(mockEntity, convertedValue);
            }
            
            Console.WriteLine($"🟢 CreateDynamicObject SUCCESS - Created entity of type: {mockEntity?.GetType()?.Name}");
            return mockEntity!;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🔴 CreateDynamicObject ERROR for {propertyName}: {ex.Message}");
            Console.WriteLine($"🔴 Exception details: {ex}");
            
            // Fallback: retornar el valor original en un objeto genérico
            var fallback = new { Value = value };
            Console.WriteLine($"🟨 Using fallback object: {fallback}");
            return fallback;
        }
    }
    
    #endregion
    
    #region Public Helper Methods
    
    /// <summary>
    /// Limpia el cache de filtros (útil cuando se actualizan datos)
    /// </summary>
    public async Task ClearFilterCache()
    {
        await cacheSemaphore.WaitAsync();
        try
        {
            filterCache.Clear();
        }
        finally
        {
            cacheSemaphore.Release();
        }
    }
    
    /// <summary>
    /// Limpia el cache de filtros para una propiedad específica
    /// </summary>
    public async Task ClearFilterCache(string propertyPath)
    {
        await cacheSemaphore.WaitAsync();
        try
        {
            var keysToRemove = filterCache.Keys
                .Where(key => key.Contains($"{typeof(T).Name}_{propertyPath}"))
                .ToList();
                
            foreach (var key in keysToRemove)
            {
                filterCache.Remove(key);
            }
        }
        finally
        {
            cacheSemaphore.Release();
        }
    }
    
    /// <summary>
    /// Obtiene estadísticas del cache de filtros
    /// </summary>
    public async Task<(int totalEntries, int expiredEntries, TimeSpan oldestEntry)> GetFilterCacheStats()
    {
        await cacheSemaphore.WaitAsync();
        try
        {
            var now = DateTime.Now;
            var expired = filterCache.Count(kvp => now - kvp.Value.timestamp > FilterCacheTimeout);
            var oldest = filterCache.Any() 
                ? now - filterCache.Min(kvp => kvp.Value.timestamp)
                : TimeSpan.Zero;
                
            return (filterCache.Count, expired, oldest);
        }
        finally
        {
            cacheSemaphore.Release();
        }
    }
    
    #endregion
}