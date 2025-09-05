# 🔐 Permissions Generator

Generador automático de permisos del sistema para entidades en la base de datos SQL Server.

## 📋 Descripción

El Permissions Generator crea automáticamente los 6 permisos estándar para cualquier entidad en la tabla `system_permissions`:

- **CREATE** - Crear registros
- **VIEW** - Ver registros
- **UPDATE** - Actualizar registros
- **DELETE** - Eliminar registros
- **VIEWMENU** - Ver menú
- **RESTORE** - Restaurar registros

## 🚀 Uso

### Comando básico
```bash
python3 tools/permissions/permissions_generator.py --entity Marca
```

### Con plural personalizado
```bash
python3 tools/permissions/permissions_generator.py --entity Categoria --plural Categorias
```

### Solo preview (sin ejecutar)
```bash
python3 tools/permissions/permissions_generator.py --entity Marca --preview
```

### Con cadena de conexión personalizada
```bash
python3 tools/permissions/permissions_generator.py --entity Marca --connection-string "Server=mi-server;Database=mi-db;Trusted_Connection=yes;"
```

## 🔧 Parámetros

| Parámetro | Requerido | Descripción | Ejemplo |
|-----------|-----------|-------------|---------|
| `--entity` | ✅ | Nombre de la entidad | `Marca`, `Categoria` |
| `--plural` | ❌ | Plural de la entidad | `Marcas`, `Categorias` |
| `--preview` | ❌ | Solo mostrar SQL sin ejecutar | - |
| `--connection-string` | ❌ | Cadena de conexión personalizada | Ver ejemplo arriba |

## 📊 Permisos Generados

Para una entidad `Marca` se generan:

```
MARCA.CREATE    - Crear marcas
MARCA.VIEW      - Ver marcas  
MARCA.UPDATE    - Actualizar marcas
MARCA.DELETE    - Eliminar marcas
MARCA.VIEWMENU  - Ver menú de marcas
MARCA.RESTORE   - Restaurar marcas
```

## 🗄️ Estructura en Base de Datos

Los permisos se insertan en la tabla `system_permissions` con esta estructura:

```sql
INSERT INTO [system_permissions] 
([Id], [Nombre], [Descripcion], [FechaCreacion], [FechaModificacion], 
[OrganizationId], [ActionKey], [GroupKey], [GrupoNombre], [Active]) 
VALUES (
  'GUID-GENERADO',
  'MARCA.CREATE',
  'Crear marcas',
  '2024-01-01 10:00:00.000',
  '2024-01-01 10:00:00.000',
  'ORG-ID',
  'MARCA.CREATE',
  'MARCA',
  'Marcas',
  '1'
)
```

## ⚙️ Configuración de Base de Datos

### Conexión por defecto
- **Server**: `localhost`
- **Database**: `NuevoProyectoDB`  
- **Authentication**: Windows (Trusted Connection)

### Requisitos
- SQL Server con ODBC Driver 17
- Tabla `system_permissions` debe existir
- Permisos de escritura en la base de datos

## 🔍 Validaciones

- ✅ Verifica que los permisos no existan antes de crearlos
- ✅ Obtiene automáticamente el `OrganizationId` de la tabla `system_organizations`
- ✅ Genera GUIDs únicos para cada permiso
- ✅ Maneja errores de conexión y duplicados

## 📝 Ejemplos de Salida

### Ejecución exitosa
```
🔐 Generando permisos para entidad: Marca
📝 Plural: Marcas
🏷️ Grupo: MARCA

🔌 Conectando a base de datos...
🏢 Organization ID: F5B94C07-FAE1-4A2B-90AB-B73D4AAD67DC

✅ Preparado: MARCA.CREATE - Crear marcas
✅ Preparado: MARCA.VIEW - Ver marcas
✅ Preparado: MARCA.UPDATE - Actualizar marcas
✅ Preparado: MARCA.DELETE - Eliminar marcas
✅ Preparado: MARCA.VIEWMENU - Ver menú de marcas
✅ Preparado: MARCA.RESTORE - Restaurar marcas

📊 Total a crear: 6 permisos
💾 Ejecutando inserts...

🎉 PERMISOS CREADOS EXITOSAMENTE!
✅ 6 permisos insertados en system_permissions
```

### Con preview
```
👀 MODO PREVIEW - No se ejecutarán cambios

📋 SQL QUE SE EJECUTARÍA:
------------------------------------------------------------
INSERT INTO [dbo].[system_permissions] 
([Id], [Nombre], [Descripcion], [FechaCreacion], [FechaModificacion], 
[OrganizationId], [CreadorId], [ModificadorId], [Active], [ActionKey], [GroupKey], [GrupoNombre]) 
VALUES ('ABC-123', N'MARCA.CREATE', N'Crear marcas', ...)
```

## 🔄 Integración con Entity Generator

Este tool se ejecuta automáticamente entre las fases 1 y 2 del generador de entidades:

1. **Fase 1**: Crear tabla en base de datos
2. **🔐 Permissions**: Crear permisos de la entidad ← **AQUÍ**
3. **Fase 2**: Generar backend + frontend completo