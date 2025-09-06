# 🔧 Entity Framework Interceptors

Sistema de interceptores para Entity Framework que permite agregar lógica personalizada a las operaciones `Add()`, `Update()` y `SaveChangesAsync()`.

## 🚀 Configuración Rápida

### **1. En Program.cs**
```csharp
using Backend.Utils.EFInterceptors.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Obtener connection string
var connectionString = Environment.GetEnvironmentVariable("SQL") 
    ?? throw new InvalidOperationException("SQL connection string not found");

// Registrar los interceptores
builder.Services.AddEFInterceptors(connectionString);

// Registrar handlers automáticamente desde el assembly
builder.Services.AddHandlersFromAssemblies(typeof(Program));
```

### **2. Usar en Controladores**
```csharp
public class CategoriaController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoriaController(AppDbContext context)
    {
        _context = context; // Automáticamente usa InterceptedAppDbContext
    }

    [HttpPost]
    public async Task<IActionResult> Create(Categoria categoria)
    {
        _context.Add(categoria);          // ← Interceptado automáticamente
        await _context.SaveChangesAsync(); // ← Interceptado automáticamente
        return Ok(categoria);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, Categoria categoria)
    {
        _context.Update(categoria);        // ← Interceptado automáticamente
        await _context.SaveChangesAsync(); // ← Interceptado automáticamente
        return Ok(categoria);
    }
}
```

---

## 🎯 Tipos de Handlers

### **AddHandler** - Intercepta `context.Add()`
```csharp
public class MyAddHandler : AddHandler
{
    public override async Task<bool> HandleAddAsync<T>(DbContext context, T entity)
    {
        // Tu lógica aquí ANTES de que se ejecute Add()
        
        // Retorna true para permitir, false para bloquear
        return true;
    }
}
```

### **UpdateHandler** - Intercepta `context.Update()`
```csharp
public class MyUpdateHandler : UpdateHandler
{
    public override async Task<bool> HandleUpdateAsync<T>(DbContext context, T entity, T originalEntity)
    {
        // Tu lógica aquí ANTES de que se ejecute Update()
        // originalEntity contiene los valores previos
        
        // Retorna true para permitir, false para bloquear
        return true;
    }
}
```

### **SaveHandler** - Intercepta `context.SaveChangesAsync()`
```csharp
public class MySaveHandler : SaveHandler
{
    public override async Task<bool> HandleBeforeSaveAsync(DbContext context)
    {
        // Tu lógica ANTES de guardar en la base de datos
        return true;
    }

    public override async Task<bool> HandleAfterSaveAsync(DbContext context, int affectedRows)
    {
        // Tu lógica DESPUÉS de guardar en la base de datos
        return true;
    }
}
```

---

## 📂 Estructura de Archivos

```
Backend.Utils/EFInterceptors/
├── Core/
│   ├── BaseHandler.cs              # Clases base abstractas
│   ├── EFInterceptorService.cs     # Servicio principal
│   └── InterceptedAppDbContext.cs  # DbContext con interceptores
├── Extensions/
│   └── DbContextExtensions.cs      # Extensiones para DI
└── Handlers/
    ├── AddHandlers/
    │   └── ExampleAddHandler.cs    # Ejemplo de AddHandler
    ├── UpdateHandlers/
    │   └── ExampleUpdateHandler.cs # Ejemplo de UpdateHandler
    └── SaveHandlers/
        └── ExampleSaveHandler.cs   # Ejemplo de SaveHandler
```

---

## 🔥 Ejemplos Prácticos

### **1. Handler de Auditoría**
```csharp
public class AuditAddHandler : AddHandler
{
    private readonly ILogger<AuditAddHandler> _logger;

    public AuditAddHandler(ILogger<AuditAddHandler> logger)
    {
        _logger = logger;
    }

    public override async Task<bool> HandleAddAsync<T>(DbContext context, T entity)
    {
        // Log de auditoría para nuevas entidades
        _logger.LogInformation($"Creating new {typeof(T).Name}: {JsonSerializer.Serialize(entity)}");
        
        // Puedes agregar campos automáticos como FechaCreacion, CreadorId, etc.
        if (entity is BaseEntity baseEntity)
        {
            baseEntity.FechaCreacion = DateTime.UtcNow;
            // baseEntity.CreadorId = GetCurrentUserId();
        }

        return true;
    }
}
```

### **2. Handler de Validación**
```csharp
public class ValidationUpdateHandler : UpdateHandler
{
    public override async Task<bool> HandleUpdateAsync<T>(DbContext context, T entity, T originalEntity)
    {
        // Validaciones personalizadas antes de actualizar
        if (entity is Categoria categoria)
        {
            if (string.IsNullOrEmpty(categoria.Nombre))
            {
                throw new ValidationException("El nombre de la categoría es requerido");
            }
            
            // Verificar si cambió el nombre y si ya existe
            if (originalEntity is Categoria original && 
                categoria.Nombre != original.Nombre)
            {
                var exists = await context.Set<Categoria>()
                    .AnyAsync(c => c.Nombre == categoria.Nombre && c.Id != categoria.Id);
                
                if (exists)
                {
                    throw new ValidationException("Ya existe una categoría con ese nombre");
                }
            }
        }

        return true;
    }
}
```

### **3. Handler de Cache Invalidation**
```csharp
public class CacheInvalidationSaveHandler : SaveHandler
{
    private readonly ICacheService _cacheService;

    public CacheInvalidationSaveHandler(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public override async Task<bool> HandleAfterSaveAsync(DbContext context, int affectedRows)
    {
        if (affectedRows > 0)
        {
            // Invalidar cache después de guardar cambios exitosamente
            await _cacheService.InvalidatePatternAsync("categoria:*");
            await _cacheService.InvalidatePatternAsync("product:*");
        }

        return true;
    }
}
```

---

## 🛠️ Registro Manual de Handlers

Si prefieres registrar handlers manualmente en lugar del auto-discovery:

```csharp
// En Program.cs
builder.Services.AddEFInterceptors(connectionString);

// Registrar handlers específicos
builder.Services.AddAddHandler<AuditAddHandler>();
builder.Services.AddUpdateHandler<ValidationUpdateHandler>();
builder.Services.AddSaveHandler<CacheInvalidationSaveHandler>();
```

---

## 📝 Flujo de Ejecución

```
Controller → context.Add(entity)
    ↓
1. ExampleAddHandler.HandleAddAsync() ← Tu lógica personalizada
    ↓
2. base.Add(entity) ← Operación original de EF
    ↓
Controller → context.SaveChangesAsync()
    ↓
3. ExampleSaveHandler.HandleBeforeSaveAsync() ← Tu lógica pre-save
    ↓
4. base.SaveChangesAsync() ← Guardado real en BD
    ↓
5. ExampleSaveHandler.HandleAfterSaveAsync() ← Tu lógica post-save
    ↓
Return affectedRows
```

---

## 🚨 Consideraciones Importantes

1. **Handlers sincrónicos**: Los interceptores usan `GetAwaiter().GetResult()` para handlers async en métodos sync
2. **Orden de ejecución**: Los handlers se ejecutan en el orden en que fueron registrados
3. **Manejo de errores**: Si un handler retorna `false` o lanza excepción, la operación se bloquea
4. **Performance**: Los interceptores agregan overhead mínimo, pero ten cuidado con lógica pesada
5. **Testing**: Los handlers son inyectados por DI, por lo que son fáciles de mockear en tests

---

## 🔍 Logging

El sistema incluye logging automático:

```csharp
// Program.cs
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug); // Para ver logs de interceptores
```

**Logs típicos:**
```
[12:34:56 INF] [ExampleAddHandler] Processing Add operation for entity type: Categoria
[12:34:56 INF] [ExampleSaveHandler] BeforeSave: 1 changes detected
[12:34:56 INF] [ExampleSaveHandler] AfterSave: Successfully saved 1 changes
```