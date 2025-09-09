# Comando /ai-mejora

## Documentación Completa del Sistema Frontend/Backend

Este comando contiene toda la información necesaria para entender y trabajar con el sistema de componentes y la arquitectura Frontend/Backend.

---

## 🏗️ ARQUITECTURA GENERAL

### Flujo de Comunicación Completo
```
Frontend Components → Frontend Services → Backend Controllers → Backend Services → DbContext → Database
     ↑                      ↑                    ↑                   ↑              ↑         ↑
   UI/UX              HTTP Client           API Endpoints      Business Logic    EF Core   SQL Server
```

### Estructura de Directorios
```
Frontend/
├── Components/Base/          # Componentes reutilizables base
├── Components/CustomRadzen/  # Extensiones de Radzen
├── Components/FluentUI/      # Componentes Microsoft Fluent
├── Components/Validation/    # Sistema de validación reactiva
├── Modules/[Area]/[Entity]/  # Módulos específicos de negocio
└── Services/                 # Servicios HTTP frontend

Backend/
├── Modules/[Area]/[Entity]/  # Controladores y servicios backend
└── Services/                 # Servicios transversales

Shared.Models/
├── Entities/                 # Modelos compartidos
├── Services/                 # Builders y DTOs
└── DTOs/                     # Request/Response objects
```

## ⚡ PRIORIDADES DE DESARROLLO

### Jerarquía de Componentes (ORDEN DE PRIORIDAD)

**🥇 1. COMPONENTES CUSTOM (Primera Opción)**
- **Siempre priorizar:** Usar componentes del sistema (`Frontend/Components/Base/`, `CustomRadzen/`, `FluentUI/`, `Validation/`)
- **Ventajas:** Consistencia visual, funcionalidad probada, mantenimiento centralizado
- **Ejemplos:** `EntityTable`, `PageWithCommandBar`, `FormValidator`, `Lookup`, `SimpleCommandBar`

**🥈 2. COMPONENTES RADZEN (Segunda Opción)**  
- **Cuándo usar:** Cuando no existe un componente custom equivalente
- **Componentes permitidos:** `RadzenTextBox`, `RadzenDropDown`, `RadzenDataGrid`, `RadzenButton`, etc.
- **Configuración:** Usar clases CSS del sistema para mantener consistencia visual

**🥉 3. CÓDIGO CSS PERSONALIZADO (Tercera Opción)**
- **Cuándo usar:** Solo para ajustes menores de styling que no se pueden lograr con componentes
- **Restricciones:** Debe seguir las variables CSS del tema del sistema
- **Ubicación:** Preferiblemente en archivos `.razor.css` aislados

**🚫 4. JAVASCRIPT PERSONALIZADO (ÚLTIMA OPCIÓN)**
- **Cuándo usar:** Únicamente para funcionalidades que no se pueden implementar con Blazor
- **Restricciones:** Debe estar bien documentado y encapsulado
- **Ejemplo:** Integraciones con librerías externas, manipulación DOM específica

### Ejemplo de Implementación Correcta:

```razor
<!-- ✅ CORRECTO: Usar componentes custom primero -->
<PageWithCommandBar ShowSave="true" OnSave="SaveForm">
    <FormValidator Entity="entity" ValidationRules="validationRules">
        <ValidatedInput FieldName="Nombre" Value="entity.Nombre">
            <!-- ✅ CORRECTO: Radzen dentro de componente custom -->
            <RadzenTextBox @bind-Value="entity.Nombre" class="w-100" />
        </ValidatedInput>
    </FormValidator>
</PageWithCommandBar>

<!-- ❌ INCORRECTO: Saltarse componentes custom -->
<div class="custom-page-layout">
    <RadzenTextBox @bind-Value="entity.Nombre" />
    <style>
        .custom-page-layout { /* CSS personalizado innecesario */ }
    </style>
</div>
```

---

## 🧩 COMPONENTES BASE (Frontend/Components/)

### 1. **EntityTable<T>** - Tabla Empresarial Avanzada
**Ubicación:** `Frontend/Components/Base/EntityTable/`
**Uso:** Lista principal para cualquier entidad

#### Funcionalidades:
- ✅ Paginación automática con tamaños configurables
- ✅ Búsqueda multi-campo con operadores personalizables
- ✅ Filtros avanzados por columna con tipado fuerte
- ✅ Exportación a Excel con columnas personalizables
- ✅ Auto-refresh con intervals y countdown visual
- ✅ Configuración de columnas con persistencia localStorage
- ✅ Múltiples vistas preconfiguradas (ViewManager)
- ✅ Responsive design con layout móvil
- ✅ Acciones Edit/Delete integradas

#### Parámetros Principales:
```csharp
[Parameter] public BaseApiService<T> Service { get; set; } = null!;
[Parameter] public QueryBuilder<T> BaseQuery { get; set; } = new();
[Parameter] public List<IViewConfiguration> ViewConfigurations { get; set; } = new();
[Parameter] public EventCallback<T> OnEdit { get; set; }
[Parameter] public EventCallback<T> OnDelete { get; set; }
[Parameter] public string? ExportFileName { get; set; }
[Parameter] public bool ShowSearchBar { get; set; } = true;
[Parameter] public bool ShowExcelExport { get; set; } = true;
[Parameter] public bool ShowRefreshButton { get; set; } = true;
[Parameter] public bool EnableAutoRefresh { get; set; } = false;
```

#### Ejemplo de Uso:
```razor
<EntityTable T="SystemPermissions"
             Service="permissionService"
             BaseQuery="viewManager.GetDefaultQuery()"
             ViewConfigurations="viewManager.GetViewConfigurations()"
             OnEdit="HandleEdit"
             OnDelete="HandleDelete"
             ExportFileName="SystemPermissions" />
```

### 2. **PageWithCommandBar** - Layout Estándar con Navegación
**Ubicación:** `Frontend/Components/Base/PageWithCommandBar.razor`
**Uso:** Wrapper estándar para todas las páginas

#### Funcionalidades:
- ✅ Barra de comandos integrada (SimpleCommandBar)
- ✅ Navegación automática (botón Back)
- ✅ Botones estándar (New, Save) con callbacks
- ✅ Soporte para comandos personalizados
- ✅ Responsive design
- ✅ Soporte para temas (dark/light)

#### Parámetros:
```csharp
[Parameter] public string? BackPath { get; set; }
[Parameter] public bool ShowBack { get; set; } = true;
[Parameter] public bool ShowNew { get; set; } = false;
[Parameter] public bool ShowSave { get; set; } = false;
[Parameter] public EventCallback OnNew { get; set; }
[Parameter] public EventCallback OnSave { get; set; }
[Parameter] public List<CommandBarItem> CustomItems { get; set; } = new();
```

### 3. **FormValidator + ValidatedInput** - Sistema de Validación Reactiva
**Ubicación:** `Frontend/Components/Validation/`
**Uso:** Validación en tiempo real para formularios

#### FormValidator:
```csharp
[Parameter] public object Entity { get; set; } = null!;
[Parameter] public FormValidationRules ValidationRules { get; set; } = null!;
```

#### ValidatedInput:
```csharp
[Parameter] public string FieldName { get; set; } = string.Empty;
[Parameter] public object? Value { get; set; }
[Parameter] public RenderFragment ChildContent { get; set; } = null!;
```

#### Ejemplo de Uso:
```razor
<FormValidator Entity="entity" ValidationRules="validationRules">
    <ValidatedInput FieldName="Nombre" Value="entity.Nombre">
        <RadzenTextBox @bind-Value="entity.Nombre" class="w-100" />
    </ValidatedInput>
</FormValidator>
```

### 4. **Lookup<T>** - Dropdown Inteligente
**Ubicación:** `Frontend/Components/Base/Lookup.razor`
**Uso:** Selección de entidades relacionadas con búsqueda y cache

#### Funcionalidades:
- ✅ Búsqueda en tiempo real (servidor o memoria)
- ✅ Paginación automática
- ✅ Cache configurable con TTL
- ✅ Botón "+" para creación rápida
- ✅ Configuración flexible de propiedades

#### Parámetros Clave:
```csharp
[Parameter] public BaseApiService<T> Service { get; set; } = null!;
[Parameter] public QueryBuilder<T> BaseQuery { get; set; } = new();
[Parameter] public string DisplayProperty { get; set; } = "Name";
[Parameter] public string ValueProperty { get; set; } = "Id";
[Parameter] public bool EnableCache { get; set; } = false;
[Parameter] public TimeSpan CacheTTL { get; set; } = TimeSpan.FromMinutes(5);
```

### 5. **CrmTabs + CrmTab** - Sistema de Pestañas con URL Sync
**Ubicación:** `Frontend/Components/Base/CrmTabs/`
**Uso:** Organización de contenido en pestañas

#### Funcionalidades:
- ✅ Sincronización automática con URL
- ✅ Pestañas dinámicas con auto-registro
- ✅ Navegación con JavaScript integration
- ✅ Estados de visibilidad por pestaña

### 6. **EssentialsCard/Grid/Item** - Sistema de Información Esencial
**Ubicación:** `Frontend/Components/Base/Essentials/`
**Uso:** Mostrar metadatos y información clave de entidades

#### EssentialsCard:
- Container colapsable con header personalizable
- Ideal para mostrar información de auditoría

#### EssentialsGrid:
- CSS Grid responsive (2 columnas → 1 en móvil)
- Layout automático para items

#### EssentialsItem:
- Elemento label-value con soporte para links
- Formateo automático de tipos

### 7. **SimpleCommandBar** - Barra de Comandos Fluent
**Ubicación:** `Frontend/Components/FluentUI/SimpleCommandBar.razor`
**Uso:** Barra de comandos estilo Microsoft 365

#### Funcionalidades:
- ✅ Overflow automático con menú desplegable
- ✅ Sub-menús multinivel
- ✅ Separación de comandos primarios y secundarios
- ✅ Responsive design

### 8. **Dialogs Personalizados**
**Ubicación:** `Frontend/Components/CustomRadzen/Dialog/`

#### DynamicFormDialog:
- Formularios completamente dinámicos
- Soporte para múltiples tipos de input
- Validación declarativa integrada

#### ConfirmWaitDialog:
- Confirmación con countdown obligatorio
- Prevención de clicks accidentales

---

## 🏛️ ARQUITECTURA BACKEND

### BaseQueryController<T>
**Ubicación:** `Backend/Services/BaseQueryController.cs`
**Propósito:** Controlador base que proporciona endpoints CRUD estándar

#### Endpoints Automáticos:
```csharp
POST   /api/[area]/[entity]/create        // Crear individual
PUT    /api/[area]/[entity]/update        // Actualizar individual
GET    /api/[area]/[entity]/all           // Obtener todos (paginado/sin paginar)
GET    /api/[area]/[entity]/{id}          // Obtener por ID
DELETE /api/[area]/[entity]/{id}          // Eliminar
POST   /api/[area]/[entity]/create-batch  // Crear lote
PUT    /api/[area]/[entity]/update-batch  // Actualizar lote
POST   /api/[area]/[entity]/query         // Consultas avanzadas
GET    /api/[area]/[entity]/health        // Health check
```

#### Características:
- ✅ Validación automática de permisos
- ✅ Logging automático de operaciones
- ✅ Manejo consistente de errores
- ✅ Responses tipadas con ApiResponse<T>

### BaseQueryService<T>
**Ubicación:** `Backend/Services/BaseQueryService.cs`
**Propósito:** Servicio base para lógica de negocio CRUD

#### Funcionalidades:
- ✅ Operaciones CRUD individuales y por lotes
- ✅ Consultas avanzadas con filtros dinámicos
- ✅ Validaciones de entidad
- ✅ Transacciones automáticas
- ✅ Logging detallado
- ✅ Health checks

---

## 🌐 ARQUITECTURA FRONTEND SERVICES

### BaseApiService<T>
**Ubicación:** `Frontend/Services/BaseApiService.cs`
**Propósito:** Cliente HTTP base para comunicación con API

#### Métodos Automáticos:
```csharp
// CRUD Individual
Task<ApiResponse<T>> CreateAsync(CreateRequest<T> request)
Task<ApiResponse<T>> UpdateAsync(UpdateRequest<T> request)
Task<ApiResponse<PagedResponse<T>>> GetAllPagedAsync(int page, int pageSize)
Task<ApiResponse<List<T>>> GetAllUnpagedAsync()
Task<ApiResponse<T?>> GetByIdAsync(Guid id)
Task<ApiResponse<bool>> DeleteAsync(Guid id)

// CRUD por Lotes
Task<ApiResponse<List<T>>> CreateBatchAsync(CreateBatchRequest<T> request)
Task<ApiResponse<List<T>>> UpdateBatchAsync(UpdateBatchRequest<T> request)

// Query Builder Tipado
QueryBuilder<T> Query() // Fluent API para consultas

// Health Check
Task<ApiResponse<HealthCheckResult>> HealthCheckAsync()
```

#### QueryBuilder Usage:
```csharp
// Consulta simple
var results = await service.Query()
    .Where(x => x.Active)
    .OrderBy(x => x.Name)
    .ToListAsync();

// Consulta compleja
var pagedResults = await service.Query()
    .Where(x => x.Active && x.CreatedDate >= DateTime.Today.AddDays(-30))
    .Include(x => x.Creator)
    .Include(x => x.Organization)
    .Search("search term", x => x.Name, x => x.Description)
    .OrderByDescending(x => x.CreatedDate)
    .ThenBy(x => x.Name)
    .ToPagedResultAsync(page: 1, pageSize: 20);
```

---

## 📋 PATRÓN DE MÓDULO COMPLETO

### Ejemplo: SystemPermissions
**Estructura de archivos:**
```
Frontend/Modules/Admin/SystemPermissions/
├── SystemPermissionList.razor/.cs          # Lista principal con EntityTable
├── SystemPermissionFormulario.razor/.cs    # Formulario crear/editar
├── SystemPermissionFast.razor/.cs          # Creación rápida
├── SystemPermissionService.cs              # Cliente HTTP (hereda BaseApiService)
└── SystemPermissionViewManager.cs          # Configuraciones de vistas

Backend/Modules/Admin/SystemPermissions/
├── SystemPermissionController.cs           # API endpoints (hereda BaseQueryController)
└── SystemPermissionService.cs              # Lógica de negocio (hereda BaseQueryService)

Shared.Models/Entities/SystemEntities/
└── SystemPermissions.cs                    # Modelo compartido
```

### 1. Lista (SystemPermissionList)
```razor
<PageWithCommandBar BackPath="/admin" ShowNew="true" OnNew="NavigateToNew">
    <EntityTable T="SystemPermissions"
                 Service="permissionService"
                 BaseQuery="viewManager.GetDefaultQuery()"
                 ViewConfigurations="viewManager.GetViewConfigurations()"
                 OnEdit="HandleEdit"
                 OnDelete="HandleDelete"
                 ExportFileName="SystemPermissions" />
</PageWithCommandBar>
```

### 2. Formulario (SystemPermissionFormulario)
```razor
<PageWithCommandBar ShowSave="true" OnSave="SaveForm" BackPath="@BackPath">
    <CrmTabs DefaultTabId="general">
        <CrmTab Id="general" Title="Información General">
            <FormValidator Entity="entity" ValidationRules="validationRules">
                <EssentialsCard Title="Datos del Permiso" IsCollapsed="false">
                    <ValidatedInput FieldName="Nombre" Value="entity.Nombre">
                        <RadzenTextBox @bind-Value="entity.Nombre" class="w-100" Placeholder="Nombre del permiso" />
                    </ValidatedInput>
                    
                    <ValidatedInput FieldName="Descripcion" Value="entity.Descripcion">
                        <RadzenTextArea @bind-Value="entity.Descripcion" class="w-100" Placeholder="Descripción opcional" />
                    </ValidatedInput>
                </EssentialsCard>
                
                @if (isEditMode)
                {
                    <EssentialsCard Title="Información del Sistema" IsCollapsed="true">
                        <EssentialsGrid>
                            <EssentialsItem Label="ID" Value="@entity.Id?.ToString()" />
                            <EssentialsItem Label="Fecha Creación" Value="@entity.FechaCreacion?.ToString("dd/MM/yyyy HH:mm")" />
                            <EssentialsItem Label="Estado" Value="@(entity.Active ? "Activo" : "Inactivo")" />
                        </EssentialsGrid>
                    </EssentialsCard>
                }
            </FormValidator>
        </CrmTab>
    </CrmTabs>
</PageWithCommandBar>
```

### 3. Servicio Frontend
```csharp
public class SystemPermissionService : BaseApiService<SystemPermissions>
{
    public SystemPermissionService(HttpClient httpClient) : base(httpClient, "admin/systempermission") { }
    
    // Métodos personalizados adicionales si se necesitan
}
```

### 4. ViewManager
```csharp
public class SystemPermissionViewManager : IViewManager<SystemPermissions>
{
    public QueryBuilder<SystemPermissions> GetDefaultQuery()
    {
        return new QueryBuilder<SystemPermissions>()
            .Where(x => x.Active)
            .OrderBy(x => x.Nombre);
    }
    
    public List<IViewConfiguration> GetViewConfigurations()
    {
        return new List<IViewConfiguration>
        {
            new ViewConfiguration<SystemPermissions>
            {
                DisplayName = "Vista Completa",
                QueryBuilder = GetDefaultQuery(),
                ColumnConfigs = new List<ColumnConfig>
                {
                    new() { PropertyName = "Nombre", Title = "Nombre", Width = "200px", Sortable = true, Filterable = true },
                    new() { PropertyName = "Descripcion", Title = "Descripción", Width = "300px", Sortable = true, Filterable = true }
                }
            }
        };
    }
}
```

### 5. Controlador Backend
```csharp
[ApiController]
[Route("api/admin/systempermission")]
public class SystemPermissionController : BaseQueryController<SystemPermissions>
{
    public SystemPermissionController(SystemPermissionService service) : base(service) { }
    
    // Endpoints personalizados adicionales si se necesitan
}
```

### 6. Servicio Backend
```csharp
public class SystemPermissionService : BaseQueryService<SystemPermissions>
{
    public SystemPermissionService(AppDbContext context) : base(context) { }
    
    // Lógica de negocio personalizada si se necesita
}
```

---

## 🔐 SISTEMA DE PERMISOS

### Configuración de Permisos
```csharp
// En ModularMenu
[Parameter] public List<string> UserPermissions { get; set; } = new();

// Patrones soportados:
"ADMIN.SYSTEMPERMISSIONS.READ"    // Permiso exacto
"ADMIN.SYSTEMPERMISSIONS.*"       // Todos los permisos del módulo
"ADMIN.*"                         // Todos los permisos del área
"*"                               // Todos los permisos (superadmin)
```

### Validación Automática
- Los controladores validan permisos automáticamente
- Los menús se filtran por permisos de usuario
- Los botones/acciones se ocultan según permisos

---

## 🎨 PATRONES DE DISEÑO Y UX

### 1. **Responsive Design**
- Layout adaptativo con CSS Grid y Flexbox
- Comportamiento móvil específico
- Overflow handling inteligente

### 2. **Consistencia Visual**
- Temas (dark/light) consistentes
- Paleta de colores unificada
- Iconografía coherente

### 3. **Gestión de Estado**
- localStorage para configuraciones de usuario
- URL state management en CrmTabs
- Cache configurable con TTL

### 4. **Navegación**
- Breadcrumbs automáticos
- Sincronización URL-Estado
- Navegación por pestañas

### 5. **Feedback Visual**
- Loading states en todas las operaciones
- Notificaciones toast para acciones
- Validación en tiempo real
- Confirmaciones para acciones destructivas

---

## 🛠️ HERRAMIENTAS Y COMANDOS

### Comandos de Desarrollo
```bash
# Frontend
dotnet watch run --project Frontend

# Backend  
dotnet watch run --project Backend

# Tests
dotnet test

# Build completo
dotnet build
```

### Debugging y Logging
- Logging automático en todas las capas
- Health checks integrados
- Error handling consistente

---

## 📚 MEJORES PRÁCTICAS

### 1. **Creación de Nuevos Módulos**
1. Crear entidad en `Shared.Models/Entities/`
2. Crear controlador y servicio backend heredando de `BaseQueryController<T>` y `BaseQueryService<T>`
3. Crear servicio frontend heredando de `BaseApiService<T>`
4. Crear ViewManager implementando `IViewManager<T>`
5. Crear componentes List, Formulario y Fast siguiendo los patrones establecidos

### 2. **Validación de Formularios**
```csharp
// En código:
var validationRules = FormValidationRulesBuilder.Create<Entity>()
    .AddRule(x => x.Name, ValidationRule.Required().WithMessage("El nombre es requerido"))
    .AddRule(x => x.Name, ValidationRule.Length(3, 100))
    .AddRule(x => x.Email, ValidationRule.Email())
    .Build();
```

### 3. **Configuración de ViewManager**
```csharp
public class EntityViewManager : IViewManager<Entity>
{
    public QueryBuilder<Entity> GetDefaultQuery() => new QueryBuilder<Entity>()
        .Where(x => x.Active)
        .Include(x => x.Creator)
        .OrderBy(x => x.Name);
        
    public List<IViewConfiguration> GetViewConfigurations() => new()
    {
        new ViewConfiguration<Entity>
        {
            DisplayName = "Vista Principal",
            QueryBuilder = GetDefaultQuery(),
            ColumnConfigs = GetDefaultColumns()
        }
    };
}
```

### 4. **Gestión de Permisos**
- Usar nombres descriptivos: `AREA.MODULE.ACTION`
- Implementar wildcards para jerarquías
- Validar tanto en frontend como backend

### 5. **Optimización de Performance**
- Usar cache en Lookups cuando sea apropiado
- Configurar Select en QueryBuilder para traer solo columnas necesarias
- Implementar paginación en todas las listas grandes

---

## 🔍 TROUBLESHOOTING

### Problemas Comunes

1. **EntityTable no carga datos**
   - Verificar que el Service esté inyectado correctamente
   - Revisar que BaseQuery no tenga filtros inválidos
   - Comprobar permisos del usuario para la entidad

2. **Validaciones no funcionan**
   - Asegurar que FormValidator envuelva los ValidatedInput
   - Verificar que ValidationRules esté configurado correctamente
   - Revisar que FieldName coincida con la propiedad de la entidad

3. **Lookups lentos**
   - Habilitar cache si los datos no cambian frecuentemente
   - Optimizar SearchableFields para usar índices de DB
   - Considerar paginación server-side vs client-side

4. **Problemas de permisos**
   - Verificar configuración en ModularMenu
   - Comprobar que el backend valide los mismos permisos
   - Revisar logs de permisos en el backend

---

## 🚀 EXTENSIBILIDAD

### Crear Componentes Personalizados
```csharp
// Heredar de componentes base cuando sea posible
public partial class CustomEntityTable<T> : EntityTable<T> where T : class
{
    // Extensiones personalizadas
}
```

### Extender Servicios Base
```csharp
public class CustomApiService<T> : BaseApiService<T> where T : class
{
    public CustomApiService(HttpClient httpClient, string baseEndpoint) 
        : base(httpClient, baseEndpoint) { }
        
    // Métodos adicionales personalizados
    public async Task<ApiResponse<CustomResult>> CustomOperationAsync(CustomRequest request)
    {
        return await PostAsync<CustomResult>("custom-endpoint", request);
    }
}
```

### Configuraciones Avanzadas
- Configurar auto-refresh intervals globalmente
- Personalizar exports a Excel
- Configurar validaciones complejas cross-field
- Implementar workflows de aprobación

---

Esta documentación contiene todo lo necesario para entender, mantener y extender el sistema completo. Úsala como referencia para cualquier desarrollo o mantenimiento del sistema.