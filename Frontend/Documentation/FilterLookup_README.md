# Sistema FilterLookup para EntityTable

El sistema FilterLookup permite crear filtros personalizados para relaciones en el EntityTable, proporcionando una experiencia rica de búsqueda y selección similar a los lookups tradicionales.

## Características Principales

- ✅ **Búsqueda por múltiples campos**: Buscar por nombre, RUT, email, etc.
- ✅ **Visualización de múltiples columnas**: Mostrar información relevante en formato tabla
- ✅ **Integración con componente Lookup existente**: Reutiliza la lógica ya implementada
- ✅ **Soporte para paginación**: Maneja grandes volúmenes de datos
- ✅ **Operadores de filtro**: Equals, NotEquals, IsNull, IsNotNull

## Configuración Básica

### Ejemplo 1: Lookup simple de Categorías

```csharp
new ColumnConfig<Producto> 
{
    Property = "CategoriaId",
    Title = "Categoría",
    FilterLookup = new FilterLookup<Categoria>
    {
        DisplayProperty = "Nombre",
        ValueProperty = "Id",
        Service = categoriaService,
        SearchColumns = new[] { "Nombre" }
    }
}
```

### Ejemplo 2: Lookup complejo de Clientes con múltiples columnas

```csharp
new ColumnConfig<Pedido> 
{
    Property = "ClienteId",
    Title = "Cliente",
    FilterLookup = new FilterLookup<Cliente>
    {
        DisplayProperty = "Nombre",
        ValueProperty = "Id",
        Service = clienteService,
        DisplayColumns = new[]
        {
            new LookupColumnConfig 
            { 
                Property = "Nombre", 
                Title = "Nombre", 
                Width = "200px" 
            },
            new LookupColumnConfig 
            { 
                Property = "Rut", 
                Title = "RUT", 
                Width = "120px" 
            },
            new LookupColumnConfig 
            { 
                Property = "Email", 
                Title = "Email", 
                Width = "250px" 
            }
        },
        SearchColumns = new[] { "Nombre", "Rut", "Email" },
        PageSize = 15
    }
}
```

### Ejemplo 3: Lookup con datos estáticos

```csharp
new ColumnConfig<Usuario> 
{
    Property = "EstadoId",
    Title = "Estado",
    FilterLookup = new FilterLookup<Estado>
    {
        DisplayProperty = "Descripcion",
        ValueProperty = "Id",
        StaticData = new List<Estado>
        {
            new Estado { Id = 1, Descripcion = "Activo" },
            new Estado { Id = 2, Descripcion = "Inactivo" },
            new Estado { Id = 3, Descripcion = "Suspendido" }
        },
        SearchColumns = new[] { "Descripcion" }
    }
}
```

### Ejemplo 4: Lookup con función personalizada de carga

```csharp
new ColumnConfig<Proyecto> 
{
    Property = "ResponsableId",
    Title = "Responsable",
    FilterLookup = new FilterLookup<Usuario>
    {
        DisplayProperty = "NombreCompleto",
        ValueProperty = "Id",
        DataLoader = async (searchTerm, skip, take) =>
        {
            // Lógica personalizada para cargar usuarios activos
            var usuarios = await usuarioService.GetUsuariosActivosAsync(searchTerm, skip, take);
            return (usuarios.Data, usuarios.TotalCount);
        },
        SearchColumns = new[] { "Nombre", "Apellido", "Email" }
    }
}
```

## Propiedades de FilterLookup<T>

| Propiedad | Tipo | Descripción | Por Defecto |
|-----------|------|-------------|-------------|
| `DisplayProperty` | `string` | Propiedad principal a mostrar | "Nombre" |
| `ValueProperty` | `string` | Propiedad que contiene el valor | "Id" |
| `DisplayColumns` | `LookupColumnConfig[]` | Columnas a mostrar en el lookup | `[]` |
| `SearchColumns` | `string[]` | Columnas por las que se puede buscar | `[]` |
| `Service` | `BaseApiService<T>` | Servicio API para cargar datos | `null` |
| `StaticData` | `List<T>` | Datos estáticos (alternativa al Service) | `null` |
| `DataLoader` | `Func<...>` | Función personalizada para cargar datos | `null` |
| `PageSize` | `int` | Tamaño de página | 20 |
| `NullDisplayText` | `string` | Texto cuando el valor es nulo | "(Sin seleccionar)" |

## Propiedades de LookupColumnConfig

| Propiedad | Tipo | Descripción | Por Defecto |
|-----------|------|-------------|-------------|
| `Property` | `string` | Nombre de la propiedad | `""` |
| `Title` | `string` | Título a mostrar | `""` |
| `Width` | `string?` | Ancho de la columna | `null` |
| `TextAlign` | `TextAlign` | Alineación del texto | `TextAlign.Left` |
| `Visible` | `bool` | Si la columna es visible | `true` |

## Integración en EntityTable

### Configuración completa de tabla con lookups

```csharp
@page "/pedidos"
@using Frontend.Components.Base.Tables
@using Frontend.Models

<EntityTable T="Pedido" 
             ApiService="@pedidoService"
             ColumnConfigs="@columnConfigs" />

@code {
    private List<ColumnConfig<Pedido>> columnConfigs = new()
    {
        new ColumnConfig<Pedido> 
        { 
            Property = "Numero", 
            Title = "N° Pedido", 
            Width = "120px" 
        },
        new ColumnConfig<Pedido> 
        { 
            Property = "ClienteId", 
            Title = "Cliente", 
            FilterLookup = new FilterLookup<Cliente>
            {
                DisplayProperty = "Nombre",
                Service = clienteService,
                DisplayColumns = new[]
                {
                    new LookupColumnConfig { Property = "Nombre", Title = "Cliente", Width = "200px" },
                    new LookupColumnConfig { Property = "Rut", Title = "RUT", Width = "120px" }
                },
                SearchColumns = new[] { "Nombre", "Rut" }
            }
        },
        new ColumnConfig<Pedido> 
        { 
            Property = "ProductoId", 
            Title = "Producto", 
            FilterLookup = new FilterLookup<Producto>
            {
                DisplayProperty = "Nombre",
                Service = productoService,
                SearchColumns = new[] { "Nombre", "Codigo" }
            }
        },
        new ColumnConfig<Pedido> 
        { 
            Property = "FechaPedido", 
            Title = "Fecha", 
            Width = "150px" 
        },
        new ColumnConfig<Pedido> 
        { 
            Property = "Total", 
            Title = "Total", 
            Width = "120px" 
        }
    };
}
```

## Funcionalidad del Filtro

1. **Selección del Operador**: 
   - Igual a
   - No es igual a
   - Es nulo
   - No es nulo

2. **Búsqueda**: El usuario puede buscar en los campos especificados en `SearchColumns`

3. **Selección**: Al seleccionar un elemento, se aplica el filtro usando el `ValueProperty`

4. **Visualización**: Se muestra información rica usando las `DisplayColumns` configuradas

## Casos de Uso Típicos

### 1. Filtro de Productos por Categoría
- Campo: `CategoriaId`
- Buscar por: Nombre de categoría
- Mostrar: Nombre y descripción

### 2. Filtro de Pedidos por Cliente
- Campo: `ClienteId` 
- Buscar por: Nombre, RUT, Email
- Mostrar: Nombre, RUT, Email, Teléfono

### 3. Filtro de Usuarios por Rol
- Campo: `RolId`
- Datos estáticos de roles del sistema
- Mostrar: Nombre y descripción del rol

### 4. Filtro de Documentos por Estado
- Campo: `EstadoId`
- Datos estáticos de estados
- Mostrar: Estado y descripción

## Notas Técnicas

- El sistema detecta automáticamente si una columna tiene `FilterLookup` configurado
- Se integra perfectamente con el componente `Lookup` existente
- Soporta paginación y búsqueda asíncrona
- Los operadores disponibles están optimizados para relaciones (Guid)
- Compatible con cache y optimizaciones del sistema Lookup base

## Limitaciones Actuales

- Solo soporta relaciones con Guid como clave
- Las columnas dinámicas en el lookup aún no están completamente implementadas
- Solo disponible para filtros personalizados (no integrado con filtros nativos de RadzenDataGrid)

## Próximas Mejoras

- [ ] Soporte para claves compuestas
- [ ] Renderizado dinámico de columnas personalizadas
- [ ] Integración con filtros nativos de RadzenDataGrid
- [ ] Soporte para relaciones many-to-many
- [ ] Auto-detección de relaciones por convención de nombres