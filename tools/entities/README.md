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

> **Nota**: Actualmente solo está disponible `SoloCrear`, pero la herramienta está preparada para agregar más atributos en el futuro.

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

# Marcar múltiples campos de una entidad
python customvalidator.py categoria:Nombre:SoloCrear categoria:Descripcion:SoloCrear categoria:OrganizationId:SoloCrear

# Trabajar con múltiples entidades
python customvalidator.py categoria:Nombre:SoloCrear system_users:Email:SoloCrear system_organization:Nombre:SoloCrear

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