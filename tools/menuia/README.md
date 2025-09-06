# 🤖 MenuIA - Generador Inteligente de Menús

Sistema inteligente para crear y gestionar menús en aplicaciones Blazor de forma automática y escalable.

## 🚀 Características

- ✅ **Generación individual** de menús con `menu_ai.py`
- ✅ **Generación masiva** con `generate_menu_batch.py` 
- ✅ **IA contextual** que elige iconos y ubicaciones inteligentemente
- ✅ **Auto-registro** en MenuUnificado.razor
- ✅ **Gestión de permisos** automática
- ✅ **Soporte para módulos/submódulos**

## 📁 Estructura de Archivos

```
tools/menuia/
├── context.md              # Contexto para la IA
├── menu_ai.py             # Generador individual 
├── generate_menu.py       # Generador individual con auto-creación
├── generate_menu_batch.py # Generador masivo inteligente
└── README.md             # Esta documentación
```

## 🎯 Uso Individual

### 1. Agregar una entidad específica

```bash
python tools/menuia/generate_menu.py modulo/entidad
python tools/menuia/generate_menu.py modulo/submodulo/entidad
```

**Ejemplos:**
```bash
# Agregar Productos al módulo Principal
python tools/menuia/generate_menu.py principal/productos

# Agregar Marcas al módulo Inventario con submódulo
python tools/menuia/generate_menu.py inventario/core/marcas

# Agregar Usuarios al módulo Administración  
python tools/menuia/generate_menu.py administracion/usuarios
```

### 2. Modificar menú existente

Si solo quieres agregar a un archivo existente sin crear módulos:

```bash
python tools/menuia/menu_ai.py ruta/archivo.cs "Entidad Nueva" "PERMISO.VIEWMENU"
```

## 🚀 Uso Masivo (Recomendado para múltiples entidades)

### 1. Crear archivo de entidades

Crea un archivo de texto (ej: `mis_entidades.txt`) con todas las entidades:

```txt
# Sistema de Inventario Completo
principal/productos
principal/categorias
principal/clientes
principal/proveedores

inventario/core/marcas
inventario/core/unidades
inventario/core/ubicaciones
inventario/stock/almacenes
inventario/stock/inventarios
inventario/stock/ajustes

administracion/usuarios
administracion/roles
administracion/permisos
administracion/configuracion

ventas/facturas
ventas/cotizaciones
ventas/clientes
ventas/descuentos

compras/ordenes
compras/recepciones
compras/proveedores
compras/gastos

reportes/ventas
reportes/inventario
reportes/financieros
reportes/auditoria
```

### 2. Ejecutar procesamiento masivo

```bash
python tools/menuia/generate_menu_batch.py mis_entidades.txt
```

### 3. Resultado del procesamiento masivo

```
🚀 GenerateMenu BATCH - Procesador masivo de menús
============================================================
📄 Procesando archivo: mis_entidades.txt  
✅ Se encontraron 24 entidades válidas

📋 Resumen de entidades:
  🔹 Productos → /principal/productos/list (IA elegirá icono)
  🔹 Categorias → /principal/categorias/list (IA elegirá icono)
  🔹 Marcas → /inventario/core/marcas/list (IA elegirá icono)
  ...

¿Procesar 24 entidades? (s/n): s

🗂️ Se procesarán 6 módulos:
  📁 Principal (existente) - 4 entidades
  📁 Inventario (nuevo) - 6 entidades  
  📁 Administracion (existente) - 4 entidades
  📁 Ventas (nuevo) - 4 entidades
  📁 Compras (nuevo) - 4 entidades
  📁 Reportes (nuevo) - 2 entidades

📁 Procesando módulo: inventario
   Entidades: 6
   📝 Creando InventarioMenuConfig.cs...
   ✅ InventarioMenuConfig.cs creado

🔗 Registrando 4 módulos nuevos en MenuUnificado.razor...
✅ Módulos registrados en MenuUnificado.razor

🎉 PROCESAMIENTO MASIVO COMPLETADO!
✅ 24 entidades procesadas
✅ 6 módulos actualizados  
✅ 4 módulos nuevos registrados
```

## 🧠 Inteligencia Artificial

### Decisiones Automáticas

La IA toma decisiones inteligentes sobre:

**📱 Iconos por Entidad:**
- `productos` → `inventory` 📦
- `usuarios` → `people` 👥  
- `reportes` → `analytics` 📊
- `categorias` → `category` 🏷️
- `ventas` → `point_of_sale` 💰
- `configuracion` → `settings` ⚙️

**📁 Módulos por Función:**
- Operaciones diarias → `principal`
- Gestión de stock → `inventario` 
- Administración → `administracion`
- Transacciones → `ventas`/`compras`

**🔗 Rutas Consistentes:**
- Patrón estándar: `/modulo/entidad/list`
- Con submódulo: `/modulo/submodulo/entidad/list`
- Siguiendo convenciones existentes

**🔐 Permisos Automáticos:**
- Formato estándar: `[ENTIDAD].VIEWMENU`
- Siempre en mayúsculas
- Ej: `PRODUCTOS.VIEWMENU`, `USUARIOS.VIEWMENU`

### Contexto Inteligente (context.md)

El archivo `context.md` contiene:
- Mapeo detallado de iconos por categoría
- Reglas para decidir módulos apropiados
- Patrones de nomenclatura y rutas
- Instrucciones específicas para la IA

## 📊 Archivos Generados

### Estructura de MenuModule

```csharp
public static class InventarioMenuConfig
{
    public static MenuModule GetMenuModule()
    {
        return new MenuModule
        {
            Text = "📊 Inventario",
            Icon = "inventory",
            MenuItems = new List<MenuItem>
            {
                new MenuItem
                {
                    Text = "Productos",
                    Icon = "inventory",
                    Path = "/inventario/productos/list",
                    Permissions = new List<string>{ "PRODUCTOS.VIEWMENU"}
                },
                new MenuItem
                {
                    Text = "Marcas", 
                    Icon = "label",
                    Path = "/inventario/core/marcas/list",
                    Permissions = new List<string>{ "MARCAS.VIEWMENU"}
                }
            }
        };
    }
}
```

### Registro en MenuUnificado.razor

```csharp
private async Task LoadModules()
{
    try
    {
        var principalModule = PrincipalMenuConfig.GetMenuModule();
        var adminModule = AdministracionMenuConfig.GetMenuModule();
        var inventarioModule = InventarioMenuConfig.GetMenuModule(); // ← Auto-agregado
        var ventasModule = VentasMenuConfig.GetMenuModule();         // ← Auto-agregado
        
        allModules = new List<MenuModule>
        {
            principalModule,
            adminModule,
            inventarioModule,
            ventasModule
        };
    }
    catch (Exception ex)
    {
        allModules = new List<MenuModule>();
    }
}
```

## 🔧 Comandos Adicionales

### Ayuda
```bash
python tools/menuia/generate_menu.py --help
python tools/menuia/generate_menu_batch.py --help
```

### Crear archivo de ejemplo
```bash
python tools/menuia/generate_menu_batch.py --example
```

## ⚡ Ventajas del Procesamiento Masivo

1. **🚀 Velocidad**: Procesa 20+ entidades en segundos
2. **🎯 Contexto completo**: La IA ve TODAS las entidades juntas
3. **🧠 Decisiones inteligentes**: Iconos y ubicaciones coherentes
4. **📊 Agrupación eficiente**: Una operación por módulo
5. **🔄 Consistencia**: Patrones uniformes en toda la aplicación
6. **📋 Resumen completo**: Visibilidad total del proceso

## 🎯 Casos de Uso

### Startup/MVP Rápido
```bash
# 5 módulos básicos en segundos
python tools/menuia/generate_menu_batch.py startup_entities.txt
```

### Sistema Empresarial Completo  
```bash
# 50+ entidades organizadas inteligentemente
python tools/menuia/generate_menu_batch.py enterprise_system.txt
```

### Módulo Específico
```bash
# Solo inventario con 15 entidades
python tools/menuia/generate_menu_batch.py inventario_completo.txt
```

## 🔍 Troubleshooting

**❌ Error de encoding:**
- El script maneja automáticamente emojis en Windows

**❌ Archivo no encontrado:**
- Verificar rutas relativas desde la raíz del proyecto

**❌ IA no responde:**
- Verificar configuración de claude_code_sdk
- Revisar permisos de herramientas en opciones

---

**¡MenuIA hace que crear menús complejos sea súper rápido y sin errores! 🚀**