# 🎯 Custom Validator Tool

Herramienta para gestionar atributos personalizados en entidades sin tocar el código generado por Entity Framework Core.

## 🚀 Uso

### **Formato Principal**
```bash
python customvalidator.py entidad:campo:atributo1|atributo2 [entidad2:campo2:atributo3]
```

### **Ejemplos Básicos**
```bash
# Un solo campo de una entidad
python customvalidator.py categoria:Nombre:SoloCrear

# Un campo con múltiples atributos (preparado para el futuro)
python customvalidator.py categoria:Nombre:SoloCrear|Required

# Múltiples campos de la misma entidad
python customvalidator.py categoria:Nombre:SoloCrear categoria:Descripcion:SoloCrear

# Múltiples entidades y campos
python customvalidator.py categoria:Nombre:SoloCrear system_users:Email:SoloCrear

# Combinación compleja
python customvalidator.py categoria:Nombre:SoloCrear categoria:Descripcion:SoloCrear system_users:Email:SoloCrear system_users:Password:SoloCrear
```

### **Comandos Especiales**
```bash
# Listar todas las entidades disponibles
python customvalidator.py --list

# Ayuda completa
python customvalidator.py --help
```

## 🔄 Conversión de Nombres

La herramienta convierte automáticamente nombres de tablas a nombres de entidades siguiendo las convenciones de EF Core:

| Tabla | Entidad |
|-------|---------|
| `categoria` | `Categoria` |
| `system_users` | `SystemUsers` |
| `user_profile_data` | `UserProfileData` |
| `system_organization_test` | `SystemOrganizationTest` |

## 📋 Funcionalidades

- ✅ **Crea archivos .Metadata.cs** si no existen
- ✅ **Actualiza archivos existentes** agregando nuevos campos/atributos  
- ✅ **Previene duplicados** - no agrega el mismo atributo dos veces
- ✅ **Valida entidades** - verifica que la entidad exista antes de crear metadata
- ✅ **Soporte multi-atributo** - puede agregar múltiples atributos al mismo campo
- ✅ **Procesamiento en lote** - maneja múltiples campos en una sola ejecución

## 🏷️ Atributos Disponibles

| Atributo | Descripción | Ejemplo |
|----------|-------------|---------|
| `SoloCrear` | Campo solo modificable durante creación | `categoria:Nombre:SoloCrear` |
| `AutoIncremental` | Campo con numeración automática incremental | `producto:Codigo:AutoIncremental` |
| `NoSelect` | Campo que se devuelve como null en consultas (para datos sensibles) | `system_users:Password:NoSelect` |
| `FieldPermission` ⭐ | Campo protegido por permisos granulares CREATE/UPDATE/VIEW (Interactivo) | `empleado:SueldoBase:FieldPermission` |
| `Auditar` 🆕 | Campo que será auditado automáticamente - cambios se registran en system_auditoria | `empleado:SueldoBase:Auditar` |

> **Nota**: La herramienta soporta múltiples atributos y está preparada para agregar más en el futuro.

## 📁 Estructura Generada

```
Shared.Models/Entities/
├── Categoria.cs              # ← Generado por EF Core (no tocar)
└── Categoria.Metadata.cs     # ← Generado por esta herramienta
```

## 📝 Ejemplo de Archivo Generado

```csharp
// Categoria.Metadata.cs
using System;
using System.ComponentModel.DataAnnotations;
using Shared.Models.Attributes;

namespace Shared.Models.Entities
{
    [MetadataType(typeof(CategoriaMetadata))]
    public partial class Categoria { }

    public class CategoriaMetadata
    {
        [SoloCrear]
        public string Nombre;
        
        [SoloCrear]
        public string Descripcion;
        
        [SoloCrear]
        public string OrganizationId;
    }
}
```

## ⚡ Ventajas

- **Persistente**: Los archivos .Metadata.cs no se borran cuando regeneras modelos con EF Core
- **Limpio**: No modifica el código generado automáticamente  
- **Flexible**: Puede agregar múltiples atributos y campos
- **Seguro**: Valida duplicados y existencia de entidades
- **Eficiente**: Procesa múltiples campos en una sola ejecución

## 🛠️ Casos de Uso Comunes

```bash
# Marcar campo Nombre como solo creación
python customvalidator.py categoria:Nombre:SoloCrear

# Marcar campo Password como no seleccionable (seguridad)
python customvalidator.py system_users:Password:NoSelect

# Proteger campo con permisos granulares (nuevo sistema)
python customvalidator.py empleado:SueldoBase:FieldPermission

# Marcar múltiples campos de una entidad
python customvalidator.py categoria:Nombre:SoloCrear categoria:Descripcion:SoloCrear categoria:OrganizationId:SoloCrear

# Trabajar con múltiples entidades y atributos
python customvalidator.py categoria:Nombre:SoloCrear system_users:Email:SoloCrear system_users:Password:NoSelect

# Combinar múltiples atributos en una entidad
python customvalidator.py system_users:Password:NoSelect system_users:Email:SoloCrear system_users:CreatedDate:AutoIncremental

# Ver qué entidades están disponibles
python customvalidator.py --list
```

## 💡 Notas Importantes

1. **Formato obligatorio**: Cada argumento debe ser `entidad:campo:atributo`
2. **Múltiples atributos**: Se separan con `|` (`atributo1|atributo2`)
3. **Múltiples entidades**: Cada entidad puede aparecer múltiples veces
4. **Espacios**: Los argumentos se separan por espacios
5. **Validación**: La herramienta verifica que cada entidad exista antes de crear metadata
6. **Agrupación**: Los campos de la misma entidad se agrupan automáticamente en el mismo archivo .Metadata.cs

## 🎯 Roadmap

- [ ] Soporte para atributos con parámetros: `Required("mensaje")`
- [ ] Más atributos: `Required`, `MaxLength`, `Range`, etc.
- [ ] Validación de tipos de datos para atributos específicos
- [ ] Modo interactivo para seleccionar campos y atributos

---

## 🔒 Sistema de Permisos a Nivel de Campo

### **Nuevo: FieldPermission (Configuración Interactiva)** ⭐

El atributo `FieldPermission` ahora cuenta con configuración **completamente interactiva**:

```bash
# Iniciar configuración interactiva de FieldPermission
python customvalidator.py empleado:SueldoBase:FieldPermission
```

**🎯 Proceso Interactivo:**
1. **Selección de permisos**: Elige qué tipos aplicar (CREATE, UPDATE, VIEW o todos)
2. **Verificación automática**: Verifica si los permisos existen en la base de datos
3. **Creación automática**: Opcionalmente crea permisos faltantes en `system_permissions`
4. **Generación de código**: Crea automáticamente el atributo completo con los permisos configurados

**Ejemplo de sesión interactiva:**
```
🔐 CONFIGURACIÓN FIELDPERMISSION
   🏗️  Entidad: Empleado
   📝 Campo: SueldoBase
--------------------------------------------------
🎯 Selecciona qué permisos aplicar:
   1. CREATE - Controla creación de registros con este campo
   2. UPDATE - Controla modificación del campo en registros existentes
   3. VIEW   - Controla si el campo es visible en consultas
   4. Todos los anteriores
   0. Cancelar

🔹 Tu elección (1,2,3,4 o 0): 4

📋 Permisos seleccionados:
   CREATE: EMPLEADO.SUELDOBASE.CREATE
   UPDATE: EMPLEADO.SUELDOBASE.EDIT
   VIEW: EMPLEADO.SUELDOBASE.VIEW

🔍 Verificando permisos en base de datos...
   ❌ EMPLEADO.SUELDOBASE.CREATE NO existe
   ❌ EMPLEADO.SUELDOBASE.EDIT NO existe  
   ❌ EMPLEADO.SUELDOBASE.VIEW NO existe

🔨 Se encontraron 3 permisos faltantes.
¿Deseas crearlos automáticamente? (s/N): s
   🔨 Creando EMPLEADO.SUELDOBASE.CREATE...
   ✅ EMPLEADO.SUELDOBASE.CREATE creado exitosamente
   🔨 Creando EMPLEADO.SUELDOBASE.EDIT...
   ✅ EMPLEADO.SUELDOBASE.EDIT creado exitosamente
   🔨 Creando EMPLEADO.SUELDOBASE.VIEW...
   ✅ EMPLEADO.SUELDOBASE.VIEW creado exitosamente

✅ Atributo generado:
   [FieldPermission(CREATE="EMPLEADO.SUELDOBASE.CREATE", UPDATE="EMPLEADO.SUELDOBASE.EDIT", VIEW="EMPLEADO.SUELDOBASE.VIEW")]
```

**Resultado Final en Empleado.Metadata.cs:**
```csharp
[FieldPermission(CREATE="EMPLEADO.SUELDOBASE.CREATE", UPDATE="EMPLEADO.SUELDOBASE.EDIT", VIEW="EMPLEADO.SUELDOBASE.VIEW")]
public string SueldoBase;
```

**🚀 Ventajas del Sistema Interactivo:**
- ✅ **Todo en uno**: Metadata + Base de datos en una sola operación
- ✅ **Inteligente**: Detecta permisos existentes y evita duplicados
- ✅ **Flexible**: Puedes elegir solo los permisos que necesitas
- ✅ **Automático**: Genera nombres siguiendo convenciones estándar
- ✅ **Verificado**: Los permisos se crean realmente en la base de datos

## 📋 Sistema de Auditoría

### **Nuevo: Auditar** 🆕

Marca campos para auditoría automática en `system_auditoria`:

```bash
# Marcar campo para auditoría automática
python customvalidator.py empleado:SueldoBase:Auditar
```

**Funciona automáticamente:**
- 📝 **CREATE**: Registra valores iniciales al crear registros
- 📝 **UPDATE**: Registra valores anteriores y nuevos al modificar
- 📝 **Force Integration**: Incluye comentarios cuando se usa `ForceSaveChangesAsync("razón")`
- 📝 **JSON Storage**: Almacena cambios en formato JSON para fácil análisis

**📖 Documentación completa**: `/docs/10.SistemaAuditoria.md`

**🚀 El sistema funciona automáticamente** - sin código adicional en controladores.