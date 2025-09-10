# Sistema de Autorización por Permisos - Guía de Uso

## Resumen
Este sistema permite proteger páginas Blazor mediante permisos específicos usando un atributo simple y una página de "No Autorizado" elegante.

## Componentes del Sistema

### 1. AuthService (Existente)
El servicio ya existente que maneja la autenticación y permisos:
- `HasAnyPermission(params string[] permissions)` - Verifica si el usuario tiene al menos uno de los permisos
- `HasPermission(string permission)` - Verifica un permiso específico
- `GetAllUserPermissions()` - Obtiene todos los permisos del usuario

### 2. Página Not Authorized (`/not-authorized`)
Página elegante que muestra:
- Mensaje de acceso denegado
- Permisos requeridos para acceder
- Permisos actuales del usuario
- Botones para navegar (Volver/Ir al Inicio)
- Información de contacto

### 3. AuthorizePermissionAttribute
Atributo para decorar páginas que requieren permisos específicos:
```csharp
[AuthorizePermission("permission1", "permission2", "permission3")]
```

### 4. AuthorizedPageBase
Clase base que maneja automáticamente la verificación de permisos y redirección.

## Cómo Usar el Sistema

### Paso 1: Crear una Página Protegida

```razor
@page "/my-secure-page"
@using Frontend.Attributes
@using Frontend.Components.Auth
@inherits AuthorizedPageBase
@attribute [AuthorizePermission("users.view", "admin.access")]

<PageTitle>Mi Página Segura</PageTitle>

<h1>Contenido Protegido</h1>
<p>Solo usuarios con permisos 'users.view' o 'admin.access' pueden ver esto.</p>

@code {
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync(); // ¡IMPORTANTE! Siempre llamar al base
        
        // Tu lógica de inicialización aquí (solo se ejecuta si tiene permisos)
        if (HasRequiredPermissions)
        {
            await OnPermissionsVerifiedAsync();
        }
    }

    protected override async Task OnPermissionsVerifiedAsync()
    {
        // Este método se ejecuta después de verificar permisos exitosamente
        // Aquí puedes cargar datos, configurar componentes, etc.
    }
}
```

### Paso 2: Propiedades Disponibles en AuthorizedPageBase

```csharp
public abstract class AuthorizedPageBase : ComponentBase
{
    // Servicios inyectados automáticamente
    protected AuthService AuthService { get; set; }
    protected NavigationManager Navigation { get; set; }
    
    // Estados disponibles en tu página
    protected bool IsCheckingPermissions { get; private set; } // Si está verificando permisos
    protected bool HasRequiredPermissions { get; private set; } // Si tiene los permisos requeridos
    protected string[] RequiredPermissions { get; private set; } // Lista de permisos requeridos
}
```

## Ejemplos de Uso

### Ejemplo 1: Página de Administración
```razor
@page "/admin/users"
@inherits AuthorizedPageBase
@attribute [AuthorizePermission("admin.users", "super.admin")]

<h1>Gestión de Usuarios</h1>
<!-- Solo usuarios con 'admin.users' O 'super.admin' pueden acceder -->
```

### Ejemplo 2: Página con Múltiples Permisos
```razor
@page "/reports/financial"
@inherits AuthorizedPageBase
@attribute [AuthorizePermission("reports.view", "reports.financial", "admin.full")]

<h1>Reportes Financieros</h1>
<!-- Usuario necesita al menos UNO de estos permisos -->
```

### Ejemplo 3: Página con Un Solo Permiso
```razor
@page "/profile/settings"
@inherits AuthorizedPageBase
@attribute [AuthorizePermission("profile.edit")]

<h1>Configuración del Perfil</h1>
<!-- Solo usuarios con 'profile.edit' pueden acceder -->
```

## Flujo de Autorización

1. **Usuario navega a página protegida**
2. **Sistema verifica autenticación** - Si no está autenticado → Redirige a `/login`
3. **Sistema verifica permisos** - Compara permisos del usuario vs requeridos
4. **Si tiene permisos** → Muestra la página
5. **Si NO tiene permisos** → Redirige a `/not-authorized` con información de permisos requeridos

## Características Especiales

### Indicador de Carga
Mientras se verifican permisos, se muestra un indicador de progreso circular con el texto "Verificando permisos..."

### Página Not Authorized Inteligente
- Muestra los permisos específicos que faltan
- Muestra los permisos actuales del usuario (primeros 10)
- Permite navegar hacia atrás o al inicio
- Diseño responsive y accesible

### Manejo de Errores
En caso de cualquier error durante la verificación:
- Se niega el acceso por seguridad
- Se redirige a `/not-authorized`
- Se registra el error en consola

## Compatibilidad

✅ **Compatible con tu sistema existente**:
- Usa el `AuthService` actual
- No modifica componentes existentes
- Se integra con `AuthorizePermission` component
- Funciona con el sistema de permisos por wildcards (`categoria.*`)

## Próximos Pasos

1. **Aplicar a páginas existentes**: Agregar `@inherits AuthorizedPageBase` y `@attribute [AuthorizePermission(...)]`
2. **Customizar página Not Authorized**: Modificar estilos o contenido según necesidades
3. **Agregar logging**: Implementar registro de intentos de acceso denegado
4. **Métricas**: Rastrear qué páginas son más solicitadas sin permisos

## Ejemplo Completo de Migración

**Antes:**
```razor
@page "/admin/settings"
<h1>Configuración Admin</h1>
```

**Después:**
```razor
@page "/admin/settings"
@inherits AuthorizedPageBase
@attribute [AuthorizePermission("admin.settings", "super.admin")]

<h1>Configuración Admin</h1>

@code {
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        // Tu código aquí
    }
}
```

¡Listo! Tu página ahora está protegida automáticamente. 🔒