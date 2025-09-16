# 🎯 Auto-Generación de Entidades CRUD con Python Script

Documentación completa del sistema avanzado de generación automática de entidades CRUD completas.

## 📋 Índice

- [🔧 Instalación y Configuración](#-instalación-y-configuración)
- [🚀 Uso Básico](#-uso-básico)
- [📊 Targets Disponibles](#-targets-disponibles)
- [🗄️ Configuración de Base de Datos](#️-configuración-de-base-de-datos)
- [📝 Configuración de Formularios](#-configuración-de-formularios)
- [📊 Configuración de Grillas](#-configuración-de-grillas)
- [🔗 Lookups y Relaciones](#-lookups-y-relaciones)
- [👁️ Campos de Solo Lectura](#️-campos-de-solo-lectura)
- [🔍 Configuración de Búsqueda](#-configuración-de-búsqueda)
- [⚠️ Validaciones y Errores](#️-validaciones-y-errores)
- [💡 Ejemplos Completos](#-ejemplos-completos)
- [🛠️ Resolución de Problemas](#️-resolución-de-problemas)

## 🔧 Instalación y Configuración

### Prerrequisitos

1. **Python 3.12+** instalado
2. **SQL Server** con conexión configurada en `launchSettings.json`
3. **Proyecto .NET** con estructura Backend/Frontend/Shared.Models

### Ubicación del Script

```bash
tools/forms/entity-generator.py
```

## 🚀 Uso Básico

### Comandos Base

**Entidad Normal:**
```bash
python3 tools/forms/entity-generator.py \
    --entity "NombreEntidad" \
    --module "Modulo.Submodulo" \
    --target [db|interfaz|todo] \
    --auto-register \               # ✨ NUEVO: Auto-registro en FormDesigner
    --system-entity \               # ✨ NUEVO: Entidad global del sistema
    --icon "material_icon" \        # ✨ NUEVO: Icono Material Design
    --category "Categoria"          # ✨ NUEVO: Categoría de organización
```

**Relación NN (Muchos-a-Muchos):**
```bash
python3 tools/forms/entity-generator.py \
    --source tabla1 \
    --to tabla2 \
    [--alias nombreEspecial] \
    --module "Modulo.Submodulo" \
    --target db
```

### Parámetros Obligatorios

**Para Entidades Normales:**
- `--entity`: Nombre de la entidad (ej: `Producto`, `Cliente`)
- `--module`: Módulo del proyecto (ej: `Inventario.Core`, `Ventas.Facturacion`)
- `--target`: Qué generar (`db`, `interfaz`, `todo`)

**Para Relaciones NN:**
- `--source`: Tabla origen de la relación (ej: `venta`)
- `--to`: Tabla destino de la relación (ej: `productos`)
- `--module`: Módulo del proyecto
- `--target`: Solo `db` (las relaciones NN no tienen interfaz)

### Parámetros Opcionales

- `--plural`: Plural de la entidad (por defecto: `{entidad}s`) - *solo para entidades normales*
- `--alias`: Nombre especial para la relación NN (ej: `promocion`) - *solo para relaciones NN*

### ✨ Nuevos Parámetros de FormDesigner

- `--auto-register`: Auto-registra la entidad en `system_form_entities` para FormDesigner
- `--system-entity`: Marca la entidad como global del sistema (OrganizationId=NULL)
- `--icon`: Icono Material Design para la entidad (ej: `person`, `business`, `inventory`)
- `--category`: Categoría de organización (ej: `RRHH`, `Core`, `Bancario`, `Previsional`)
- `--allow-custom-fields`: Permitir campos personalizados (default: true)

## 📊 Targets Disponibles

### 🗄️ `db` - Solo Base de Datos
Crea únicamente la tabla SQL y genera los permisos del sistema.

**Incluye:**
- ✅ Tabla en SQL Server
- ✅ Modelos EF Core sincronizados
- ✅ 6 permisos del sistema automáticos
- ✅ Validaciones de integridad referencial

### 🎨 `interfaz` - Solo Interfaz
Genera únicamente el código de backend y frontend (requiere tabla existente).

**Incluye:**
- ✅ Service y Controller del backend
- ✅ Service, ViewManager y componentes del frontend
- ✅ Registro en ServiceRegistry de ambos lados

### 🚀 `todo` - Completo
Genera todo: base de datos + interfaz completa.

**Incluye:**
- ✅ Todo lo de `db`
- ✅ Todo lo de `interfaz`
- ✅ Sistema CRUD completamente funcional

## 🗄️ Configuración de Base de Datos

### Campos Regulares (`--fields`)

Define todos los campos que van a la tabla SQL.

**Sintaxis:** `"nombre:tipo:tamaño"`

**Tipos soportados:**
- `string:tamaño` → `NVARCHAR(tamaño)`
- `text` → `NVARCHAR(MAX)`
- `int` → `INT`
- `decimal:precision,escala` → `DECIMAL(precision,escala)`
- `datetime` → `DATETIME2`
- `bool` → `BIT`
- `guid` → `UNIQUEIDENTIFIER`

**Ejemplos:**
```bash
--fields "nombre:string:255" \
         "descripcion:text" \
         "precio:decimal:18,2" \
         "stock:int" \
         "fecha_creacion:datetime" \
         "activo:bool"
```

### Foreign Keys (`--fk`)

Define relaciones con otras tablas existentes.

**Sintaxis:** `"campo_id:tabla_referencia"`

**Importante:** La tabla referenciada debe existir antes de crear la nueva entidad.

**Ejemplos:**
```bash
--fk "categoria_id:categoria" \
     "proveedor_id:proveedor" \
     "usuario_id:system_users"
```

## 📝 Configuración de Formularios

### Form Fields (`--form-fields`)

Define comportamiento específico de campos en el formulario de edición.

**Sintaxis:** `"campo:opciones"`

**Opciones disponibles:**
- `required` - Campo obligatorio
- `unique` - Valor único en BD
- `nullable` - Permite valores nulos
- `placeholder=texto` - Texto placeholder
- `label=texto` - Etiqueta personalizada
- `default=valor` - Valor por defecto
- `min=número` - Valor mínimo (numéricos)
- `max=número` - Valor máximo (numéricos)
- `min_length=número` - Longitud mínima (texto)
- `max_length=número` - Longitud máxima (texto)

**Ejemplos:**
```bash
--form-fields "nombre:required:placeholder=Ingrese el nombre del producto:min_length=3" \
              "codigo:required:unique:placeholder=Código único:max_length=50" \
              "precio:required:min=0:placeholder=0.00" \
              "stock_minimo:default=5:min=0" \
              "descripcion:placeholder=Descripción detallada (opcional)"
```

## 📊 Configuración de Grillas

### Grid Fields (`--grid-fields`)

Define qué campos aparecen en la tabla de listado y cómo se muestran.

**Sintaxis:** `"campo:ancho:alineación:opciones"`

**Alineación:**
- `left` - Izquierda
- `right` - Derecha (recomendado para números)
- `center` - Centro

**Opciones:**
- `s` - Sortable (ordenable)
- `f` - Filterable
- `sf` - Sortable + Filterable

**Para lookups:**
```bash
"campo_id->TablaRelacionada.CampoDisplay:ancho:align:opciones"
```

**Ejemplos:**
```bash
--grid-fields "nombre:200px:left:sf" \
              "codigo:120px:left:s" \
              "precio:120px:right:sf" \
              "categoria_id->Categoria.Nombre:150px:left:f" \
              "stock:100px:right:s" \
              "activo:80px:center:f"
```

## 🔗 Lookups y Relaciones

### Lookups (`--lookups`)

Define componentes de selección para Foreign Keys.

**Sintaxis:** `"campo_fk:tabla_objetivo:campo_display:opciones"`

**Opciones:**
- `required` - Selección obligatoria
- `cache` - Cachear opciones en memoria
- `fast` - Búsqueda rápida
- `form` - Mostrar en formulario
- `grid` - Mostrar en grilla
- `form,grid` - Mostrar en ambos

**Ejemplos:**
```bash
--lookups "categoria_id:categoria:Nombre:required:cache:form,grid" \
          "proveedor_id:proveedor:RazonSocial:required:fast:form" \
          "usuario_asignado_id:system_users:NombreCompleto:cache:form"
```

## 👁️ Campos de Solo Lectura

### Readonly Fields (`--readonly-fields`)

Define campos que se muestran pero no se pueden editar.

**Sintaxis:** `"campo:tipo:opciones"`

**Opciones:**
- `label=texto` - Etiqueta personalizada
- `format=formato` - Formato de visualización

**Ejemplos:**
```bash
--readonly-fields "fecha_creacion:datetime:label=Creado el" \
                  "total_vendido:int:label=Total vendido:format=currency" \
                  "stock_actual:int:label=Stock disponible" \
                  "ultima_actualizacion:datetime:label=Última actualización"
```

## 🔍 Configuración de Búsqueda

### Search Fields (`--search-fields`)

Define en qué campos se puede buscar en el listado.

**Sintaxis:** `"campo1,campo2,campo3"`

**Ejemplo:**
```bash
--search-fields "nombre,codigo,descripcion"
```

## ⚠️ Validaciones y Errores

### Validaciones Automáticas

El sistema valida automáticamente:

1. **Existencia de campos:** Todos los campos referenciados en UI deben existir en `--fields` o `--fk`
2. **Coherencia de lookups:** Los lookups deben corresponder a FKs definidas
3. **Existencia de tablas:** Las tablas referenciadas en FKs deben existir
4. **Tipos de datos:** Coherencia entre tipos BD y UI

### Errores Comunes

**Error: "Tabla referenciada 'categorias' no existe"**
```bash
# ❌ Incorrecto - tabla no existe
--fk "categoria_id:categorias"

# ✅ Correcto - usar nombre real de tabla
--fk "categoria_id:categoria"
```

**Error: "form-field 'precio' no existe en --fields"**
```bash
# ❌ Incorrecto - campo no definido en BD
--fields "nombre:string:255"
--form-fields "precio:required"

# ✅ Correcto - definir campo primero
--fields "nombre:string:255" "precio:decimal:18,2"
--form-fields "precio:required"
```

**Error: "lookup 'categoria_id' debe ser definido como FK"**
```bash
# ❌ Incorrecto - lookup sin FK
--fields "categoria_id:string:50"
--lookups "categoria_id:categoria:Nombre"

# ✅ Correcto - definir como FK
--fk "categoria_id:categoria"
--lookups "categoria_id:categoria:Nombre"
```

## 💡 Ejemplos Completos

### 📦 Ejemplo 1: Producto Simple

```bash
python3 tools/forms/entity-generator.py \
    --entity "Producto" \
    --plural "Productos" \
    --module "Inventario.Core" \
    --target todo \
    --fields "nombre:string:255" "codigo:string:50" "precio:decimal:18,2" "descripcion:text" \
    --form-fields "nombre:required:placeholder=Nombre del producto" \
                  "codigo:required:unique:placeholder=Código único" \
                  "precio:required:min=0:placeholder=0.00" \
    --grid-fields "nombre:200px:left:sf" \
                  "codigo:120px:left:s" \
                  "precio:120px:right:sf" \
    --search-fields "nombre,codigo,descripcion"
```

### 🛒 Ejemplo 2: Producto con Relaciones

```bash
python3 tools/forms/entity-generator.py \
    --entity "Producto" \
    --plural "Productos" \
    --module "Inventario.Core" \
    --target todo \
    --fields "nombre:string:255" \
             "codigo:string:50" \
             "precio:decimal:18,2" \
             "descripcion:text" \
             "stock_minimo:int" \
             "fecha_vencimiento:datetime" \
             "activo:bool" \
    --fk "categoria_id:categoria" \
         "proveedor_id:proveedor" \
    --form-fields "nombre:required:placeholder=Nombre del producto:min_length=3" \
                  "codigo:required:unique:placeholder=Código único" \
                  "precio:required:min=0:placeholder=Precio de venta" \
                  "stock_minimo:default=5:min=0:placeholder=Stock mínimo" \
                  "fecha_vencimiento:nullable:label=Fecha de vencimiento" \
    --grid-fields "nombre:200px:left:sf" \
                  "codigo:120px:left:s" \
                  "precio:120px:right:sf" \
                  "categoria_id->Categoria.Nombre:150px:left:f" \
                  "proveedor_id->Proveedor.RazonSocial:180px:left:f" \
                  "stock_minimo:100px:right:s" \
                  "activo:80px:center:f" \
    --readonly-fields "fecha_creacion:datetime:label=Creado el" \
                      "stock_actual:int:label=Stock disponible" \
    --lookups "categoria_id:categoria:Nombre:required:cache:form,grid" \
              "proveedor_id:proveedor:RazonSocial:required:fast:form,grid" \
    --search-fields "nombre,codigo,descripcion"
```

### 🔗 Ejemplo 3: Relación NN (Muchos-a-Muchos) - NUEVA SINTAXIS

**Sintaxis elegante para relaciones Many-to-Many:**

```bash
# Relación simple Venta ↔ Productos
python3 tools/forms/entity-generator.py \
    --source venta \
    --to productos \
    --module "Ventas" \
    --target db \
    --fields "cantidad:int" \
             "precio_unitario:decimal:18,2" \
             "descuento:decimal:5,2" \
    --fk "venta_id:venta" \
         "producto_id:producto"

# Relación con alias (para casos especiales)
python3 tools/forms/entity-generator.py \
    --source venta \
    --to productos \
    --alias promocion \
    --module "Ventas" \
    --target db \
    --fields "cantidad:int" \
             "precio_promocional:decimal:18,2" \
             "descuento:decimal:5,2" \
    --fk "venta_id:venta" \
         "producto_id:producto"
```

**Resultado:**
- **Tabla:** `nn_venta_productos` o `nn_venta_productos_promocion`
- **Modelo:** `Shared.Models/Entities/NN/NnVentaProductos.cs`
- **Namespace:** `Shared.Models.Entities.NN`
- **Permisos especiales:** `VENTA.ADDTARGET`, `VENTA.DELETETARGET`, `VENTA.EDITTARGET`

### 👤 Ejemplo 3: Cliente con Validaciones Avanzadas

```bash
python3 tools/forms/entity-generator.py \
    --entity "Cliente" \
    --plural "Clientes" \
    --module "Ventas.Core" \
    --target todo \
    --fields "razon_social:string:200" \
             "ruc:string:20" \
             "telefono:string:20" \
             "email:string:100" \
             "direccion:text" \
             "limite_credito:decimal:18,2" \
             "dias_credito:int" \
    --fk "tipo_cliente_id:tipo_cliente" \
    --form-fields "razon_social:required:placeholder=Razón social o nombre completo:min_length=3" \
                  "ruc:required:unique:placeholder=RUC/Cédula:min_length=10:max_length=20" \
                  "telefono:placeholder=Número de teléfono" \
                  "email:placeholder=correo@ejemplo.com" \
                  "limite_credito:default=0:min=0:placeholder=Límite de crédito" \
                  "dias_credito:default=30:min=0:max=365:placeholder=Días de crédito" \
    --grid-fields "razon_social:250px:left:sf" \
                  "ruc:120px:left:sf" \
                  "telefono:120px:left:f" \
                  "email:180px:left:f" \
                  "tipo_cliente_id->TipoCliente.Nombre:120px:left:f" \
                  "limite_credito:120px:right:s" \
                  "active:80px:center:f" \
    --readonly-fields "fecha_creacion:datetime:label=Cliente desde" \
                      "total_compras:decimal:label=Total compras:format=currency" \
                      "ultima_compra:datetime:label=Última compra" \
    --lookups "tipo_cliente_id:tipo_cliente:Nombre:required:cache:form,grid" \
    --search-fields "razon_social,ruc,telefono,email"
```

### 📋 Ejemplo 4: Solo Base de Datos

```bash
# Primero crear solo la tabla
python3 tools/forms/entity-generator.py \
    --entity "Categoria" \
    --module "Inventario.Core" \
    --target db \
    --fields "nombre:string:100" "descripcion:text" "orden:int" \
    --form-fields "nombre:required" "orden:default=1"

# Después crear la interfaz
python3 tools/forms/entity-generator.py \
    --entity "Categoria" \
    --module "Inventario.Core" \
    --target interfaz
```

## 🛠️ Resolución de Problemas

### Error: "No se encontró launchSettings.json"

**Causa:** El script no encuentra la configuración de conexión a BD.

**Solución:** Verifica que existe `Backend/Properties/launchSettings.json` con la variable `SQL` configurada.

### Error: "Foreign key references invalid table"

**Causa:** La tabla referenciada en el FK no existe.

**Solución:** 
1. Crear primero la tabla referenciada
2. Verificar el nombre exacto de la tabla (singular, minúscula)
3. Usar `--target db` para crear solo las tablas necesarias primero

### Error: "form-field no existe en --fields"

**Causa:** Referencia a un campo en UI que no está definido en BD.

**Solución:** Agregar el campo a `--fields` o `--fk` antes de referenciarlo.

### Encoding/Unicode Errors

**Causa:** Problemas con caracteres especiales en Windows.

**Solución:** El sistema maneja esto automáticamente, pero asegúrate de usar la terminal correcta.

### Permisos ya existen

**Información:** Normal si ya ejecutaste el script antes. El sistema detecta permisos existentes y solo crea los faltantes.

## 🎯 URLs Generadas

Después de generar una entidad completa, tendrás disponibles:

**Lista:** `/inventariocore/producto/list`
**Formulario:** `/inventariocore/producto/formulario`
**Creación rápida:** Disponible como componente independiente

## 📁 Archivos Generados

### Backend
- `Backend/Modules/{Modulo}/{Entidad}Controller.cs`
- `Backend/Modules/{Modulo}/{Entidad}Service.cs`
- `Backend/Services/ServiceRegistry.cs` (actualizado)

### Frontend
- `Frontend/Modules/{Modulo}/{Entidad}Service.cs`
- `Frontend/Modules/{Modulo}/{Entidad}ViewManager.cs`
- `Frontend/Modules/{Modulo}/{Entidad}List.razor + .cs`
- `Frontend/Modules/{Modulo}/{Entidad}Fast.razor + .cs`
- `Frontend/Modules/{Modulo}/{Entidad}Formulario.razor + .cs`
- `Frontend/Services/ServiceRegistry.cs` (actualizado)

### Base de Datos
- Tabla SQL con campos personalizados y BaseEntity
- 6 permisos del sistema automáticos
- Modelos EF Core sincronizados

---

## 📞 Soporte

Para reportar problemas o sugerir mejoras:
1. Revisa esta documentación completamente
2. Verifica los ejemplos similares a tu caso de uso
3. Asegúrate que las tablas referenciadas existan
4. Verifica la sintaxis de los parámetros

**¡El sistema está diseñado para generar entidades CRUD completas y funcionales con una sola línea de comandos!** 🚀