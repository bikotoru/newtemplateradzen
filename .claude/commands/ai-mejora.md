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

// 📝 NOTA: ApiEndpoint se usa cuando necesitas un endpoint personalizado que mantenga
// todas las funciones de filtrado, ordenamiento y paginación de EntityTable
```

### **Patrón de ApiEndpoint Personalizado:**

#### **Cuándo usar ApiEndpoint:**
```razor
<!-- ✅ Usar ApiEndpoint cuando necesitas lógica personalizada pero mantienes funcionalidad completa -->
<EntityTable T="MyEntity"
             ApiEndpoint="/api/custom/my-filtered-endpoint"
             ApiService="@MyEntityService"  <!-- Requerido para otras operaciones -->
             BaseQuery="@currentView.QueryBuilder"  <!-- Se combina con endpoint -->
             ExcelFileName="CustomExport" />
```

#### **Implementación Backend para ApiEndpoint:**
```csharp
// ✅ CORRECTO - Controller SIN lógica de negocio, solo validación y delegación
[HttpPost("custom-filtered-view")]
public async Task<IActionResult> GetCustomFilteredView([FromBody] QueryRequest queryRequest)
{
    // "*" para endpoints sin restricciones, "view" para permisos específicos
    var (user, hasPermission, errorResult) = await ValidatePermissionAsync("*");
    if (errorResult != null) return errorResult;

    try
    {
        // SOLO pasar QueryRequest y SessionDataDto - NO lógica de negocio aquí
        var result = await _service.GetCustomFilteredPagedAsync(queryRequest, user);
        return Ok(ApiResponse<PagedResponse<MyEntity>>.SuccessResponse(result));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in custom endpoint");
        return StatusCode(500, ApiResponse<PagedResponse<MyEntity>>.ErrorResponse("Error interno"));
    }
}
```

#### **Implementación Service Backend:**
```csharp
// ✅ CORRECTO - TODA la lógica de negocio en el service para reutilización
public async Task<PagedResponse<MyEntity>> GetCustomFilteredPagedAsync(QueryRequest queryRequest, SessionDataDto sessionData)
{
    // TODA la lógica de filtros de negocio aquí - puede reutilizarse
    var baseQuery = _dbSet.Where(x => x.OrganizationId == sessionData.OrganizationId || x.OrganizationId == null)
                          .Where(x => x.Active)  // Otros filtros de negocio
                          .Include(x => x.RelatedEntity);
    
    // QueryPagedAsync maneja filtros, ordenamiento, paginación automáticamente
    return await QueryPagedAsync(queryRequest, sessionData, baseQuery);
}
```

### **ColumnConfig Templates con RenderFragment:**

#### **Template CORRECTO para ColumnConfig:**
```csharp
// ✅ CORRECTO - Template usa RenderFragment con builder pattern
new ColumnConfig<Shared.Models.Entities.SystemEntities.SystemPermissions>
{
    Property = "Organization.Nombre",
    Title = "Organización",
    Width = "180px",
    Sortable = true,
    Filterable = true,
    TextAlign = TextAlign.Left,
    Visible = true,
    Order = 4,
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
- [ ] ¿Si uso `ApiEndpoint`, mi backend devuelve `PagedResponse<T>` con la misma estructura que query estándar?
- [ ] ¿Si uso Template en ColumnConfig, estoy usando RenderFragment con builder pattern?
- [ ] ¿Estoy inyectando servicios con nombres correctos?

---

## 🔍 TROUBLESHOOTING

### Problemas Comunes

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

5. **EntityTable no carga datos**
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

Esta documentación contiene todo lo necesario para entender, mantener y extender el sistema completo. Úsala como referencia para cualquier desarrollo o mantenimiento del sistema.