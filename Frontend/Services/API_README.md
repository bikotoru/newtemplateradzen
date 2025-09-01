# Servicio API

El servicio `API` es un servicio genérico para realizar peticiones HTTP con diferentes tipos de retorno y soporte para métodos autenticados y no autenticados.

## Archivos del Servicio

- **`Frontend/Services/API.cs`** - Servicio principal
- **`Frontend.Tests/Services/APITests.cs`** - Tests completos del servicio

## Características

- ✅ **Métodos HTTP**: GET, POST, PUT, DELETE
- ✅ **Tres tipos de retorno**: `string`, `ApiResponse<T>`, `T` directo
- ✅ **Autenticación automática**: Versiones autenticadas y no autenticadas
- ✅ **Deserialización case-insensitive**: Funciona con cualquier formato de JSON
- ✅ **Manejo de errores**: Captura excepciones de red y errores HTTP
- ✅ **Token automático**: Usa el token de `AuthService.Session.Token`
- ✅ **Header Authorization**: Envía el token en el header `Authorization`

## Configuración

El servicio se registra automáticamente en `Program.cs`:

```csharp
builder.Services.AddScoped<API>();
```

## Métodos Disponibles

### GET

| Método | Retorno | Autenticación | Descripción |
|--------|---------|---------------|-------------|
| `GetStringAsync` | `string` | ✅ | Retorna respuesta como string |
| `GetStringNoAuthAsync` | `string` | ❌ | Retorna respuesta como string sin auth |
| `GetAsync<T>` | `ApiResponse<T>` | ✅ | Retorna ApiResponse con datos |
| `GetNoAuthAsync<T>` | `ApiResponse<T>` | ❌ | Retorna ApiResponse sin auth |
| `GetDirectAsync<T>` | `T?` | ✅ | Retorna objeto directamente |
| `GetDirectNoAuthAsync<T>` | `T?` | ❌ | Retorna objeto sin auth |

### POST

| Método | Retorno | Autenticación | Descripción |
|--------|---------|---------------|-------------|
| `PostStringAsync` | `string` | ✅ | Envía datos y retorna string |
| `PostStringNoAuthAsync` | `string` | ❌ | Envía datos sin auth |
| `PostAsync<T>` | `ApiResponse<T>` | ✅ | Envía datos y retorna ApiResponse |
| `PostNoAuthAsync<T>` | `ApiResponse<T>` | ❌ | Envía datos sin auth |
| `PostDirectAsync<T>` | `T?` | ✅ | Envía datos y retorna objeto |
| `PostDirectNoAuthAsync<T>` | `T?` | ❌ | Envía datos sin auth |

### PUT

| Método | Retorno | Autenticación | Descripción |
|--------|---------|---------------|-------------|
| `PutStringAsync` | `string` | ✅ | Actualiza datos y retorna string |
| `PutStringNoAuthAsync` | `string` | ❌ | Actualiza datos sin auth |
| `PutAsync<T>` | `ApiResponse<T>` | ✅ | Actualiza datos y retorna ApiResponse |
| `PutNoAuthAsync<T>` | `ApiResponse<T>` | ❌ | Actualiza datos sin auth |
| `PutDirectAsync<T>` | `T?` | ✅ | Actualiza datos y retorna objeto |
| `PutDirectNoAuthAsync<T>` | `T?` | ❌ | Actualiza datos sin auth |

### DELETE

| Método | Retorno | Autenticación | Descripción |
|--------|---------|---------------|-------------|
| `DeleteStringAsync` | `string` | ✅ | Elimina recurso y retorna string |
| `DeleteStringNoAuthAsync` | `string` | ❌ | Elimina recurso sin auth |
| `DeleteAsync<T>` | `ApiResponse<T>` | ✅ | Elimina recurso y retorna ApiResponse |
| `DeleteNoAuthAsync<T>` | `ApiResponse<T>` | ❌ | Elimina recurso sin auth |
| `DeleteDirectAsync<T>` | `T?` | ✅ | Elimina recurso y retorna objeto |
| `DeleteDirectNoAuthAsync<T>` | `T?` | ❌ | Elimina recurso sin auth |

## Ejemplos de Uso

### Inyección del servicio

```csharp
@inject API Api

// O en constructor
public class MiComponente
{
    private readonly API _api;
    
    public MiComponente(API api)
    {
        _api = api;
    }
}
```

### GET - Obtener datos

```csharp
// Retorno directo del objeto
var categoria = await _api.GetDirectAsync<Categoria>("/api/categorias/123");
if (categoria != null)
{
    Console.WriteLine($"Nombre: {categoria.Nombre}");
}

// Con ApiResponse para mejor manejo de errores
var response = await _api.GetAsync<List<Categoria>>("/api/categorias");
if (response.Success)
{
    foreach (var cat in response.Data)
    {
        Console.WriteLine(cat.Nombre);
    }
}
else
{
    Console.WriteLine($"Error: {response.Message}");
}

// Sin autenticación (datos públicos)
var info = await _api.GetDirectNoAuthAsync<InfoPublica>("/api/public/info");
```

### POST - Crear datos

```csharp
var nuevaCategoria = new Categoria
{
    Nombre = "Nueva Categoría",
    Descripcion = "Descripción"
};

// Con manejo completo de respuesta
var response = await _api.PostAsync<Categoria>("/api/categorias", nuevaCategoria);
if (response.Success)
{
    Console.WriteLine($"Categoría creada: {response.Data.Id}");
}

// Retorno directo
var creada = await _api.PostDirectAsync<Categoria>("/api/categorias", nuevaCategoria);

// Sin autenticación
var feedback = await _api.PostNoAuthAsync<bool>("/api/public/feedback", 
    new { mensaje = "Excelente servicio" });
```

### PUT - Actualizar datos

```csharp
var categoriaActualizada = new Categoria
{
    Id = 123,
    Nombre = "Nombre Actualizado",
    Descripcion = "Nueva descripción"
};

var response = await _api.PutAsync<Categoria>("/api/categorias/123", categoriaActualizada);
if (response.Success)
{
    Console.WriteLine("Categoría actualizada correctamente");
}
```

### DELETE - Eliminar datos

```csharp
var response = await _api.DeleteAsync<bool>("/api/categorias/123");
if (response.Success && response.Data)
{
    Console.WriteLine("Categoría eliminada");
}

// Retorno directo
bool eliminada = await _api.DeleteDirectAsync<bool>("/api/categorias/123");
```

## Manejo de Errores

### Con ApiResponse<T>

```csharp
var response = await _api.GetAsync<Categoria>("/api/categorias/999");

if (!response.Success)
{
    Console.WriteLine($"Error: {response.Message}");
    
    // Mostrar errores específicos
    if (response.Errors?.Any() == true)
    {
        foreach (var error in response.Errors)
        {
            Console.WriteLine($"- {error}");
        }
    }
}
```

### Con try-catch para errores de red

```csharp
try
{
    var categoria = await _api.GetDirectAsync<Categoria>("/api/categorias/123");
    // Procesar categoría...
}
catch (Exception ex)
{
    Console.WriteLine($"Error de conexión: {ex.Message}");
}
```

## Configuración JSON

El servicio utiliza configuración JSON case-insensitive y camelCase:

```json
{
  "id": 1,
  "nombre": "Categoría",
  "DESCRIPCION": "Descripción"
}
```

Se deserializa correctamente a:

```csharp
public class Categoria
{
    public int Id { get; set; }
    public string Nombre { get; set; }
    public string Descripcion { get; set; }
}
```

## Autenticación

Los métodos autenticados automáticamente:

1. Llaman a `AuthService.EnsureInitializedAsync()`
2. Obtienen el token de `AuthService.Session.Token`
3. Agregan el header: `Authorization: {token}`

Los métodos no autenticados (`*NoAuth*`) no requieren token y pueden usarse para:

- APIs públicas
- Endpoints de login
- Información pública
- Servicios externos

## Endpoints Completos

Recuerda usar endpoints completos con el servicio:

```csharp
// ✅ Correcto
await _api.GetAsync<Categoria>("/api/categorias/123");
await _api.GetAsync<Categoria>("https://api.externa.com/categorias/123");

// ❌ Incorrecto
await _api.GetAsync<Categoria>("categorias/123"); // Falta '/'
```

## Métodos de Procesamiento de ApiResponse

Además de los métodos HTTP básicos, el servicio incluye métodos especializados para procesar `ApiResponse<T>` y manejar errores de forma elegante:

### Procesamiento de Respuestas

| Método | Descripción | Ejemplo |
|--------|-------------|---------|
| `ProcessResponseAsync` | Ejecuta callbacks según el estado del response | Manejo async de éxito/error |
| `ProcessResponse` | Versión síncrona del anterior | Procesamiento inmediato |
| `GetDataOrDefault` | Extrae datos o retorna valor por defecto | Fallback seguro |
| `GetDataOrThrow` | Extrae datos o lanza excepción | Validación estricta |

### Transformación de Datos

| Método | Descripción | Ejemplo |
|--------|-------------|---------|
| `TransformResponse` | Transforma datos de un tipo a otro | `User` → `UserSummary` |
| `TransformResponseAsync` | Transformación asíncrona | Procesamiento complejo |

### Combinación y Validación

| Método | Descripción | Ejemplo |
|--------|-------------|---------|
| `CombineResponses` | Combina múltiples responses | Batch operations |
| `IsSuccessWithData` | Verifica éxito con datos | Validación rápida |
| `HasErrors` | Verifica si hay errores | Check de estado |
| `GetErrorMessages` | Obtiene todos los errores | Display de errores |

### API Fluent

| Método | Descripción | Ejemplo |
|--------|-------------|---------|
| `OnSuccess` / `OnSuccessAsync` | Ejecuta acción si es exitoso | Fluent processing |
| `OnError` / `OnErrorAsync` | Ejecuta acción si hay error | Error handling |

## Ejemplos de Procesamiento Avanzado

### 1. Procesamiento con Callbacks

```csharp
var response = await _api.GetAsync<Category>("/api/categories/123");

await _api.ProcessResponseAsync(
    response,
    onSuccess: async category => 
    {
        Console.WriteLine($"Categoría: {category.Name}");
        await UpdateUI(category);
    },
    onError: async error => 
    {
        Console.WriteLine($"Error: {error.Message}");
        await ShowErrorDialog(error);
    }
);
```

### 2. Obtener Datos con Fallback

```csharp
var response = await _api.GetAsync<Category>("/api/categories/123");

// Opción 1: Valor por defecto
var category = _api.GetDataOrDefault(response, 
    new Category { Name = "Categoría por defecto" });

// Opción 2: Lanzar excepción si falla
try 
{
    var requiredCategory = _api.GetDataOrThrow(response);
    // Usar categoría garantizada
}
catch (InvalidOperationException ex)
{
    // Manejar error crítico
}
```

### 3. Transformación de Datos

```csharp
var userResponse = await _api.GetAsync<User>("/api/users/123");

// Transformar a otro tipo
var nameResponse = _api.TransformResponse(userResponse, 
    user => user.FullName.ToUpper());

// Transformación asíncrona
var processedResponse = await _api.TransformResponseAsync(userResponse, 
    async user => 
    {
        await Task.Delay(100); // Procesamiento complejo
        return new UserSummary 
        { 
            Name = user.Name, 
            Status = await GetUserStatus(user.Id) 
        };
    });
```

### 4. Combinación de Múltiples Responses

```csharp
var responses = new[]
{
    await _api.GetAsync<Category>("/api/categories/1"),
    await _api.GetAsync<Category>("/api/categories/2"),
    await _api.GetAsync<Category>("/api/categories/3")
};

var combined = _api.CombineResponses(responses);

if (combined.Success)
{
    Console.WriteLine($"Obtenidas {combined.Data.Count} categorías");
    
    // Manejar errores parciales
    if (combined.Errors?.Any() == true)
    {
        Console.WriteLine("Algunos elementos fallaron:");
        foreach (var error in combined.Errors)
        {
            Console.WriteLine($"- {error}");
        }
    }
}
```

### 5. API Fluent para Encadenar Operaciones

```csharp
var response = await _api.GetAsync<Category>("/api/categories/123");

await _api
    .OnSuccessAsync(response, async category =>
    {
        Console.WriteLine($"Procesando: {category.Name}");
        await SaveToCache(category);
    })
    .OnErrorAsync(response, async error =>
    {
        Console.WriteLine($"Error: {error.Message}");
        await LogError(error);
    });
```

### 6. Uso en Componentes Blazor

```csharp
@inject API Api

@code {
    private List<Category> categories = new();
    private bool isLoading = true;
    private string errorMessage = "";

    protected override async Task OnInitializedAsync()
    {
        var response = await Api.GetAsync<List<Category>>("/api/categories");
        
        await Api.ProcessResponseAsync(
            response,
            onSuccess: async data =>
            {
                categories = data;
                isLoading = false;
                StateHasChanged();
            },
            onError: async error =>
            {
                errorMessage = Api.GetErrorMessages(error);
                isLoading = false;
                StateHasChanged();
            }
        );
    }
}
```

### 7. Manejo de Errores con Reintentos

```csharp
public async Task<Category> GetCategoryWithRetry(int id)
{
    var response = await _api.GetAsync<Category>($"/api/categories/{id}");
    int maxRetries = 3;
    int currentRetry = 0;

    while (!response.Success && currentRetry < maxRetries)
    {
        await Task.Delay(1000 * (currentRetry + 1)); // Backoff
        response = await _api.GetAsync<Category>($"/api/categories/{id}");
        currentRetry++;
    }

    return _api.GetDataOrThrow(response);
}
```

### 8. Validaciones Rápidas

```csharp
var response = await _api.GetAsync<Category>("/api/categories/123");

// Validaciones simples
if (_api.IsSuccessWithData(response))
{
    // Procesar datos seguros
    ProcessCategory(response.Data);
}

if (_api.HasErrors(response))
{
    // Mostrar todos los errores
    var allErrors = _api.GetErrorMessages(response);
    ShowErrorMessage(allErrors);
}
```

## Tests

El servicio incluye tests completos que cubren:

- ✅ Todos los métodos HTTP
- ✅ Todos los tipos de retorno
- ✅ Autenticación y no autenticación
- ✅ Manejo de errores
- ✅ Deserialización case-insensitive
- ✅ Errores de red

Para ejecutar los tests:

```bash
dotnet test --filter "FullyQualifiedName~APITests"
```