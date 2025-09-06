# MenuIA - Contexto para Agregar Entidades al Menú

## Objetivo
Analizar un archivo de configuración de menú y agregar inteligentemente una nueva entidad con el icono, módulo y permisos correctos.

## Inputs Esperados
1. **path_archivo**: Ruta al archivo de configuración del menú (ej: `Frontend/Layout/Menu/Modules/PrincipalMenuConfig.cs`)
2. **nueva_entidad**: Nombre de la nueva entidad a agregar (ej: "Productos", "Usuarios", "Reportes")
3. **permiso**: Permiso requerido para acceder (ej: "PRODUCTOS.*", "ADMIN.USUARIOS", "*")

## Proceso Inteligente

### 1. Análisis del Archivo
- Leer y entender la estructura actual del menú
- Identificar patrones de nomenclatura
- Analizar iconos existentes y su lógica
- Determinar el estilo de rutas (ej: `/categoria/list`, `/admin/users`)

### 2. Decisiones Inteligentes por Nombre de Entidad

#### Iconos por Categoría:
- **Gestión/Admin**: `settings`, `admin_panel_settings`, `manage_accounts`
- **Productos/Inventario**: `inventory`, `shopping_cart`, `store`, `category`
- **Usuarios/Personas**: `people`, `person`, `account_box`, `group`
- **Reportes/Analytics**: `analytics`, `assessment`, `bar_chart`, `insights`
- **Finanzas/Dinero**: `account_balance`, `monetization_on`, `payment`, `receipt`
- **Documentos/Archivos**: `description`, `folder`, `file_copy`, `archive`
- **Comunicación**: `email`, `chat`, `notifications`, `campaign`
- **Configuración**: `settings`, `tune`, `build`, `construction`

#### Rutas por Patrón:
- Seguir el patrón existente (ej: `/{entidad-lowercase}/list`)
- Considerar si va en módulo principal o administrativo
- Mantener consistencia con rutas existentes

#### Módulo por Tipo:
- **Módulo Principal**: Operaciones diarias, CRUD básico
- **Módulo Administración**: Configuración, usuarios, permisos

### 3. Ubicación Inteligente
- Si la entidad es de "gestión de usuarios/roles/permisos" → Módulo Administración
- Si la entidad es operacional/CRUD → Módulo Principal
- Mantener orden alfabético dentro del módulo
- Considerar agrupación lógica por funcionalidad

### 4. Formato de Salida
Generar el código C# correcto manteniendo:
- Indentación y estilo del archivo original
- Comentarios si existen
- Estructura de la clase static
- Formato de MenuItem con todos los campos requeridos

## Ejemplos de Comportamiento Esperado

### Input:
- **nueva_entidad**: "Productos"  
- **permiso**: "PRODUCTOS.*"

### Output Inteligente:
```csharp
new MenuItem
{
    Text = "Productos",
    Icon = "inventory", // Icono inteligente para productos
    Path = "/productos/list", // Ruta siguiendo patrón
    Permissions = new List<string> { "PRODUCTOS.*" }
}
```

### Input:
- **nueva_entidad**: "Gestión de Roles"
- **permiso**: "ADMIN.ROLES"

### Output Inteligente:
- **Módulo**: AdministracionMenuConfig (por ser "gestión")
- **Icono**: "admin_panel_settings" (por ser roles administrativos)
- **Ruta**: "/admin/roles" (siguiendo patrón admin)

## Instrucciones para la IA

1. **SIEMPRE** leer primero el archivo completo para entender el contexto
2. **ANALIZAR** el patrón de nombres, iconos y rutas existentes
3. **DECIDIR** inteligentemente el módulo más apropiado
4. **SELECCIONAR** el icono que mejor represente la entidad
5. **GENERAR** la ruta siguiendo el patrón existente
6. **INSERTAR** en la posición correcta (orden alfabético o lógico)
7. **MANTENER** el estilo y formato del archivo original
8. **VERIFICAR** que no se dupliquen entradas

## Resultado Final
El archivo debe quedar perfectamente formateado, con la nueva entidad agregada en el lugar correcto, con icono apropiado y siguiendo todos los patrones existentes.