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

### 📋 METODOLOGÍA DE TRABAJO

**IMPORTANTE: Antes de implementar cualquier tarea, SIEMPRE debo:**

1. **🔍 INVESTIGACIÓN TÉCNICA PREVIA (OBLIGATORIO)**
   - **Validar métodos de servicios existentes**: Usar Grep/Read para verificar qué métodos están disponibles en servicios que voy a usar
   - **Revisar firmas de métodos base**: Confirmar parámetros exactos de BaseQueryService, BaseApiService, etc.
   - **Verificar propiedades de componentes**: Comprobar qué propiedades están disponibles en componentes base
   - **Validar campos protegidos**: Revisar nombres exactos de campos como _baseUrl, _endpoint, etc.
   - **Comprobar dependencias**: Verificar qué servicios/interfaces están disponibles e inyectados

2. **📝 CREAR PLAN DETALLADO**
   - Explicar en alto nivel qué se va a hacer
   - Desglosar las tareas específicas paso a paso
   - Identificar archivos que se van a crear/modificar
   - Estimar complejidad y posibles dependencias

3. **⚙️ SOLICITAR CONFIGURACIÓN DEL USUARIO**
   - Confirmar el alcance del trabajo
   - Validar el nombre de entidades/módulos
   - Verificar permisos y área de la aplicación
   - Preguntar por requisitos específicos o personalizaciones

4. **✅ OBTENER APROBACIÓN**
   - Mostrar el plan completo al usuario
   - Esperar confirmación antes de proceder
   - Hacer ajustes si es necesario

**Ejemplo de Investigación Previa:**
```bash
# 1. VALIDAR MÉTODOS DE SERVICIOS EXISTENTES
grep -n "GetCurrentUser\|GetUser" Backend.Utils/Security/PermissionService.cs
find . -name "*PermissionService*" -type f | xargs grep -n "public.*Get"

# 2. REVISAR FIRMAS DE MÉTODOS BASE  
grep -n "CreateAsync.*SessionDataDto" Backend.Utils/Services/BaseQueryService.cs
grep -n "public.*CreateAsync" Backend.Utils/Services/BaseQueryService.cs

# 3. VERIFICAR PROPIEDADES DE COMPONENTES
grep -n "ApiEndpoint" Frontend/Components/Base/Tables/ViewConfiguration.cs
find Frontend/Components -name "*ViewConfiguration*" | xargs grep -n "public.*string"

# 4. VALIDAR CAMPOS PROTEGIDOS
grep -n "_endpoint\|_baseUrl" Frontend/Services/BaseApiService.cs
read Frontend/Services/BaseApiService.cs | head -30

# 5. COMPROBAR DEPENDENCIAS
find . -name "*Controller*" -type f | head -5 | xargs grep -n "ValidatePermissionAsync"
```

**Ejemplo de Plan:**
```
PLAN: Crear módulo SystemUsers

INVESTIGACIÓN PREVIA REALIZADA: ✅
- ✅ Confirmé que PermissionService tiene método GetUserAsync() con SessionDataDto
- ✅ Validé que BaseQueryService.CreateAsync() requiere SessionDataDto como parámetro
- ✅ Verifiqué que ViewConfiguration no tiene ApiEndpoint, se usa en EntityTable
- ✅ Confirmé que BaseApiService usa _baseUrl (no _endpoint)

ALTO NIVEL:
Vamos a crear un módulo completo para gestión de usuarios del sistema con CRUD completo, 
validaciones, permisos y integración con el sistema de autenticación existente.

TAREAS ESPECÍFICAS:
1. Crear entidad SystemUsers en Shared.Models
2. Crear controlador y servicio backend con herencia de BaseQuery
3. Crear servicio frontend heredando BaseApiService
4. Crear ViewManager con configuraciones de columnas
5. Crear componente List con EntityTable
6. Crear componente Formulario con validaciones
7. Crear componente Fast para creación rápida
8. Configurar permisos y navegación

CONFIGURACIÓN NECESARIA:
- ¿Qué campos específicos necesita la entidad SystemUsers?
- ¿Qué validaciones especiales requiere?
- ¿Qué permisos debe tener (ADMIN.SYSTEMUSERS.*)?
- ¿Alguna integración especial con autenticación?

¿Confirmas que proceda con este plan?
```

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

### API - Servicio HTTP Genérico Central
**Ubicación:** `Frontend/Services/API.cs`
**Propósito:** Servicio HTTP genérico centralizado con autenticación automática

#### Características Principales:
- ✅ **Autenticación automática:** Se inyecta AuthService y maneja tokens
- ✅ **Métodos múltiples:** GET, POST, PUT, DELETE con variantes
- ✅ **Tipos de respuesta flexibles:** String, ApiResponse<T>, T directo
- ✅ **Con/sin autenticación:** Variantes NoAuth para endpoints públicos
- ✅ **Manejo de archivos:** PostFileAsync/GetFileAsync para binarios
- ✅ **Procesamiento de respuestas:** Métodos helper para manejar ApiResponse<T>

#### Tipos de Métodos Disponibles:

**Para cada verbo HTTP (GET, POST, PUT, DELETE):**
```csharp
// Retorna string simple
Task<string> GetStringAsync(string endpoint)
Task<string> GetStringNoAuthAsync(string endpoint)

// Retorna ApiResponse<T> tipado
Task<ApiResponse<T>> GetAsync<T>(string endpoint)
Task<ApiResponse<T>> GetNoAuthAsync<T>(string endpoint)

// Retorna T directamente (sin wrapper)
Task<T?> GetDirectAsync<T>(string endpoint)
Task<T?> GetDirectNoAuthAsync<T>(string endpoint)
```

#### Métodos de Procesamiento de ApiResponse:
```csharp
// Procesamiento condicional
Task ProcessResponseAsync<T>(ApiResponse<T> response, Func<T, Task> onSuccess, Func<ApiResponse<T>, Task>? onError)
void ProcessResponse<T>(ApiResponse<T> response, Action<T> onSuccess, Action<ApiResponse<T>>? onError)

// Extracción de datos
T? GetDataOrDefault<T>(ApiResponse<T> response, T? defaultValue = default)
T GetDataOrThrow<T>(ApiResponse<T> response)

// Transformación
ApiResponse<TResult> TransformResponse<T, TResult>(ApiResponse<T> response, Func<T, TResult> transform)
ApiResponse<List<T>> CombineResponses<T>(params ApiResponse<T>[] responses)

// Helpers de estado
bool IsSuccessWithData<T>(ApiResponse<T> response)
bool HasErrors<T>(ApiResponse<T> response)
string GetErrorMessages<T>(ApiResponse<T> response)

// Acciones condicionales
ApiResponse<T> OnSuccess<T>(ApiResponse<T> response, Action<T> action)
ApiResponse<T> OnError<T>(ApiResponse<T> response, Action<ApiResponse<T>> action)
```

#### Ejemplo de Uso de API (RECOMENDADO):
```csharp
public partial class MyComponent : ComponentBase
{
    [Inject] private API API { get; set; } = null!;

    private async Task LoadDataAsync()
    {
        // ✅ OPCIÓN RECOMENDADA: ApiResponse<T> con manejo manual
        var response = await API.GetAsync<List<MyEntity>>("/api/myentities/all");
        if (response.Success && response.Data != null)
        {
            entities = response.Data;
            ShowNotification("Datos cargados exitosamente", NotificationSeverity.Success);
        }
        else
        {
            errorMessage = API.GetErrorMessages(response);
            ShowNotification($"Error: {errorMessage}", NotificationSeverity.Error);
        }
    }

    private async Task SaveEntityAsync()
    {
        // ✅ OPCIÓN RECOMENDADA: POST con ApiResponse<T>
        var response = await API.PostAsync<MyEntity>("/api/myentities/create", newEntity);
        
        // Usar ProcessResponse helper para código más limpio
        await API.ProcessResponseAsync(response,
            onSuccess: async savedEntity => 
            {
                entities.Add(savedEntity);
                ShowNotification("Entidad guardada exitosamente", NotificationSeverity.Success);
                await CloseDialog();
            },
            onError: async error => 
            {
                errorMessage = API.GetErrorMessages(error);
                ShowNotification($"Error al guardar: {errorMessage}", NotificationSeverity.Error);
            });
    }

    private async Task UpdateEntityAsync()
    {
        // ✅ OPCIÓN RECOMENDADA: PUT con ApiResponse<T>
        var response = await API.PutAsync<MyEntity>($"/api/myentities/update", entityToUpdate);
        
        // Validar respuesta y actuar
        if (API.IsSuccessWithData(response))
        {
            // Actualizar en lista local
            var index = entities.FindIndex(e => e.Id == response.Data!.Id);
            if (index >= 0) entities[index] = response.Data;
            
            ShowNotification("Entidad actualizada exitosamente", NotificationSeverity.Success);
        }
        else
        {
            ShowNotification(API.GetErrorMessages(response), NotificationSeverity.Error);
        }
    }
}
```

### BaseApiService<T>
**Ubicación:** `Frontend/Services/BaseApiService.cs`
**Propósito:** Cliente HTTP base especializado para entidades específicas (hereda de API)

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
    <!-- ✅ EJEMPLO ACTUALIZADO - ApiEndpoint para reglas de negocio personalizadas -->
    <EntityTable T="SystemPermissions"
                 ApiEndpoint="/api/admin/systempermission/view-filtered"  <!-- Endpoint personalizado -->
                 ApiService="@SystemPermissionService"                    <!-- Requerido para operaciones -->
                 BaseQuery="@currentView.QueryBuilder"                    <!-- Se combina con endpoint -->
                 ColumnConfigs="@currentView.ColumnConfigs"
                 OnEdit="@HandleEdit"
                 OnDelete="@HandleDelete"
                 ExcelFileName="SystemPermissions" />
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
                ColumnConfigs = new List<ColumnConfig<SystemPermissions>>
                {
                    new ColumnConfig<SystemPermissions>
                    {
                        Property = "ActionKey", 
                        Title = "Action Key", 
                        Width = "250px", 
                        Sortable = true, 
                        Filterable = true,
                        Order = 1
                    },
                    new ColumnConfig<SystemPermissions>
                    {
                        Property = "GrupoNombre", 
                        Title = "Grupo", 
                        Width = "150px", 
                        Sortable = true, 
                        Filterable = true,
                        Order = 2
                    },
                    new ColumnConfig<SystemPermissions>
                    {
                        Property = "Descripcion", 
                        Title = "Descripción", 
                        Width = "300px", 
                        Sortable = false, 
                        Filterable = true,
                        Order = 3
                    },
                    new ColumnConfig<SystemPermissions>
                    {
                        Property = "Organization.Nombre",
                        Title = "Organización",
                        Width = "200px",
                        Sortable = true,
                        Filterable = true,
                        Order = 4,
                        FormatExpression = p => p.Organization?.Nombre ?? "Global"  // ✅ Usar FormatExpression
                    }
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
    private readonly SystemPermissionService _systempermissionService;

    public SystemPermissionController(SystemPermissionService service, ILogger<SystemPermissionController> logger, IServiceProvider serviceProvider) 
        : base(service, logger, serviceProvider)
    {
        _systempermissionService = service;
    }
    
    /// <summary>
    /// ✅ EJEMPLO: Endpoint personalizado con reglas de negocio (Global + Mi Organización)
    /// </summary>
    [HttpPost("view-filtered")]
    public async Task<IActionResult> GetFilteredPermissions([FromBody] QueryRequest queryRequest)
    {
        var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
        if (errorResult != null) return errorResult;

        try
        {
            // ⚠️ CRÍTICO: Retornar PagedResult<T> para compatibilidad con EntityTable
            var result = await _systempermissionService.GetFilteredPermissionsPagedAsync(queryRequest, user);
            return Ok(ApiResponse<Shared.Models.QueryModels.PagedResult<SystemPermissions>>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener permisos filtrados");
            return StatusCode(500, ApiResponse<Shared.Models.QueryModels.PagedResult<SystemPermissions>>.ErrorResponse("Error interno del servidor"));
        }
    }
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

## 🚀 SISTEMA FORCE OPERATIONS - BYPASS DE FIELD PERMISSIONS

### **¿Cuándo Usar Force Operations?**

El Sistema Force permite **bypasear temporalmente** las validaciones de FieldPermission para casos específicos donde las operaciones automáticas del sistema necesitan acceso a campos protegidos:

#### **Escenarios de Uso Obligatorio:**

1. **✅ Procesos de Aprobación**: 
   - Juan aprueba una solicitud → El sistema debe actualizar automáticamente el sueldo
   - Maria autoriza un cambio → El sistema modifica campos que el usuario no puede tocar directamente

2. **✅ Importaciones/Migraciones**: 
   - Carga masiva de datos con permisos elevados del sistema
   - Sincronización de datos desde sistemas externos

3. **✅ Procesos Automáticos**: 
   - Workflows que actualizan campos protegidos como resultado de otras acciones
   - Cálculos automáticos que modifican campos sensibles

4. **✅ Reportes/Auditorías Especiales**: 
   - Consultas que necesitan ver todos los campos para análisis
   - Exports de datos completos para auditores

### **Force SaveChanges - Para Operaciones de Escritura**

```csharp
// ❌ Operación NORMAL - Aplica validaciones FieldPermission (puede fallar)
await _context.SaveChangesAsync();

// ✅ FORCE - Saltea validaciones FieldPermission
await _context.ForceSaveChangesAsync();
await _context.ForceSaveChangesAsync("Juan aprobó solicitud de aumento #12345");

// ✅ Versión síncrona también disponible
_context.ForceSaveChanges("Proceso automático de nómina");
```

### **Force Queries - Para Operaciones de Lectura**

```csharp
// ❌ Operación NORMAL - Oculta campos sin permisos VIEW
var empleados = await _context.Empleados.ToListAsync();

// ✅ FORCE - Muestra todos los campos sin importar permisos VIEW
var empleados = await _context.Empleados.ForceToListAsync();
var empleados = await _context.Empleados.ForceToListAsync("Reporte para auditoría");

// ✅ Otros métodos Force disponibles:
var empleado = await query.ForceFirstOrDefaultAsync("Validación proceso aprobación");
var count = await query.ForceCountAsync("Conteo para dashboard ejecutivo"); 
bool exists = await query.ForceAnyAsync("Verificación automática sistema");
var empleado = await query.ForceSingleOrDefaultAsync("Obtener empleado específico");
```

### **⭐ Ejemplo Completo: Sistema de Aprobaciones**

```csharp
/// <summary>
/// CASO DE USO: Juan (Jefe) aprueba solicitud de aumento
/// Juan tiene permisos SOLICITUD.APROBAR pero NO tiene EMPLEADO.SUELDOBASE.EDIT
/// El sistema debe poder actualizar el sueldo automáticamente tras la aprobación
/// </summary>
public async Task AprobarSolicitudAumentoAsync(Guid solicitudId, Guid usuarioAprobadorId)
{
    // 1. ✅ FORCE QUERY - Obtener datos completos para validación
    //    (puede incluir campos que el usuario no puede ver normalmente)
    var solicitud = await _context.SolicitudesAumento
        .Where(s => s.Id == solicitudId && s.Estado == "Pendiente")
        .ForceFirstOrDefaultAsync("Proceso de aprobación - obtener solicitud completa");
        
    if (solicitud == null) throw new InvalidOperationException("Solicitud no encontrada o ya procesada");
        
    var empleado = await _context.Empleados
        .Where(e => e.Id == solicitud.EmpleadoId)
        .ForceFirstOrDefaultAsync("Proceso de aprobación - obtener empleado completo");

    if (empleado == null) throw new InvalidOperationException("Empleado no encontrado");

    // 2. ✅ Validaciones de negocio (usando datos completos obtenidos con Force)
    if (solicitud.NuevoSueldo <= empleado.SueldoBase)
        throw new InvalidOperationException("El nuevo sueldo debe ser mayor al actual");
    
    if (solicitud.NuevoSueldo > empleado.SueldoBase * 1.5m)
        throw new InvalidOperationException("Aumento no puede ser mayor al 50%");

    // 3. ✅ Actualizar datos (algunos campos protegidos por FieldPermission)
    solicitud.Estado = "Aprobada";
    solicitud.AprobadoPor = usuarioAprobadorId;
    solicitud.FechaAprobacion = DateTime.UtcNow;
    
    // CAMPO PROTEGIDO: Normalmente requiere EMPLEADO.SUELDOBASE.EDIT
    empleado.SueldoBase = solicitud.NuevoSueldo;
    empleado.FechaUltimoCambioSueldo = DateTime.UtcNow;

    // 4. ✅ FORCE SAVE - Saltear validaciones FieldPermission para campos protegidos
    await _context.ForceSaveChangesAsync($"Aprobación automática solicitud #{solicitudId} por usuario {usuarioAprobadorId}");
    
    // 5. ✅ Log de auditoría
    _logger.LogInformation("Solicitud {SolicitudId} aprobada automáticamente. Sueldo actualizado de {SueldoAnterior} a {SueldoNuevo}", 
        solicitudId, solicitud.SueldoAnterior, solicitud.NuevoSueldo);
}
```

### **⚠️ Consideraciones de Seguridad y Auditoría**

```csharp
/// <summary>
/// PRINCIPIOS DE SEGURIDAD para Force Operations
/// </summary>

// ✅ 1. SIEMPRE documentar la razón del Force
await _context.ForceSaveChangesAsync("RAZÓN ESPECÍFICA Y CLARA");

// ✅ 2. Usar Force SOLO en métodos de negocio controlados (nunca desde controllers directamente)
// ❌ PROHIBIDO en Controllers:
[HttpPost("direct-update")]  
public async Task<IActionResult> BadExample()
{
    // ❌ NUNCA hacer Force directamente en controllers
    await _context.ForceSaveChangesAsync(); // ❌ Riesgo de seguridad
}

// ✅ CORRECTO en Services:
public class ApprovalService 
{
    public async Task ProcessApprovalAsync(Guid requestId, Guid approverId)
    {
        // ✅ Force dentro de lógica de negocio controlada
        await _context.ForceSaveChangesAsync($"Proceso aprobación {requestId} por {approverId}");
    }
}

// ✅ 3. Validar PRIMERO que la operación Force es legítima
public async Task UpdateSalaryAsync(Guid empleadoId, decimal nuevoSueldo, string justificacion)
{
    // VALIDACIONES DE NEGOCIO PRIMERO
    var empleado = await _context.Empleados.FindAsync(empleadoId);
    if (empleado == null) throw new ArgumentException("Empleado no existe");
    if (nuevoSueldo <= 0) throw new ArgumentException("Sueldo debe ser positivo");
    if (string.IsNullOrEmpty(justificacion)) throw new ArgumentException("Justificación requerida para Force");
    
    // Otras validaciones específicas del dominio
    await ValidateBusinessRulesAsync(empleado, nuevoSueldo);
    
    // SOLO después de validaciones, usar Force
    empleado.SueldoBase = nuevoSueldo;
    await _context.ForceSaveChangesAsync($"Actualización sueldo: {justificacion}");
}

// ✅ 4. Logging automático de operaciones Force
// El sistema automáticamente registra:
// 🚀 FORCE MODE: Saltando validaciones FieldPermission - Razón: Juan aprobó solicitud #12345
// 🚀 FORCE MODE: Saltando ocultación campos FieldPermission - Razón: Reporte auditoría mensual
```

### **📋 Guía de Implementación Force Operations**

#### **PASO 1: Identificar Necesidad de Force**
```csharp
// ✅ Preguntarse:
// - ¿Es una operación automática legítima del sistema?
// - ¿El usuario tiene permisos para INICIAR la acción pero no para los campos resultantes?
// - ¿Es para reportes/auditorías especiales?
// - ¿Hay alternativas sin usar Force?

// ❌ NO usar Force para:
// - Bypasear permisos por conveniencia
// - Operaciones iniciadas directamente por usuario final
// - Casos donde se puede dar el permiso correcto al usuario
```

#### **PASO 2: Implementar con Validaciones**
```csharp
public async Task ProcessWithForce(ProcessRequest request)
{
    // 1. Validar que el usuario puede INICIAR esta operación
    if (!await _permissionService.HasPermissionAsync(request.UserId, "PROCESS.APPROVE"))
        throw new UnauthorizedAccessException("No autorizado para aprobar");
    
    // 2. Validaciones de negocio completas
    await ValidateBusinessRules(request);
    
    // 3. Obtener datos con Force si es necesario
    var data = await _context.Entities
        .Where(conditions)
        .ForceToListAsync($"Proceso {request.Type} iniciado por {request.UserId}");
    
    // 4. Procesar con lógica de negocio
    foreach (var item in data)
    {
        // Aplicar cambios que requieren permisos especiales
        item.ProtectedField = CalculateNewValue(item);
    }
    
    // 5. Guardar con Force
    await _context.ForceSaveChangesAsync($"Proceso {request.Type} completado automáticamente");
}
```

#### **PASO 3: Testing de Force Operations**
```csharp
[Test]
public async Task Force_Operations_Should_Bypass_Field_Permissions()
{
    // Arrange: Usuario SIN permisos de campo
    var userWithoutFieldPermissions = CreateUserWithoutFieldPermissions();
    SetCurrentUser(userWithoutFieldPermissions);
    
    // Act: Operación Force
    var result = await _service.ProcessApprovalAsync(requestId, userId);
    
    // Assert: Debe completarse exitosamente a pesar de falta de permisos
    Assert.IsTrue(result.Success);
    Assert.AreEqual(expectedValue, entity.ProtectedField);
}

[Test]
public async Task Normal_Operations_Should_Respect_Field_Permissions()
{
    // Arrange: Usuario SIN permisos de campo
    var userWithoutFieldPermissions = CreateUserWithoutFieldPermissions();
    SetCurrentUser(userWithoutFieldPermissions);
    
    // Act & Assert: Operación normal debe fallar
    await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
        _context.SaveChangesAsync()); // Sin Force
}
```

### **🔍 Debugging Force Operations**

#### **Logs del Sistema Force**
```bash
# ✅ Logs exitosos - estos logs indican que Force está funcionando
🚀 FORCE MODE: Saltando validaciones FieldPermission - Razón: Juan aprobó solicitud #12345
🚀 FORCE MODE: Saltando ocultación campos FieldPermission - Razón: Reporte auditoría mensual

# ❌ Logs problemáticos - investigar si aparecen estos
🔒 CAMPO OMITIDO: Campo 'SueldoBase' omitido del UPDATE. Permiso requerido: EMPLEADO.SUELDOBASE.EDIT
🚨 ACCESO DENEGADO: No tiene permisos para actualizar campo 'SueldoBase'
```

#### **Troubleshooting Force Operations**
```csharp
// ❌ PROBLEMA: Force no funciona, sigue validando permisos
// ✅ SOLUCIÓN: Verificar que AsyncLocal<ForceOperationInfo> esté funcionando

// ❌ PROBLEMA: Force funciona pero no se registra en logs  
// ✅ SOLUCIÓN: Verificar configuración de logging en Program.cs

// ❌ PROBLEMA: Force causa errores de concurrencia
// ✅ SOLUCIÓN: Force es thread-safe con AsyncLocal, verificar otros issues

// ❌ PROBLEMA: Force se "filtra" a otras operaciones
// ✅ SOLUCIÓN: ForceOperationContext se limpia automáticamente, verificar flujo de código
```

### **📝 Documentación Obligatoria**

Cuando implementes Force Operations, SIEMPRE documentar:

```csharp
/// <summary>
/// ⚠️ FORCE OPERATION: Este método usa Force para bypasear FieldPermissions
/// 
/// JUSTIFICACIÓN: Proceso de aprobación automática donde el usuario aprobador
/// tiene permisos para aprobar pero no para modificar campos resultantes directamente.
/// 
/// CAMPOS AFECTADOS: SueldoBase, FechaUltimoCambio (requieren EMPLEADO.SUELDOBASE.EDIT)
/// TRIGGER: Usuario con permiso SOLICITUD.APROBAR aprueba solicitud
/// AUDITORIA: Registrado en logs con ID de solicitud y usuario aprobador
/// 
/// VALIDACIONES PREVIAS:
/// - Usuario tiene permiso SOLICITUD.APROBAR
/// - Solicitud existe y está en estado Pendiente
/// - Nuevo sueldo cumple reglas de negocio (max 50% aumento)
/// </summary>
/// <param name="solicitudId">ID de la solicitud a aprobar</param>
/// <param name="usuarioAprobadorId">ID del usuario que aprueba</param>
public async Task AprobarSolicitudAumentoAsync(Guid solicitudId, Guid usuarioAprobadorId)
{
    // Implementation with Force...
}
```

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

### 1. **Inyección de Servicios en Componentes**

**Servicios Principales a Inyectar:**
```csharp
// En componentes (.razor.cs)
[Inject] private API API { get; set; } = null!;                          // Servicio HTTP genérico
[Inject] private NavigationManager Navigation { get; set; } = null!;      // Navegación
[Inject] private DialogService DialogService { get; set; } = null!;       // Dialogs de Radzen
[Inject] private NotificationService NotificationService { get; set; } = null!; // Notificaciones
[Inject] private QueryService QueryService { get; set; } = null!;         // Constructor de queries

// Servicios específicos de entidad (opcional, alternativa a API)
[Inject] private SystemPermissionService SystemPermissionService { get; set; } = null!;
```

**PRIORIDAD: Usar API genérico > Servicios específicos**
- **✅ PREFERIR:** `API.GetAsync<T>("/endpoint")` con ApiResponse<T>
- **⚠️ ALTERNATIVA:** `EntityService.GetAsync()` (solo si necesitas métodos muy específicos)

**PRIORIDAD en Métodos de API:**
1. **🥇 PRIMERA OPCIÓN - Métodos con ApiResponse<T>:**
   ```csharp
   Task<ApiResponse<T>> GetAsync<T>(string endpoint)
   Task<ApiResponse<T>> PostAsync<T>(string endpoint, object? data = null)
   Task<ApiResponse<T>> PutAsync<T>(string endpoint, object? data = null)
   Task<ApiResponse<T>> DeleteAsync<T>(string endpoint)
   ```

2. **🥈 SEGUNDA OPCIÓN - Métodos directos (solo si es necesario):**
   ```csharp
   Task<T?> GetDirectAsync<T>(string endpoint)
   Task<T?> PostDirectAsync<T>(string endpoint, object? data = null)
   ```

3. **🥉 TERCERA OPCIÓN - Métodos string (casos muy específicos):**
   ```csharp
   Task<string> GetStringAsync(string endpoint)
   Task<string> PostStringAsync(string endpoint, object? data = null)
   ```

**¿Por qué priorizar ApiResponse<T>?**
- ✅ Manejo consistente de errores
- ✅ Información detallada de éxito/fallo
- ✅ Mensajes de error estructurados
- ✅ Integración con helpers de procesamiento

### 🚫 PROHIBICIÓN ABSOLUTA: NO USAR `dynamic`

**❌ PROHIBIDO - Jamás usar `dynamic`:**
```csharp
// ❌ NUNCA HACER ESTO
var response = await API.PostAsync<dynamic>("/api/endpoint", data);
dynamic result = response.Data;
var value = result.SomeProperty; // ❌ Sin tipado fuerte

// ❌ NUNCA HACER ESTO
public async Task<dynamic> GetDataAsync() { }
var data = await GetDataAsync();
```

**✅ OBLIGATORIO - Siempre usar Modelos tipados:**
```csharp
// ✅ CORRECTO - Usar modelo específico
var response = await API.PostAsync<MyEntity>("/api/endpoint", data);
MyEntity result = response.Data;
var value = result.SomeProperty; // ✅ Tipado fuerte

// ✅ CORRECTO - Definir modelos para respuestas
public class ApiResponseModel
{
    public string Name { get; set; }
    public int Count { get; set; }
    public DateTime Date { get; set; }
}

var response = await API.GetAsync<ApiResponseModel>("/api/endpoint");
```

**Razones por las que `dynamic` está PROHIBIDO:**
- ❌ **Sin IntelliSense:** No hay autocompletado de propiedades
- ❌ **Sin validación en compilación:** Errores solo en runtime
- ❌ **Difícil debugging:** No se puede inspeccionar fácilmente
- ❌ **Sin documentación:** No se sabe qué propiedades existen
- ❌ **Mantenimiento complejo:** Cambios causan errores ocultos
- ❌ **Sin refactoring seguro:** Renombrar propiedades no actualiza referencias

**Alternativas correctas:**
1. **Crear modelos específicos** en `Shared.Models/`
2. **Usar clases parciales** si el modelo es muy grande
3. **Usar records** para datos simples de solo lectura
4. **Usar DTOs** para transferencia de datos específica

### 2. **Creación de Nuevos Módulos**
1. Crear entidad en `Shared.Models/Entities/`
2. Crear controlador y servicio backend heredando de `BaseQueryController<T>` y `BaseQueryService<T>`
3. Crear servicio frontend heredando de `BaseApiService<T>`
4. Crear ViewManager implementando `IViewManager<T>`
5. Crear componentes List, Formulario y Fast siguiendo los patrones establecidos

### 3. **Validación de Formularios**
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

## 🔧 REFERENCIA TÉCNICA CRÍTICA

### **Firmas Exactas de Servicios Base (OBLIGATORIO CONSULTAR)**

#### **BaseQueryService<T> - Firmas Reales:**
```csharp
// ✅ CORRECTO - Todos requieren SessionDataDto
public virtual async Task<T> CreateAsync(CreateRequest<T> request, SessionDataDto sessionData)
public virtual async Task<T> UpdateAsync(UpdateRequest<T> request, SessionDataDto sessionData)
public virtual async Task<List<T>> GetAllUnpagedAsync(SessionDataDto sessionData)
public virtual async Task<PagedResponse<T>> GetAllPagedAsync(int page, int pageSize, SessionDataDto sessionData)
public virtual async Task<T?> GetByIdAsync(Guid id, SessionDataDto sessionData)
public virtual async Task<bool> DeleteAsync(Guid id, SessionDataDto sessionData)

// ❌ INCORRECTO - Estos métodos NO existen sin SessionDataDto
// Task<T> CreateAsync(CreateRequest<T> request) // ❌ NO EXISTE
```

#### **BaseQueryController<T> - Método ValidatePermissionAsync:**
```csharp
// ✅ CORRECTO - Método disponible que retorna user, permission y errorResult
protected async Task<(SessionDataDto? user, bool hasPermission, IActionResult? errorResult)> ValidatePermissionAsync(string action)

// ✅ EJEMPLOS DE USO CORRECTO:
var (user, hasPermission, errorResult) = await ValidatePermissionAsync("create");
if (errorResult != null) return errorResult;
// user contiene SessionDataDto con OrganizationId

var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
if (errorResult != null) return errorResult;
```

#### **PermissionService - Métodos REALES Disponibles:**
```csharp
// ❌ ESTOS MÉTODOS NO EXISTEN:
// GetCurrentUserAsync() // ❌ NO EXISTE
// GetUserAsync() // ❌ NO EXISTE

// ✅ MÉTODOS QUE SÍ EXISTEN (verificar con Grep antes de usar):
public async Task<List<string>> GetUserPermissionsAsync(Guid userId, Guid organizationId)
// Otros métodos se deben verificar con: grep -n "public.*async" Backend.Utils/Security/PermissionService.cs
```

#### **BaseApiService<T> - Campos Protegidos:**
```csharp
// ✅ CORRECTO - Campos que SÍ existen:
protected readonly API _api;
protected readonly ILogger<BaseApiService<T>> _logger;
protected readonly string _baseUrl;  // ✅ Usar este, NO _endpoint

// ❌ INCORRECTO - Campos que NO existen:
// protected readonly string _endpoint;  // ❌ NO EXISTE
```

#### **EntityTable<T> - Propiedades Disponibles REALES:**
```csharp
// ✅ CORRECTO - Estas propiedades SÍ existen:
[Parameter] public string? ApiEndpoint { get; set; }         // ✅ AGREGADO - Para endpoints personalizados
[Parameter] public BaseApiService<T>? ApiService { get; set; } // ✅ CORRECTO - NO "Service"
[Parameter] public string ExcelFileName { get; set; } = "";   // ✅ CORRECTO - NO "ExportFileName"
[Parameter] public QueryBuilder<T>? BaseQuery { get; set; }   // ✅ Existe

// ❌ INCORRECTO - Estos parámetros NO existen:
// [Parameter] public BaseApiService<T>? Service { get; set; }     // ❌ Usar ApiService
// [Parameter] public string ExportFileName { get; set; }          // ❌ Usar ExcelFileName

// ⚠️ ERROR MUY COMÚN - SIEMPRE usar ApiService="@MiServicio" en EntityTable:
// ❌ FALTA: <EntityTable T="SystemUsers" ... />                           // Sin ApiService
// ✅ CORRECTO: <EntityTable T="SystemUsers" ApiService="@SystemUserService" ... />

// 📝 NOTA: ApiEndpoint se usa cuando necesitas un endpoint personalizado que mantenga
// todas las funciones de filtrado, ordenamiento y paginación de EntityTable
```

### **Patrón ApiEndpoint Personalizado - REGLAS DE NEGOCIO + FUNCIONALIDAD COMPLETA:**

**📋 Cuándo usar ApiEndpoint personalizado:**
- Necesitas aplicar reglas de negocio específicas (ej: Global + Mi Organización)
- Quieres mantener TODA la funcionalidad de EntityTable (filtros, ordenamiento, paginación, exportación)
- Necesitas lógica personalizada pero compatible con el sistema estándar

**⚠️ CRÍTICO: El backend DEBE retornar `PagedResult<T>` (no `PagedResponse<T>`) para compatibilidad con EntityTable.**

#### **Frontend - EntityTable con ApiEndpoint:**
```razor
<!-- ✅ CORRECTO - ApiEndpoint + ApiService para funcionalidad completa -->
<EntityTable T="SystemPermissions"
             ApiEndpoint="/api/admin/systempermission/view-filtered"  <!-- Endpoint personalizado -->
             ApiService="@SystemPermissionService"                    <!-- Requerido para otras operaciones -->
             BaseQuery="@currentView.QueryBuilder"                    <!-- Se combina con endpoint -->
             ExcelFileName="SystemPermissions" />
```

#### **Backend - Controller (Solo Validación + Delegación):**
```csharp
[HttpPost("view-filtered")]
public async Task<IActionResult> GetFilteredPermissions([FromBody] QueryRequest queryRequest)
{
    var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
    if (errorResult != null) return errorResult;

    try
    {
        // ⚠️ CRÍTICO: Retornar PagedResult<T> para compatibilidad con EntityTable
        var result = await _service.GetFilteredPermissionsPagedAsync(queryRequest, user);
        return Ok(ApiResponse<Shared.Models.QueryModels.PagedResult<T>>.SuccessResponse(result));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error en endpoint personalizado");
        return StatusCode(500, ApiResponse<Shared.Models.QueryModels.PagedResult<T>>.ErrorResponse("Error interno"));
    }
}
```

#### **Backend - Service (Toda la Lógica de Negocio):**
```csharp
/// <summary>
/// Método personalizado que aplica reglas de negocio específicas
/// DEBE retornar PagedResult<T> para compatibilidad con EntityTable
/// </summary>
public async Task<Shared.Models.QueryModels.PagedResult<T>> GetFilteredPermissionsPagedAsync(QueryRequest queryRequest, SessionDataDto sessionData)
{
    // REGLAS DE NEGOCIO PERSONALIZADAS
    var baseQuery = _dbSet.Where(p => p.OrganizationId == null || p.OrganizationId == sessionData.OrganizationId)
                          .Where(p => p.Active)
                          .Include(p => p.Organization);

    // Aplicar filtro adicional desde EntityTable (filtros de columna)
    IQueryable<T> filteredQuery = baseQuery;
    if (!string.IsNullOrEmpty(queryRequest.Filter))
    {
        try 
        {
            // System.Linq.Dynamic.Core procesa filtros automáticamente
            filteredQuery = filteredQuery.Where(queryRequest.Filter);
        }
        catch (Exception filterEx)
        {
            // Fallback manual para filtros específicos
            _logger.LogWarning(filterEx, "Filtro dinámico falló, aplicando fallback manual");
            // ... lógica de fallback
        }
    }

    // Aplicar ordenamiento
    if (!string.IsNullOrEmpty(queryRequest.OrderBy))
    {
        // Parsing de "Campo desc" o "Campo asc"
        var orderParts = queryRequest.OrderBy.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var fieldName = orderParts[0];
        var isDescending = orderParts.Length > 1 && orderParts[1].ToLower() == "desc";
        
        filteredQuery = isDescending 
            ? filteredQuery.OrderByDescending(p => EF.Property<object>(p, fieldName))
            : filteredQuery.OrderBy(p => EF.Property<object>(p, fieldName));
    }

    // Contar total antes de paginación
    var totalCount = await filteredQuery.CountAsync();

    // Aplicar paginación
    var skip = queryRequest.Skip ?? 0;
    var take = queryRequest.Take ?? 20;
    var data = await filteredQuery.Skip(skip).Take(take).ToListAsync();

    // ⚠️ CRÍTICO: Retornar PagedResult<T> (no PagedResponse<T>)
    return new Shared.Models.QueryModels.PagedResult<T>
    {
        Data = data,
        TotalCount = totalCount,
        Page = (skip / take) + 1,
        PageSize = take
    };
}
```

#### **Patrón Común: Global + Mi Organización:**
```csharp
/// <summary>
/// Patrón reutilizable para mostrar registros globales + mi organización
/// </summary>
protected IQueryable<T> ApplyGlobalPlusMyOrgFilter<T>(IQueryable<T> query, SessionDataDto sessionData) 
    where T : class
{
    // Usar reflection para encontrar OrganizationId property
    var organizationProperty = typeof(T).GetProperty("OrganizationId");
    if (organizationProperty != null)
    {
        return query.Where(x => 
            EF.Property<Guid?>(x, "OrganizationId") == null || 
            EF.Property<Guid?>(x, "OrganizationId") == sessionData.OrganizationId);
    }
    return query;
}
```

---

## 🔧 **FILTROS DINÁMICOS Y SYSTEM.LINQ.DYNAMIC.CORE**

### **ConvertRadzenFilterToString - Valores Directos (NO @0):**

**⚠️ PROBLEMA CRÍTICO:** El patrón original usaba `@0` que NO funciona desde frontend.
**✅ SOLUCIÓN:** Incluir valores directos en el filtro con escape de comillas.

#### **Frontend - EntityTable.Filters.cs:**
```csharp
private string ConvertRadzenFilterToString(FilterDescriptor filter)
{
    if (filter == null || string.IsNullOrEmpty(filter.Property)) 
        return string.Empty;

    var property = filter.Property;
    var value = filter.FilterValue?.ToString() ?? "";
    var escapedValue = value.Replace("\"", "\\\""); // ⚠️ CRÍTICO: Escapar comillas
    
    return filter.FilterOperator switch
    {
        // ✅ CORRECTO - Valores directos con escape
        FilterOperator.Contains => $"({property} != null && {property}.ToLower().Contains(\"{escapedValue.ToLower()}\"))",
        FilterOperator.Equals => $"{property} == \"{escapedValue}\"",
        FilterOperator.NotEquals => $"{property} != \"{escapedValue}\"",
        FilterOperator.StartsWith => $"({property} != null && {property}.ToLower().StartsWith(\"{escapedValue.ToLower()}\"))",
        FilterOperator.EndsWith => $"({property} != null && {property}.ToLower().EndsWith(\"{escapedValue.ToLower()}\"))",
        
        // Valores numéricos - usar valor directo sin comillas
        FilterOperator.GreaterThan => IsNumericValue(value) ? $"{property} > {value}" : $"{property} > \"{escapedValue}\"",
        FilterOperator.GreaterThanOrEquals => IsNumericValue(value) ? $"{property} >= {value}" : $"{property} >= \"{escapedValue}\"",
        FilterOperator.LessThan => IsNumericValue(value) ? $"{property} < {value}" : $"{property} < \"{escapedValue}\"",
        FilterOperator.LessThanOrEquals => IsNumericValue(value) ? $"{property} <= {value}" : $"{property} <= \"{escapedValue}\"",
        
        // Operadores sin valores
        FilterOperator.IsNull => $"{property} == null",
        FilterOperator.IsNotNull => $"{property} != null",
        FilterOperator.IsEmpty => $"string.IsNullOrEmpty({property})",
        FilterOperator.IsNotEmpty => $"!string.IsNullOrEmpty({property})",
        _ => string.Empty
    };
}
```

### **EntityTable.DataLoading - Filtros para ApiEndpoint Personalizado:**

**⚠️ PROBLEMA:** EntityTable NO enviaba filtros de columna cuando usaba ApiEndpoint personalizado.
**✅ SOLUCIÓN:** Combinar BaseQuery + Filtros de Columna.

#### **Frontend - EntityTable.DataLoading.cs:**
```csharp
// En la sección ApiEndpoint personalizado (línea ~41)
if (!string.IsNullOrEmpty(ApiEndpoint))
{
    var API = ServiceProvider.GetRequiredService<Frontend.Services.API>();
    
    var queryRequest = new QueryRequest
    {
        Skip = args.Skip ?? 0,
        Take = args.Top ?? PageSize,
        OrderBy = args.OrderBy
    };
    
    // ⚠️ CRÍTICO: Combinar TODOS los filtros
    var allFilters = new List<string>();
    
    // BaseQuery filters
    if (queryWithFilters != null)
    {
        var baseQueryRequest = queryWithFilters.ToQueryRequest();
        if (!string.IsNullOrEmpty(baseQueryRequest.Filter))
        {
            allFilters.Add($"({baseQueryRequest.Filter})");
        }
    }
    
    // ✅ NUEVO: Column filters (args.Filters) - ESTO FALTABA
    if (args.Filters != null && args.Filters.Any())
    {
        var columnFilters = args.Filters.Select(ConvertRadzenFilterToString).Where(f => !string.IsNullOrEmpty(f));
        foreach (var filter in columnFilters)
        {
            allFilters.Add($"({filter})");
        }
    }
    
    // Combinar todos los filtros
    if (allFilters.Any())
    {
        queryRequest.Filter = string.Join(" and ", allFilters);
    }
    
    // ⚠️ CRÍTICO: Esperar PagedResponse<T> del endpoint personalizado
    var response = await API.PostAsync<Shared.Models.Responses.PagedResponse<T>>(ApiEndpoint, queryRequest);
}
```

### **Backend - Procesamiento de Filtros Dinámicos:**

#### **System.Linq.Dynamic.Core Integration:**
```csharp
using System.Linq.Dynamic.Core; // ⚠️ CRÍTICO: Agregar este using

// En el service method
if (!string.IsNullOrEmpty(queryRequest.Filter))
{
    _logger.LogInformation("Aplicando filtro: {Filter}", queryRequest.Filter);
    try 
    {
        // ✅ System.Linq.Dynamic.Core procesa filtros automáticamente
        filteredQuery = filteredQuery.Where(queryRequest.Filter);
    }
    catch (Exception filterEx)
    {
        _logger.LogWarning(filterEx, "No se pudo aplicar filtro dinámico: {Filter}", queryRequest.Filter);
        
        // ✅ Fallback manual para filtros específicos
        if (queryRequest.Filter.Contains("ActionKey"))
        {
            var searchTerm = ExtractSearchTermFromFilter(queryRequest.Filter);
            if (!string.IsNullOrEmpty(searchTerm))
            {
                filteredQuery = filteredQuery.Where(p => 
                    p.ActionKey != null && p.ActionKey.ToLower().Contains(searchTerm.ToLower()));
            }
        }
    }
}
```

#### **Helper Method para Extraer Valores:**
```csharp
/// <summary>
/// Método helper para extraer término de búsqueda de filtros como "(Active == true) and (ActionKey.Contains("test"))"
/// </summary>
private string? ExtractSearchTermFromFilter(string filter)
{
    try
    {
        // Buscar patrón .Contains("valor")
        var containsMatch = System.Text.RegularExpressions.Regex.Match(filter, @"\.Contains\(""([^""]+)""\)");
        if (containsMatch.Success)
        {
            return containsMatch.Groups[1].Value;
        }

        // Buscar patrón == "valor"
        var equalsMatch = System.Text.RegularExpressions.Regex.Match(filter, @"==\s*""([^""]+)""");
        if (equalsMatch.Success)
        {
            return equalsMatch.Groups[1].Value;
        }
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Error extrayendo término de búsqueda del filtro: {Filter}", filter);
    }

    return null;
}
```

### **ColumnConfig: Template vs FormatExpression**

#### **FormatExpression - NUEVA Funcionalidad Tipada (MÁS SIMPLE):**
```csharp
// ✅ MEJOR OPCIÓN - FormatExpression para formateo simple de texto
new ColumnConfig<SystemPermissions>
{
    Property = "Organization.Nombre",
    Title = "Organización", 
    Width = "180px",
    Sortable = true,
    Filterable = true,
    Order = 4,
    FormatExpression = p => p.Organization?.Nombre ?? "Global" // ✅ Tipado fuerte, IntelliSense
}

// ✅ Otros ejemplos de FormatExpression
new ColumnConfig<SystemPermissions>
{
    Property = "FechaCreacion",
    Title = "Fecha Creación",
    FormatExpression = p => p.FechaCreacion?.ToString("dd/MM/yyyy HH:mm") ?? "N/A"
}

new ColumnConfig<SystemPermissions>
{
    Property = "Active", 
    Title = "Estado",
    FormatExpression = p => p.Active ? "Activo" : "Inactivo"
}
```

#### **Template CORRECTO para ColumnConfig (PARA HTML COMPLEJO):**
```csharp
// ✅ USAR Template solo cuando necesites HTML complejo (badges, links, botones)
new ColumnConfig<SystemPermissions>
{
    Property = "Organization.Nombre",
    Title = "Organización",
    Width = "180px",
    Template = permission => builder =>
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class",
            $"badge {(permission.Organization != null ? "badge-primary" : "badge-success")}");
        builder.AddContent(2, permission.Organization?.Nombre ?? "Global");
        builder.CloseElement();
    }
}
```

#### **Patrones de Template Comunes:**
```csharp
// ✅ Badge condicional
Template = entity => builder =>
{
    builder.OpenElement(0, "span");
    builder.AddAttribute(1, "class", $"badge {(entity.Active ? "badge-success" : "badge-danger")}");
    builder.AddContent(2, entity.Active ? "Activo" : "Inactivo");
    builder.CloseElement();
}

// ✅ Link con navegación
Template = entity => builder =>
{
    builder.OpenElement(0, "a");
    builder.AddAttribute(1, "href", $"/detalle/{entity.Id}");
    builder.AddAttribute(2, "class", "link-primary");
    builder.AddContent(3, entity.Nombre);
    builder.CloseElement();
}

// ✅ Formato de fecha
Template = entity => builder =>
{
    builder.OpenElement(0, "span");
    builder.AddContent(1, entity.FechaCreacion?.ToString("dd/MM/yyyy HH:mm") ?? "N/A");
    builder.CloseElement();
}
```

### **Patrones de Obtención de Usuario/Organización:**

#### **En Controllers (Backend):**
```csharp
// ✅ CORRECTO - Obtener usuario actual via ValidatePermissionAsync:
var (user, hasPermission, errorResult) = await ValidatePermissionAsync("create");
if (errorResult != null) return errorResult;

var organizationId = user?.OrganizationId;  // ✅ SessionDataDto tiene OrganizationId
```

#### **En Services Backend (si necesitas usuario):**
```csharp
// ✅ CORRECTO - Recibir SessionDataDto del controller:
public async Task<bool> ValidateActionKeyAsync(string actionKey, Guid? organizationId, Guid? excludeId = null)
{
    // Usar organizationId pasado como parámetro
    var query = _dbSet.Where(p => p.ActionKey == actionKey && 
                             (p.OrganizationId == null || p.OrganizationId == organizationId));
    
    if (excludeId.HasValue)
        query = query.Where(p => p.Id != excludeId.Value);
    
    return !await query.AnyAsync();
}
```

### **🚨 PRINCIPIOS FUNDAMENTALES (NUNCA ROMPER):**

#### **1. Controllers = Solo Validación + Delegación**
```csharp
// ✅ CORRECTO - Controller limpio
[HttpPost("my-endpoint")]
public async Task<IActionResult> MyEndpoint([FromBody] QueryRequest queryRequest)
{
    var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
    if (errorResult != null) return errorResult;
    
    // SOLO delegación - NO lógica de negocio
    var result = await _service.MyBusinessLogicAsync(queryRequest, user);
    return Ok(ApiResponse<T>.SuccessResponse(result));
}

// ❌ PROHIBIDO - Lógica en controller
public async Task<IActionResult> BadExample()
{
    var result = await _service.GetData();
    // ❌ NO hacer filtrado/validaciones/transformaciones aquí
    var filtered = result.Where(x => x.SomeCondition);
    return Ok(filtered);  // ❌ Esta lógica debe estar en service
}
```

#### **2. Services = Toda la Lógica de Negocio**
```csharp
// ✅ CORRECTO - Service con lógica reutilizable
public async Task<PagedResponse<T>> MyBusinessLogicAsync(QueryRequest queryRequest, SessionDataDto sessionData)
{
    // TODA la lógica de negocio aquí - puede llamarse desde otros lugares
    var baseQuery = _dbSet.Where(x => x.OrganizationId == sessionData.OrganizationId)
                          .Where(x => x.Active)
                          .Include(x => x.Relations);
    
    return await QueryPagedAsync(queryRequest, sessionData, baseQuery);
}
```

#### **3. Permisos: "*" para Sin Restricciones**
```csharp
// ✅ Para endpoints públicos o sin restricciones
var (user, hasPermission, errorResult) = await ValidatePermissionAsync("*");

// ✅ Para endpoints con permisos específicos  
var (user, hasPermission, errorResult) = await ValidatePermissionAsync("view");
```

#### **4. Parámetros: Solo QueryRequest + SessionDataDto**
```csharp
// ✅ CORRECTO - Pasar user completo, NO extraer campos
public async Task<PagedResponse<T>> MyMethodAsync(QueryRequest queryRequest, SessionDataDto sessionData)
{
    // Usar sessionData.OrganizationId dentro del método
}

// ❌ PROHIBIDO - Extraer campos en controller
public async Task<PagedResponse<T>> BadMethodAsync(QueryRequest queryRequest, Guid organizationId)
{
    // ❌ NO hacer esto - pasar SessionDataDto completo
}
```

### **Checklist de Validación Antes de Implementar:**

**Backend Controllers:**
- [ ] ¿Estoy usando `ValidatePermissionAsync()` para obtener usuario?
- [ ] ¿Estoy pasando SOLO `QueryRequest` y `SessionDataDto` al service?
- [ ] ¿NO tengo lógica de negocio en el controller? (debe estar en service)
- [ ] ¿Uso `"*"` para endpoints sin restricciones de permisos?

**Backend Services:**  
- [ ] ¿TODA mi lógica de negocio está en el service (no en controller)?
- [ ] ¿Mis métodos reciben `QueryRequest` y `SessionDataDto` como parámetros?
- [ ] ¿Estoy heredando correctamente de `BaseQueryService<T>`?
- [ ] ¿Mis métodos pueden reutilizarse desde otros lugares?

**Frontend Services:**
- [ ] ¿Estoy usando `_baseUrl` y no `_endpoint`?
- [ ] ¿Estoy heredando de `BaseApiService<T>` correctamente?

**Frontend Components:**
- [ ] ¿Estoy usando `ApiService` (no `Service`) en EntityTable?
- [ ] ¿Estoy usando `ExcelFileName` (no `ExportFileName`) para nombres de archivos Excel?
- [ ] ¿Para formatear texto uso `FormatExpression` en lugar de `Template` cuando es posible?
- [ ] ¿Si uso Template en ColumnConfig, estoy usando RenderFragment con builder pattern?
- [ ] ¿Estoy inyectando servicios con nombres correctos?

**ApiEndpoint Personalizado:**
- [ ] ¿Mi backend retorna `PagedResult<T>` (NO `PagedResponse<T>`) para compatibilidad con EntityTable?
- [ ] ¿EntityTable tiene tanto `ApiEndpoint` como `ApiService` configurados?
- [ ] ¿Mi controller solo valida permisos y delega al service (sin lógica de negocio)?
- [ ] ¿ConvertRadzenFilterToString incluye valores directos (no @0)?
- [ ] ¿EntityTable.DataLoading combina BaseQuery + filtros de columna para ApiEndpoint?

**Filtros Dinámicos:**
- [ ] ¿Agregué `using System.Linq.Dynamic.Core;` en el service backend?
- [ ] ¿Mi service tiene try-catch para filtros dinámicos con fallback manual?
- [ ] ¿ConvertRadzenFilterToString escapa comillas correctamente?
- [ ] ¿Los valores numéricos no llevan comillas en los filtros?

---

## 🔍 TROUBLESHOOTING

### Problemas Comunes

0. **🚨 ERROR CRÍTICO MUY COMÚN: ApiService faltante en EntityTable**
   - ❌ Error: EntityTable no carga datos, no se muestran filas
   - ❌ Error: `Cannot invoke ApiService.GetAllPagedAsync because ApiService is null`
   - ❌ Código problemático: `<EntityTable T="SystemUsers" BaseQuery="..." />` (SIN ApiService)
   - ✅ Solución: **SIEMPRE** agregar `ApiService="@MiServicio"` en EntityTable
   - ✅ Código correcto: `<EntityTable T="SystemUsers" ApiService="@SystemUserService" BaseQuery="..." />`
   - ✅ Verificar: El servicio debe estar inyectado con `[Inject] private SystemUserService SystemUserService { get; set; } = null!;`

1. **Errores de Compilación por Métodos Inexistentes**
   - ❌ Error: `'PermissionService' does not contain a definition for 'GetCurrentUserAsync'`
   - ✅ Solución: Usar `ValidatePermissionAsync()` en controllers para obtener usuario
   - ✅ Verificar: `grep -n "GetCurrentUser" Backend.Utils/Security/PermissionService.cs`

2. **Errores de SessionDataDto Faltante**
   - ❌ Error: `There is no argument given that corresponds to the required parameter 'sessionData'`
   - ✅ Solución: Todos los métodos de BaseQueryService requieren SessionDataDto
   - ✅ Obtener de: `var (user, hasPermission, errorResult) = await ValidatePermissionAsync("create");`

3. **Errores de Parámetros Incorrectos en EntityTable**
   - ❌ Error: `'EntityTable' does not contain a definition for 'Service'`
   - ✅ Solución: Usar `ApiService` en lugar de `Service`
   - ❌ Error: `'EntityTable' does not contain a definition for 'ExportFileName'`
   - ✅ Solución: Usar `ExcelFileName` en lugar de `ExportFileName`
   - ✅ Verificar: `grep -n "ApiService\|ExcelFileName" Frontend/Components/Base/Tables/EntityTable.razor.cs`

4. **Errores de Campos Protegidos**
   - ❌ Error: `The name '_endpoint' does not exist in the current context`
   - ✅ Solución: Usar `_baseUrl` en lugar de `_endpoint` en BaseApiService
   - ✅ Verificar: `grep -n "_baseUrl\|_endpoint" Frontend/Services/BaseApiService.cs`

5. **Errores de Template en ColumnConfig**
   - ❌ Error: `Cannot convert lambda expression to type 'RenderFragment<T>'`
   - ✅ Solución: Usar pattern `entity => builder =>` con RenderTreeBuilder
   - ❌ Error: Intentar usar `@Html.Raw` o markup directo en Template
   - ✅ Solución: Usar `builder.OpenElement()`, `builder.AddAttribute()`, `builder.AddContent()`, `builder.CloseElement()`
   - ✅ Verificar: El Template debe ser `Func<T, RenderFragment>` no string HTML

5. **ApiEndpoint Personalizado - Errores de Tipo de Retorno**
   - ❌ Error: EntityTable no procesa datos del endpoint personalizado
   - ✅ Solución: Backend debe retornar `PagedResult<T>` (no `PagedResponse<T>`)
   - ✅ Verificar: El controller debe usar `ApiResponse<PagedResult<T>>.SuccessResponse(result)`

6. **Filtros de Columna No Funcionan con ApiEndpoint**
   - ❌ Error: Solo se envía `(Active == true)` pero no filtros de columna  
   - ✅ Solución: EntityTable.DataLoading debe combinar BaseQuery + args.Filters
   - ✅ Verificar: `ConvertRadzenFilterToString` debe usar valores directos (no @0)

7. **System.Linq.Dynamic.Core - Filtros Fallan**
   - ❌ Error: `Translation of 'EF.Property<object>...Contains("valor")' failed`
   - ✅ Solución: Agregar `using System.Linq.Dynamic.Core;` y try-catch con fallback
   - ✅ Verificar: Instalar paquete NuGet `System.Linq.Dynamic.Core`

8. **EntityTable no carga datos**
   - Verificar que el Service esté inyectado correctamente
   - Revisar que BaseQuery no tenga filtros inválidos
   - Comprobar permisos del usuario para la entidad

6. **Validaciones no funcionan**
   - Asegurar que FormValidator envuelva los ValidatedInput
   - Verificar que ValidationRules esté configurado correctamente
   - Revisar que FieldName coincida con la propiedad de la entidad

7. **Lookups lentos**
   - Habilitar cache si los datos no cambian frecuentemente
   - Optimizar SearchableFields para usar índices de DB
   - Considerar paginación server-side vs client-side

8. **Problemas de permisos**
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

## Flujo de Comunicación con Base de Datos

Siempre seguir esta estructura estricta para el flujo de datos:

```
.razor → Service Frontend → Controller → Service Backend → ContextDB
```

### Reutilización de Código
- SIEMPRE verificar si existen servicios, controladores o componentes similares antes de crear nuevos
- Reutilizar código existente cuando sea posible
- Extender funcionalidades en lugar de duplicar código

## Organización de Frontend

### Estructura de Carpetas
- **Components/**: Crear esta carpeta para cualquier componente adicional necesario
- **Components/Modals/**: TODOS los modales deben ir aquí (.razor + .razor.cs)

### Separación de Lógica e Interfaz
- **OBLIGATORIO**: Separar siempre la interfaz de la lógica
- Cada componente .razor DEBE tener su correspondiente .razor.cs
- Nunca mezclar lógica compleja en el archivo .razor

### Prioridades de UI

1. **RADZEN** - Prioridad absoluta, usar siempre que sea posible
2. **NUNCA usar Bootstrap** - No está instalado en el proyecto
3. **CSS/JS personalizado** - Solo en casos muy puntuales:
   - SIEMPRE preguntar al usuario antes de usar CSS o JS personalizado
   - Requerir confirmación explícita del usuario
   - Documentar la razón del uso excepcional

## Servicios Backend

### Manejo de Servicios Grandes
Cuando un Service sea demasiado grande, dividir en **Partial Classes**:

```csharp
// Ejemplo de división
Service[Entidad].Logica1.cs
Service[Entidad].Logica2.cs
Service[Entidad].Consultas.cs
```

### Beneficios de la División
- Archivos más pequeños y manejables
- Mejor organización del código
- Fácil mantenimiento
- Separación clara de responsabilidades

## Patrones a Seguir

### Controladores
- Heredar de `BaseQueryController` cuando sea aplicable
- Usar atributos de autorización apropiados
- Mantener lógica mínima, delegar a servicios

### Servicios Frontend
- Heredar de `BaseApiService` cuando sea aplicable
- Implementar cache cuando sea necesario
- Usar el patrón async/await

### Componentes Razor
- Usar `AuthorizePermission` para control de acceso
- Implementar validación usando `FormValidator`
- Seguir patrones existentes del proyecto

## Validaciones y Seguridad

- Usar `ValidationContext` para validaciones complejas
- Implementar permisos usando `PermisoAttribute`
- Nunca exponer datos sensibles en el frontend
- Validar tanto en frontend como backend

## Convenciones de Naming

### Archivos
- Servicios: `[Entidad]Service.cs`
- Controladores: `[Entidad]Controller.cs`
- Componentes: `[Entidad][Tipo].razor` + `[Entidad][Tipo].razor.cs`
- Modales: `[Nombre]Modal.razor` + `[Nombre]Modal.razor.cs`

### Variables y Métodos
- PascalCase para métodos públicos
- camelCase para variables locales
- Nombres descriptivos y claros

## Mejores Prácticas

1. **Consistencia**: Seguir patrones existentes en el proyecto
2. **Documentación**: Documentar métodos complejos y APIs
3. **Testing**: Verificar funcionalidad antes de finalizar
4. **Performance**: Considerar impacto en rendimiento
5. **Mantenibilidad**: Código limpio y bien estructurado

## Herramientas del Proyecto

Utilizar las herramientas disponibles en `/tools/`:
- `entity-generator.py` para generar entidades
- `permissions_generator.py` para permisos
- `generate_menu.py` para menús

## Recordatorios Importantes

- ✅ Siempre usar Radzen
- ❌ Nunca usar Bootstrap
- ⚠️ CSS/JS solo con aprobación del usuario
- 📁 Componentes en carpeta Components/
- 🔄 Separar .razor y .razor.cs
- 📚 Reutilizar código existente
- 🔧 Dividir servicios grandes en partial classes