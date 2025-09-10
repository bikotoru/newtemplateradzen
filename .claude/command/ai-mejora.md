# Guía de Arquitectura y Mejoras del Sistema

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