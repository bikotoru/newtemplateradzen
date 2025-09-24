# Sistema de Gestión de Búsquedas Guardadas (SavedQueries)

## 📋 Descripción

Este módulo implementa un sistema completo de CRUD para la gestión de búsquedas avanzadas guardadas, permitiendo a los usuarios crear, editar, eliminar, compartir y ejecutar búsquedas personalizadas.

## 🏗️ Arquitectura

### Backend (CustomFields.API)

#### Controllers
- **SavedQueriesController.cs**: Controlador principal con endpoints para CRUD completo
  - ✅ Compatible con BaseApiService (endpoints `/all`, `/create`, `/update`, `/{id}`)
  - ✅ Endpoints originales mantenidos para compatibilidad
  - ✅ Autenticación y autorización implementadas
  - ✅ Soporte para paginación y filtros

- **SavedQuerySharesController.cs**: Controlador para gestión de compartidos
  - ✅ Compartir con usuarios y roles
  - ✅ Gestión de permisos granulares
  - ✅ Autenticación y autorización implementadas

#### Permisos Requeridos
- **SAVEDQUERIES.VIEW**: Ver y obtener búsquedas guardadas
- **SAVEDQUERIES.CREATE**: Crear y duplicar búsquedas guardadas
- Los permisos de actualización y eliminación se manejan por lógica de propietario/compartidos

### Frontend 

#### Páginas Principales
- **List.razor**: Página de listado con tabla avanzada y funciones CRUD
- **Formulario.razor**: Formulario para crear/editar búsquedas guardadas

#### Componentes
- **ShareQueryModal.razor**: Modal para compartir búsquedas
- **DuplicateQueryModal.razor**: Modal para duplicar búsquedas  
- **ShareManagementTab.razor**: Tab para gestionar compartidos

#### Servicios y Tipos
- **SavedQuery.ts**: Interfaces TypeScript para el modelo de datos
- **SavedQueryService.ts**: Servicio para llamadas a la API
- **SavedQueryViewManager.ts**: Gestor de vistas para la tabla

## 🚀 Funcionalidades Implementadas

### ✅ CRUD Completo
- **Crear**: Nuevas búsquedas guardadas con validación
- **Leer**: Listado paginado con filtros y vistas múltiples
- **Actualizar**: Edición completa de búsquedas existentes
- **Eliminar**: Soft delete con confirmación

### ✅ Funciones Avanzadas
- **Duplicar**: Crear copias de búsquedas existentes
- **Compartir**: Con usuarios y roles específicos
- **Permisos Granulares**: Ver, Editar, Ejecutar, Compartir
- **Vistas Múltiples**: "Mis Búsquedas", "Búsquedas Públicas", "Plantillas"
- **Búsqueda y Filtrado**: En tiempo real en la tabla
- **Exportación a Excel**: Incluida automáticamente
- **Autenticación y Autorización**: Sistema completo implementado

### ✅ Gestión de Compartidos
- Compartir con usuarios específicos
- Compartir con roles organizacionales
- Permisos granulares por compartido
- Visualización de compartidos actuales
- Revocación de accesos

## 🛠️ Instalación y Configuración

### 1. Permisos de Base de Datos
Ejecutar el script SQL generado para agregar los permisos requeridos:
```sql
-- Ver: add_savedqueries_permissions.sql
```

### 2. Rutas del Frontend
Las siguientes rutas están disponibles:
- `/advanced-query/saved-queries/list` - Listado de búsquedas guardadas
- `/advanced-query/saved-queries/formulario` - Crear nueva búsqueda
- `/advanced-query/saved-queries/formulario/{id}` - Editar búsqueda existente

### 3. Integración con Búsqueda Avanzada
Para ejecutar una búsqueda guardada desde la página de búsqueda avanzada:
```
/advanced-query?loadQuery={id}
```

## 📝 Uso

### Para Usuarios
1. **Ver búsquedas**: Acceder a `/advanced-query/saved-queries/list`
2. **Crear nueva**: Clic en "Nuevo" desde la lista
3. **Editar**: Clic en el icono de edición en la tabla
4. **Eliminar**: Clic en el icono de eliminación (solo propietarios)
5. **Duplicar**: Clic en el icono de copia
6. **Compartir**: Clic en el icono de compartir (solo propietarios)
7. **Ejecutar**: Clic en el icono de ejecución

### Para Administradores
1. Asignar permisos `SAVEDQUERIES.VIEW` y `SAVEDQUERIES.CREATE` según sea necesario
2. Los usuarios solo pueden eliminar sus propias búsquedas
3. Los usuarios pueden editar búsquedas compartidas con ellos si tienen permisos de edición

## 🔧 Configuración Técnica

### Modelo de Datos
```typescript
interface SavedQuery {
    id: string;
    name: string;
    description?: string;
    entityName: string;
    selectedFields: string; // JSON
    filterConfiguration?: string; // JSON
    logicalOperator: number;
    takeLimit: number;
    isPublic: boolean;
    isTemplate: boolean;
    // ... otros campos
}
```

### API Endpoints

#### Búsquedas Guardadas
- `GET /api/saved-queries/all` - Listado paginado (compatible con BaseApiService)
- `POST /api/saved-queries/create` - Crear (compatible con BaseApiService)
- `PUT /api/saved-queries/update` - Actualizar (compatible con BaseApiService)
- `DELETE /api/saved-queries/{id}` - Eliminar (compatible con BaseApiService)
- `GET /api/saved-queries/{id}` - Obtener por ID
- `POST /api/saved-queries/{id}/duplicate` - Duplicar

#### Compartidos
- `GET /api/saved-queries/{id}/shares` - Listar compartidos
- `POST /api/saved-queries/{id}/shares/users/{userId}` - Compartir con usuario
- `POST /api/saved-queries/{id}/shares/roles/{roleId}` - Compartir con rol
- `PUT /api/saved-queries/{id}/shares/{shareId}` - Actualizar permisos
- `DELETE /api/saved-queries/{id}/shares/{shareId}` - Revocar compartido

## 🎯 Estado del Proyecto

### ✅ Completado
- Backend: Controllers con autenticación completa
- Frontend: Páginas y componentes principales
- Integración: BaseApiService compatible
- Permisos: Sistema de autorización
- Compartidos: Gestión completa de compartidos
- Validaciones: Frontend y backend
- Compilación: Backend compila sin errores

### 🔄 Pendiente para Testing
- Pruebas de integración frontend-backend
- Validación de permisos en runtime
- Testing de funcionalidades de compartido
- Pruebas de UI/UX en diferentes navegadores

## 📚 Notas Técnicas

- El sistema usa soft delete (`Active = false`)
- Compatible con el patrón EntityTable existente
- Sigue las convenciones del proyecto (nombres, estructura, autenticación)
- Todas las respuestas usan el formato estándar `ApiResponse<T>`
- TypeScript interfaces incluidas para type safety
- Componentes modularizados y reutilizables