# 🔗 Fase 2: Integración y Referencias

## 🎯 Objetivos
- Implementar campos de referencia entre entidades
- Sistema de lookup/búsqueda avanzada
- Relaciones dinámicas entre custom fields
- Cacheo inteligente de configuraciones

## ⏱️ Duración Estimada: 2-3 semanas
## 🚨 Prioridad: ALTA

---

## 🆕 Nuevas Funcionalidades

### 1. **Campos de Referencia (Lookup Fields)**

#### Tipos de Referencia
- **Entity Reference**: Referencias a registros de otras entidades
- **User Reference**: Referencias a usuarios del sistema
- **File Reference**: Referencias a archivos/documentos
- **Dynamic Lookup**: Búsquedas personalizables

#### Implementación Backend
```csharp
// Nuevo tipo de campo
public enum CustomFieldType
{
    // ... tipos existentes
    EntityReference,
    UserReference,
    FileReference,
    DynamicLookup
}

// Nueva configuración para referencias
public class ReferenceConfig
{
    public string TargetEntity { get; set; }
    public string DisplayField { get; set; }
    public string ValueField { get; set; }
    public List<FilterCondition> Filters { get; set; }
    public bool AllowMultiple { get; set; }
    public bool AllowCreate { get; set; }
}
```

#### Componentes Frontend
```razor
<!-- Nuevo componente de referencia -->
@* Frontend/Components/CustomFields/ReferenceFieldEditor.razor *@
<RadzenDropDown Data="@referenceData"
                TextProperty="@DisplayField"
                ValueProperty="@ValueField"
                @bind-Value="SelectedValue"
                AllowFiltering="true"
                FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive"
                LoadData="@LoadReferenceData">
    <Template>
        <div class="reference-item">
            <strong>@((context as dynamic)?.DisplayText)</strong>
            <small class="text-muted">@((context as dynamic)?.Subtitle)</small>
        </div>
    </Template>
</RadzenDropDown>
```

### 2. **Sistema de Búsqueda Avanzada**

#### API Endpoints Nuevos
```csharp
[HttpPost("api/custom-fields/search")]
public async Task<IActionResult> SearchCustomFieldData([FromBody] SearchRequest request)
{
    // Búsqueda inteligente en custom fields
    // Filtros, ordenamiento, paginación
}

[HttpGet("api/custom-fields/suggestions/{entityName}/{fieldName}")]
public async Task<IActionResult> GetFieldSuggestions(string entityName, string fieldName, string query)
{
    // Autocompletado para campos de referencia
}
```

#### Features de Búsqueda
- **Búsqueda full-text** en valores de custom fields
- **Filtros inteligentes** por tipo de campo
- **Suggestions/Autocomplete** para referencias
- **Búsqueda cross-entity** (buscar en múltiples entidades)

### 3. **Relaciones Dinámicas**

#### Dependencias entre Campos
```csharp
public class FieldDependency
{
    public string SourceField { get; set; }
    public string TargetField { get; set; }
    public DependencyType Type { get; set; } // Show/Hide, Enable/Disable, Filter
    public List<Condition> Conditions { get; set; }
}

public enum DependencyType
{
    ShowHide,        // Mostrar/ocultar campo basado en valor
    EnableDisable,   // Habilitar/deshabilitar campo
    FilterOptions,   // Filtrar opciones de select/multiselect
    SetValue,        // Establecer valor automáticamente
    Validate         // Validaciones condicionales
}
```

#### Evaluador de Condiciones Frontend
```typescript
// Frontend/Services/ConditionEvaluator.ts
export class ConditionEvaluator {
    static evaluate(conditions: Condition[], fieldValues: Record<string, any>): boolean {
        // Evaluación de condiciones complejas
        // Soporte para AND, OR, NOT
        // Operadores: equals, contains, greater_than, etc.
    }
}
```

### 4. **Cacheo Inteligente**

#### Cache Strategy
```csharp
public interface ICacheService
{
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiry = null);
    Task RemoveAsync(string key);
    Task RemoveByPatternAsync(string pattern);
}

// Implementación con Redis o Memory Cache
public class CustomFieldsCacheService : ICacheService
{
    // Cache con invalidación inteligente
    // Cache por organización, entidad, usuario
    // Warm-up de caches críticos
}
```

## 📊 Nuevos Componentes de Base de Datos

### Tablas Adicionales
```sql
-- Referencias entre custom fields y entidades
CREATE TABLE SystemCustomFieldReferences (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    CustomFieldId UNIQUEIDENTIFIER NOT NULL,
    TargetEntityName NVARCHAR(100) NOT NULL,
    TargetRecordId UNIQUEIDENTIFIER NOT NULL,
    DisplayText NVARCHAR(500),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (CustomFieldId) REFERENCES SystemCustomFieldDefinitions(Id)
);

-- Dependencias entre campos
CREATE TABLE SystemCustomFieldDependencies (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    SourceFieldId UNIQUEIDENTIFIER NOT NULL,
    TargetFieldId UNIQUEIDENTIFIER NOT NULL,
    DependencyType NVARCHAR(50) NOT NULL,
    Conditions NVARCHAR(MAX), -- JSON
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (SourceFieldId) REFERENCES SystemCustomFieldDefinitions(Id),
    FOREIGN KEY (TargetFieldId) REFERENCES SystemCustomFieldDefinitions(Id)
);

-- Cache de búsquedas
CREATE TABLE SystemCustomFieldSearchCache (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    EntityName NVARCHAR(100) NOT NULL,
    QueryHash NVARCHAR(64) NOT NULL, -- Hash de la query
    Results NVARCHAR(MAX), -- JSON con resultados
    ExpiresAt DATETIME2 NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);
```

## 🎨 Mejoras en el Diseñador

### 1. **Panel de Referencias**
- Diseñador visual para configurar referencias
- Preview de datos de referencia en tiempo real
- Configuración de filtros avanzados

### 2. **Dependency Designer**
- Editor visual de dependencias entre campos
- Simulador de condiciones
- Testing de lógica de dependencias

### 3. **Performance Monitor**
- Dashboard de performance de custom fields
- Métricas de uso de cache
- Alertas de queries lentas

## 🔄 Flujos de Trabajo Nuevos

### 1. **Flujo de Creación de Referencia**
1. Usuario selecciona tipo "Entity Reference"
2. Sistema muestra entidades disponibles
3. Usuario configura campo de display y value
4. Sistema valida que la referencia es válida
5. Preview automático con datos de muestra

### 2. **Flujo de Dependencias**
1. Usuario selecciona campo fuente
2. Define condiciones (when field X equals Y)
3. Configura acción (show/hide field Z)
4. Sistema valida lógica circular
5. Test en vivo de la dependencia

## 🛠️ Plan de Implementación

### Semana 1: Fundación de Referencias
- **Días 1-2**: Modelo de datos y migraciones
- **Días 3-4**: API endpoints básicos para referencias
- **Día 5**: Componente básico de referencia frontend

### Semana 2: Sistema de Búsqueda
- **Días 1-2**: API de búsqueda avanzada
- **Días 3-4**: Frontend de búsqueda y filtros
- **Día 5**: Integración con custom fields

### Semana 3: Dependencias y Cache
- **Días 1-2**: Sistema de dependencias
- **Días 3-4**: Implementación de cache inteligente
- **Día 5**: Testing y optimización

## 📈 Métricas de Éxito

- ✅ Campos de referencia funcionando en 3+ entidades
- ✅ Búsqueda avanzada con resultados < 200ms
- ✅ Dependencias entre campos 100% funcionales
- ✅ Cache hit rate > 80% para consultas frecuentes
- ✅ Zero queries N+1 en carga de referencias

## 🧪 Casos de Uso de Testing

### Referencias
- Crear campo de referencia Empleado -> Empresa
- Buscar empleados por empresa
- Validar integridad referencial

### Dependencias
- Campo "Tipo Cliente" condiciona "Descuento Especial"
- Campo "País" filtra opciones de "Ciudad"
- Validaciones condicionales por tipo de empleado

### Performance
- Carga de 1000+ referencias sin degradación
- Cache invalidation correcta en updates
- Búsqueda en datasets grandes (10k+ registros)

---

## 📁 Archivos Nuevos

### Backend
- `CustomFields.API/Controllers/ReferenceController.cs`
- `CustomFields.API/Services/IReferenceService.cs`
- `CustomFields.API/Services/ReferenceService.cs`
- `CustomFields.API/Services/ICacheService.cs`
- `CustomFields.API/Services/CustomFieldsCacheService.cs`
- `CustomFields.API/Models/ReferenceConfig.cs`
- `CustomFields.API/Models/FieldDependency.cs`

### Frontend
- `Frontend/Components/CustomFields/ReferenceFieldEditor.razor`
- `Frontend/Components/CustomFields/DependencyDesigner.razor`
- `Frontend/Services/ConditionEvaluator.ts`
- `Frontend/Services/ReferenceDataService.cs`

### Database
- `Database/Migrations/AddCustomFieldReferences.sql`
- `Database/Migrations/AddFieldDependencies.sql`
- `Database/Migrations/AddSearchCache.sql`