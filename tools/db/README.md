# 🛠️ Database Table Generator

Herramienta para crear tablas de base de datos automáticamente con los campos base del sistema.

## 🚀 Instalación y Configuración

```bash
# No requiere instalación adicional
# Usa Python estándar y lee configuración desde launchSettings.json
```

## 📋 Uso Básico

### **1. Crear Nueva Tabla (Solo BaseEntity)**
```bash
python tools/db/table.py --name "categories"
```
**Genera:**
- Id (UNIQUEIDENTIFIER, PK)
- OrganizationId (UNIQUEIDENTIFIER, FK a system_organization)
- FechaCreacion (DATETIME2, default GETUTCDATE())
- FechaModificacion (DATETIME2, default GETUTCDATE())
- CreadorId (UNIQUEIDENTIFIER, FK a system_users)
- ModificadorId (UNIQUEIDENTIFIER, FK a system_users)
- Active (BIT, default 1)

### **2. Agregar Campos a Tabla Existente**
```bash
python tools/db/table.py --addfield "categories" \
    --fields "descripcion:text" "imagen_url:string:500"
```
**Agrega solo los campos especificados:**
- descripcion (NVARCHAR(MAX))
- imagen_url (NVARCHAR(500))

---

## 🎯 Ejemplos Prácticos

### **1. Tabla de Productos**
```bash
python tools/db/table.py --name "products" \
    --fields "nombre:string:255" "descripcion:text" "precio:decimal:18,2" "stock:int" "codigo:string:50"
```
**Resultado:** BaseEntity + nombre, descripcion, precio, stock, codigo

### **2. Tabla de Clientes**
```bash
python tools/db/table.py --name "customers" \
    --fields "nombre:string:200" "email:string:255" "telefono:string:20" "direccion:text" "rut:string:12"
```
**Resultado:** BaseEntity + nombre, email, telefono, direccion, rut

### **3. Tabla de Órdenes con Relaciones**
```bash
python tools/db/table.py --name "orders" \
    --fields "numero:string:50" "fecha_orden:datetime" "total:decimal:18,2" "estado:string:20" \
    --fk "customer_id:customers"
```
**Resultado:** BaseEntity + campos + FK a customers

### **4. Tabla de Items de Orden**
```bash
python tools/db/table.py --name "order_items" \
    --fields "cantidad:int" "precio_unitario:decimal:18,2" "subtotal:decimal:18,2" \
    --fk "order_id:orders" "product_id:products"
```
**Resultado:** BaseEntity + campos + FK a orders y products

### **5. Tabla de Inventario**
```bash
python tools/db/table.py --name "inventory_movements" \
    --fields "tipo:string:20" "cantidad:int" "motivo:text" "fecha_movimiento:datetime" "documento:string:100" \
    --fk "product_id:products" "user_id:system_users"
```

### **6. Tabla de Configuración**
```bash
python tools/db/table.py --name "app_settings" \
    --fields "clave:string:100" "valor:text" "descripcion:string:500" "es_publico:bool"
```

---

## 📊 Tipos de Datos Soportados

| Tipo | Sintaxis | SQL Generado | Ejemplo |
|------|----------|--------------|---------|
| **Texto Corto** | `string:255` | `NVARCHAR(255)` | `nombre:string:100` |
| **Texto Largo** | `text` | `NVARCHAR(MAX)` | `descripcion:text` |
| **Número Entero** | `int` | `INT` | `stock:int` |
| **Decimal** | `decimal:18,2` | `DECIMAL(18,2)` | `precio:decimal:18,2` |
| **Fecha/Hora** | `datetime` | `DATETIME2` | `fecha_venta:datetime` |
| **Booleano** | `bool` | `BIT` | `activo:bool` |
| **GUID** | `guid` | `UNIQUEIDENTIFIER` | `external_id:guid` |
| **AutoIncremental** | `autoincremental` | `NVARCHAR(255)` | `codigo:autoincremental` |

---

## 🔗 Relaciones (Foreign Keys)

### **Sintaxis:**
```bash
--fk "campo:tabla_referencia"
```

### **Ejemplos:**
```bash
# FK a tabla system_users
--fk "vendedor_id:system_users"

# FK a tabla customers  
--fk "customer_id:customers"

# Múltiples FK
--fk "customer_id:customers" "product_id:products" "category_id:categories"
```

**Genera automáticamente:**
- Constraint de Foreign Key
- Índice en el campo FK
- Campo con tipo UNIQUEIDENTIFIER

---

## 🔧 Opciones del Comando

| Opción | Descripción | Requerido | Ejemplo |
|--------|-------------|-----------|---------|
| `--name` | Nombre de la tabla | ✅ | `--name "products"` |
| `--fields` | Campos adicionales | ❌ | `--fields "nombre:string:255"` |
| `--fk` | Foreign keys | ❌ | `--fk "user_id:system_users"` |
| `--unique` | Campos únicos | ❌ | `--unique "email" "codigo"` |
| `--execute` | Ejecutar en BD | ❌ | `--execute` |
| `--preview` | Solo mostrar SQL | ❌ | `--preview` |

---

## 📝 Ejemplos Completos de Casos de Uso

### **Sistema de E-commerce**

```bash
# 1. Categorías
python tools/db/table.py --name "categories" \
    --fields "nombre:string:100" "descripcion:text" "imagen_url:string:500"

# 2. Productos
python tools/db/table.py --name "products" \
    --fields "nombre:string:255" "descripcion:text" "precio:decimal:18,2" "stock:int" "sku:string:50" \
    --fk "category_id:categories" \
    --unique "sku"

# 3. Clientes
python tools/db/table.py --name "customers" \
    --fields "nombre:string:200" "email:string:255" "telefono:string:20" "direccion:text" \
    --unique "email"

# 4. Órdenes
python tools/db/table.py --name "orders" \
    --fields "numero:string:50" "fecha_orden:datetime" "total:decimal:18,2" "estado:string:20" \
    --fk "customer_id:customers" \
    --unique "numero"

# 5. Items de Órdenes
python tools/db/table.py --name "order_items" \
    --fields "cantidad:int" "precio_unitario:decimal:18,2" "subtotal:decimal:18,2" \
    --fk "order_id:orders" "product_id:products"
```

### **Sistema de Inventario**

```bash
# 1. Bodegas
python tools/db/table.py --name "warehouses" \
    --fields "nombre:string:100" "direccion:text" "responsable:string:200"

# 2. Movimientos de Inventario
python tools/db/table.py --name "inventory_movements" \
    --fields "tipo:string:20" "cantidad:int" "motivo:text" "documento:string:100" \
    --fk "product_id:products" "warehouse_id:warehouses"

# 3. Stock por Bodega
python tools/db/table.py --name "warehouse_stock" \
    --fields "cantidad_actual:int" "cantidad_minima:int" "cantidad_maxima:int" \
    --fk "product_id:products" "warehouse_id:warehouses"
```

### **Sistema de CRM**

```bash
# 1. Leads
python tools/db/table.py --name "leads" \
    --fields "nombre:string:200" "email:string:255" "telefono:string:20" "fuente:string:50" "estado:string:20" \
    --fk "assigned_to:system_users"

# 2. Actividades
python tools/db/table.py --name "activities" \
    --fields "tipo:string:50" "titulo:string:255" "descripcion:text" "fecha_actividad:datetime" "completado:bool" \
    --fk "lead_id:leads" "assigned_to:system_users"

# 3. Oportunidades
python tools/db/table.py --name "opportunities" \
    --fields "titulo:string:255" "valor:decimal:18,2" "probabilidad:int" "fecha_cierre:datetime" "etapa:string:50" \
    --fk "lead_id:leads" "assigned_to:system_users"
```

---

## ⚡ Flujo de Trabajo Recomendado

### **1. Diseñar la tabla**
```bash
# Solo generar SQL para revisar
python tools/db/table.py --name "mi_tabla" --fields "campo:tipo" --preview
```

### **2. Ejecutar en base de datos**
```bash
# Crear la tabla
python tools/db/table.py --name "mi_tabla" --fields "campo:tipo" --execute
```

### **3. Regenerar modelos**
```bash
# Actualizar entidades .NET
python tools/dbsync/generate-models.py
```

### **4. Usar en el código**
```csharp
// Las nuevas entidades ya están disponibles
var query = await QueryService.For<MiTabla>()
    .Where(x => x.Active)
    .ToListAsync();

// Los campos autoincrementales tienen metadata [AutoIncremental]
// disponible por reflection
```

---

## 🎯 Características Automáticas

### **Siempre incluye:**
- ✅ Todos los campos de BaseEntity
- ✅ Primary Key en campo Id
- ✅ Foreign Keys a system_organization, system_users
- ✅ Defaults para fechas (GETUTCDATE())
- ✅ Default para Active (1)
- ✅ Naming convention consistente

### **Validaciones:**
- ✅ Nombres de tabla válidos
- ✅ Tipos de datos soportados
- ✅ Referencias FK existentes
- ✅ Sintaxis de campos correcta

---

## 🚨 Notas Importantes

1. **Naming Convention:** Usa snake_case para nombres de tabla
2. **BaseEntity:** Siempre se incluyen los campos base del sistema
3. **Foreign Keys:** Deben referenciar tablas existentes
4. **Unique Fields:** Se crean como constraints únicos
5. **Preview Mode:** Usa `--preview` para ver SQL antes de ejecutar

---

## 🔍 Troubleshooting

### **Error: "Tabla no encontrada para FK"**
```bash
# Asegúrate que la tabla referenciada existe
python tools/db/table.py --name "orders" --fk "customer_id:customers"
# customers debe existir antes
```

### **Error: "Tipo de dato inválido"**
```bash
# Usa tipos soportados
python tools/db/table.py --name "test" --fields "campo:varchar:255"  # ❌ Incorrecto
python tools/db/table.py --name "test" --fields "campo:string:255"   # ✅ Correcto
```

### **Error de conexión**
```bash
# Verifica launchSettings.json tenga la variable SQL configurada
```

---

## 🔢 Campos AutoIncrementales

### **¿Qué son?**
Campos que generan códigos automáticamente con prefijo + número secuencial.

### **Ejemplo de uso:**
```bash
python tools/db/table.py --name "productos" \
    --fields "nombre:string:255" "codigo:autoincremental" \
    --execute
```

### **Lo que sucede automáticamente:**
1. **Crear campo** `codigo` como `NVARCHAR(255)` en la tabla
2. **Insertar en system_config**:
   - `productos.codigo.suffix` → `varchar`
   - `productos.codigo.number` → `int`  
3. **Sincronizar modelos** EF Core
4. **Agregar metadata** `[AutoIncremental]` al campo

### **Resultado en .NET:**
```csharp
// productos.Metadata.cs (generado automáticamente)
public class ProductosMetadata 
{
    [AutoIncremental]
    public string Codigo;
}

// Disponible por reflection:
var attr = typeof(Productos).GetProperty("Codigo")
    .GetCustomAttribute<AutoIncrementalAttribute>();
```

### **Configuración generada en BD:**
| Field | TypeField | Value |
|-------|-----------|-------|
| `productos.codigo.suffix` | `varchar` | Prefijo (ej: "PROD") |
| `productos.codigo.number` | `int` | Contador (ej: 1, 2, 3...) |

**Resultado final:** `PROD001`, `PROD002`, `PROD003`...

---

## 📈 Ejemplo de Output

```sql
-- Ejemplo de SQL generado para:
-- python tools/db/table.py --name "products" --fields "nombre:string:255" "precio:decimal:18,2"

CREATE TABLE products (
    Id UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
    OrganizationId UNIQUEIDENTIFIER NULL,
    FechaCreacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
    FechaModificacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
    CreadorId UNIQUEIDENTIFIER NULL,
    ModificadorId UNIQUEIDENTIFIER NULL,
    Active BIT DEFAULT 1 NOT NULL,
    
    -- Campos personalizados
    nombre NVARCHAR(255) NOT NULL,
    precio DECIMAL(18,2) NOT NULL,
    
    -- Foreign Keys
    CONSTRAINT FK_products_OrganizationId 
        FOREIGN KEY (OrganizationId) REFERENCES system_organization(Id),
    CONSTRAINT FK_products_CreadorId 
        FOREIGN KEY (CreadorId) REFERENCES system_users(Id),
    CONSTRAINT FK_products_ModificadorId 
        FOREIGN KEY (ModificadorId) REFERENCES system_users(Id)
);
```