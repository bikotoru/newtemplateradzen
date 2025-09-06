# Sistema de Planificación Inteligente con Gestión de Fases

## Variables
- **SOLICITUD_USUARIO**: $ARGUMENTS (descripción en lenguaje natural)
- **RAIZ_PROYECTO**: Directorio actual de trabajo
- **DOCS_DISPONIBLES**: ./docs/ (markdown files de referencia)

## 🎯 Propósito
Sistema inteligente de 2 fases que detecta la intención del usuario y ejecuta la implementación correspondiente con gestión automática de dependencias y archivos de seguimiento.

## 🚨 REGLAS CRÍTICAS

### 1. Gestión de Archivos de Implementación
```yaml
ESTRUCTURA_OBLIGATORIA:
  implementation/
  ├── {timestamp}_{nombre_solicitud}/
  │   ├── plan.md                    # Plan general
  │   ├── fase1.md                   # Fase 1 de implementación
  │   ├── fase2.md                   # Fase 2 de implementación (si aplica)
  │   ├── fase3.md                   # Fase N de implementación
  │   └── status.md                  # Estado y seguimiento

NAMING_CONVENTION:
  timestamp: YYYYMMDD_HHMMSS
  nombre_solicitud: snake_case_descriptivo
```

### 2. Orden de Dependencias para Entidades
```yaml
ORDEN_OBLIGATORIO:
  FASE_1_BASE: [entidades_sin_relaciones]
  FASE_2_LOOKUP: [entidades_con_foreign_keys_simples]
  FASE_3_COMPLEJAS: [entidades_transaccionales]
  FASE_N_RELACIONES: [tablas_nn_sin_interfaz]

EJEMPLO_CORRECTO:
  Solicitud: "Crear producto, categoria y marca"
  Orden detectado:
    Fase 1: marca, categoria
    Fase 2: producto (→marca, →categoria)
```

### 3. Detección de Tipos de Entidades
```yaml
ENTIDADES_SOLO_TABLA:
  - Tablas N:N (nn_venta_producto, nn_usuario_rol)
  - Tablas de auditoría (audit_*, log_*)
  - Tablas de sistema (system_*)
  
ENTIDADES_CON_INTERFAZ:
  - Entidades principales de negocio
  - Tablas de configuración con CRUD
  - Entidades transaccionales

BASE_ENTITY_CAMPOS_AUTOMÁTICOS:
  - Id: Guid (Primary Key)
  - OrganizationId: Guid? (Nullable)
  - FechaCreacion: DateTime
  - FechaModificacion: DateTime
  - CreadorId: Guid
  - ModificadorId: Guid
  - Active: bool

NOTA: Todos los comandos generan automáticamente estos campos base
```

## 🔍 FASE 0: DETECCIÓN DE INTENCIÓN

### Análisis Inicial
```python
def detectar_intencion_solicitud(solicitud_usuario):
    """
    Analiza la solicitud del usuario para determinar qué documentos consultar
    """
    
    # PATRONES DE PERSONALIZACIÓN/MEJORA
    if any(keyword in solicitud_usuario.lower() for keyword in [
        "personalizar", "mejorar", "modificar", "cambiar", "basándome en",
        "mockup", "prototipo", "html", "css", "diseño", "formulario existente"
    ]):
        docs_relevantes = ["6.DesignForms.md", "7.Lookup-Component.md", "8.EntityTable-Component.md"]
        tipo_solicitud = "PERSONALIZACIÓN"
        
    # PATRONES DE CREACIÓN NUEVA
    elif any(keyword in solicitud_usuario.lower() for keyword in [
        "crear", "generar", "nuevo", "sistema de", "módulo", "entidad"
    ]):
        docs_relevantes = ["auto-generacion-with-python-script.md", "2.Create-Update.md"]
        tipo_solicitud = "CREACIÓN_NUEVA"
        
    # PATRONES DE BÚSQUEDA/FILTRADO
    elif any(keyword in solicitud_usuario.lower() for keyword in [
        "buscar", "filtrar", "listar", "mostrar", "consultar"
    ]):
        docs_relevantes = ["1.Search.md", "8.EntityTable-Component.md"]
        tipo_solicitud = "BÚSQUEDA_FILTRADO"
        
    # PATRONES DE EXPORTACIÓN
    elif any(keyword in solicitud_usuario.lower() for keyword in [
        "exportar", "excel", "pdf", "reporte", "descarga"
    ]):
        docs_relevantes = ["4.Excel-Export.md", "3.Batch.md"]
        tipo_solicitud = "EXPORTACIÓN"
        
    # PATRONES DE VALIDACIÓN
    elif any(keyword in solicitud_usuario.lower() for keyword in [
        "validar", "validación", "reglas", "restricciones"
    ]):
        docs_relevantes = ["ValidationSystem.md", "2.Create-Update.md"]
        tipo_solicitud = "VALIDACIÓN"
        
    # PATRONES MIXTOS
    else:
        # Analizar combinaciones múltiples
        docs_relevantes = determinar_docs_combinados(solicitud_usuario)
        tipo_solicitud = "MIXTO"
    
    return {
        "tipo": tipo_solicitud,
        "docs_a_consultar": docs_relevantes,
        "requiere_implementacion": True
    }
```

### Consulta de Documentación
```markdown
🔍 CONSULTA AUTOMÁTICA DE DOCUMENTOS

PARA cada documento en docs_a_consultar:
  LEER ./docs/{documento}
  EXTRAER patrones y requisitos aplicables
  IDENTIFICAR componentes y herramientas necesarias
  
RESULTADO: Base de conocimiento contextual para implementación
```

## 🏗️ FASE 1: IMPLEMENTACIÓN

### Análisis y Planificación
```python
def analizar_y_planificar_implementacion(solicitud_usuario, docs_consultados):
    """
    Genera plan detallado de implementación basado en la solicitud y documentación
    """
    
    # 1. EXTRAER ENTIDADES Y COMPONENTES
    entidades_detectadas = extraer_entidades(solicitud_usuario)
    
    # 2. ANALIZAR DEPENDENCIAS
    grafo_dependencias = construir_grafo_dependencias(entidades_detectadas)
    
    # 3. DETERMINAR FASES
    fases = generar_fases_ordenadas(grafo_dependencias)
    
    # 4. CLASIFICAR TIPO DE IMPLEMENTACIÓN
    for entidad in entidades_detectadas:
        if es_tabla_nn(entidad):
            entidad.tipo = "SOLO_TABLA"
            entidad.requiere_interfaz = False
        else:
            entidad.tipo = "CON_INTERFAZ"
            entidad.requiere_interfaz = True
    
    return {
        "entidades": entidades_detectadas,
        "fases": fases,
        "comandos_python": generar_comandos_por_fase(fases),
        "archivos_implementacion": generar_estructura_archivos()
    }
```

### Creación de Archivos de Implementación
```markdown
📁 ESTRUCTURA DE ARCHIVOS GENERADA AUTOMÁTICAMENTE:

implementation/{timestamp}_{nombre_solicitud}/
├── plan.md           # Plan general con análisis completo
├── fase1.md          # Comandos Python para entidades base
├── fase2.md          # Comandos Python para entidades dependientes
├── fase3.md          # Comandos Python para entidades complejas
├── faseN.md          # Comandos Python para relaciones N:N
└── status.md         # Estado de ejecución y seguimiento

CONTENIDO DE CADA FASE:
- Lista de entidades a crear
- Comandos Python específicos listos para ejecutar
- Orden de ejecución
- Validaciones post-creación
```

### Template de Archivo de Fase
```markdown
# Fase {N}: {Descripción}

## Entidades de esta Fase
{lista_entidades_con_justificacion}

## Comandos Python a Ejecutar (Herramienta Real: entity-generator.py)

### {Entidad1} - Entidad Simple
```bash
# Crear entidad completa: BD + Interfaz
python3 tools/forms/entity-generator.py \
    --entity "{Entidad1}" \
    --plural "{Entidad1}s" \
    --module "{Modulo}" \
    --target todo \
    --fields "{campos_personalizados}" \
    --form-fields "{configuracion_formulario}" \
    --grid-fields "{configuracion_grilla}" \
    --search-fields "{campos_busqueda}"
```

### {Entidad2} - Entidad con Relaciones
```bash
# Crear entidad con Foreign Keys y Lookups
python3 tools/forms/entity-generator.py \
    --entity "{Entidad2}" \
    --plural "{Entidad2}s" \
    --module "{Modulo}" \
    --target todo \
    --fields "{campos_personalizados}" \
    --fk "{foreign_keys}" \
    --form-fields "{configuracion_formulario}" \
    --grid-fields "{configuracion_grilla_con_lookups}" \
    --lookups "{configuracion_lookups}" \
    --search-fields "{campos_busqueda}"
```

## Campos BaseEntity Automáticos
**NOTA**: Estos campos se agregan automáticamente a TODAS las entidades:
- `Id` (Guid) - Primary Key
- `OrganizationId` (Guid?) - Organización (nullable)
- `FechaCreacion` (DateTime) - Fecha de creación
- `FechaModificacion` (DateTime) - Fecha de modificación
- `CreadorId` (Guid) - Usuario creador
- `ModificadorId` (Guid) - Usuario modificador
- `Active` (bool) - Estado activo/inactivo

## Validaciones Post-Ejecución
- [ ] Tabla SQL creada con campos BaseEntity automáticos
- [ ] Compilación exitosa: `dotnet build --no-restore`
- [ ] 6 permisos del sistema creados automáticamente
- [ ] Interfaces CRUD funcionales generadas
- [ ] Lookups funcionando correctamente (si aplica)

## Notas Especiales
{notas_especificas_de_la_fase}
```

### Detección Automática de Dependencias
```python
def construir_grafo_dependencias(entidades):
    """
    Construye grafo de dependencias basado en relaciones detectadas
    """
    
    grafo = {}
    
    for entidad in entidades:
        grafo[entidad.nombre] = {
            "dependencias": [],
            "nivel": 0,
            "campos": entidad.campos
        }
        
        # Analizar campos para encontrar foreign keys
        for campo in entidad.campos:
            if es_foreign_key(campo):
                entidad_referenciada = extraer_entidad_referenciada(campo)
                grafo[entidad.nombre]["dependencias"].append(entidad_referenciada)
    
    # Calcular niveles de dependencia
    for entidad in grafo:
        grafo[entidad]["nivel"] = calcular_nivel_dependencia(entidad, grafo)
    
    return grafo

def generar_fases_ordenadas(grafo):
    """
    Genera fases ordenadas por nivel de dependencia
    """
    
    niveles = {}
    
    for entidad, info in grafo.items():
        nivel = info["nivel"]
        if nivel not in niveles:
            niveles[nivel] = []
        niveles[nivel].append(entidad)
    
    # Generar fases
    fases = []
    for nivel in sorted(niveles.keys()):
        fases.append({
            "numero": len(fases) + 1,
            "descripcion": f"Entidades de nivel {nivel}",
            "entidades": niveles[nivel]
        })
    
    return fases
```

### Generación de Comandos Python
```python
def generar_comandos_por_fase(fases, entidades):
    """
    Genera comandos Python específicos para cada fase
    """
    
    comandos_por_fase = {}
    
    for fase in fases:
        comandos = []
        
        for nombre_entidad in fase["entidades"]:
            entidad = encontrar_entidad(nombre_entidad, entidades)
            
            if entidad.tipo == "SOLO_TABLA":
                # Tabla N:N - usar generate_nn_relation.py
                comando = generar_comando_nn(entidad)
            else:
                # Entidad normal - usar generate_module_simple.py
                comando = generar_comando_entidad(entidad)
            
            comandos.append(comando)
        
        comandos_por_fase[f"fase{fase['numero']}"] = comandos
    
    return comandos_por_fase

def generar_comando_entidad(entidad):
    """
    Genera comando específico usando entity-generator.py (herramienta real)
    """
    
    # Generar campos personalizados (sin BaseEntity que se agrega automáticamente)
    campos_personalizados = " ".join([
        f'"{campo.nombre}:{campo.tipo}:{campo.tamaño or ""}"'
        for campo in entidad.campos if not campo.es_base_entity
    ])
    
    # Generar foreign keys
    foreign_keys = " ".join([
        f'"{campo.nombre}:{campo.tabla_referencia}"'
        for campo in entidad.campos if campo.es_fk
    ])
    
    # Generar configuración de formulario
    form_fields = " ".join([
        f'"{campo.nombre}:{":".join(campo.opciones_formulario)}"'
        for campo in entidad.campos if campo.opciones_formulario
    ])
    
    # Generar configuración de grilla
    grid_fields = " ".join([
        f'"{campo.nombre}:{campo.ancho_grilla}:{campo.alineacion}:{campo.opciones_grilla}"'
        for campo in entidad.campos if campo.mostrar_en_grilla
    ])
    
    # Generar lookups
    lookups = " ".join([
        f'"{campo.nombre}:{campo.tabla_referencia}:{campo.campo_display}:{":".join(campo.opciones_lookup)}"'
        for campo in entidad.campos if campo.es_lookup
    ])
    
    # Generar campos de búsqueda
    search_fields = ",".join([
        campo.nombre for campo in entidad.campos if campo.es_buscable
    ])
    
    # Construir comando base
    comando = f'''python3 tools/forms/entity-generator.py \\
    --entity "{entidad.nombre}" \\
    --plural "{entidad.plural or entidad.nombre + 's'}" \\
    --module "{entidad.modulo}" \\
    --target todo'''
    
    # Agregar parámetros opcionales si existen
    if campos_personalizados:
        comando += f''' \\
    --fields {campos_personalizados}'''
    
    if foreign_keys:
        comando += f''' \\
    --fk {foreign_keys}'''
    
    if form_fields:
        comando += f''' \\
    --form-fields {form_fields}'''
    
    if grid_fields:
        comando += f''' \\
    --grid-fields {grid_fields}'''
    
    if lookups:
        comando += f''' \\
    --lookups {lookups}'''
    
    if search_fields:
        comando += f''' \\
    --search-fields "{search_fields}"'''
    
    return comando

def generar_comando_nn(entidad):
    """
    Genera comando para relación N:N
    """
    
    return f'''python3 tools/generate_nn_relation.py "{entidad.nombre}" \\
  --main-entity "{entidad.entidad_principal}" \\
  --related-entity "{entidad.entidad_relacionada}" \\
  --custom-fields "{entidad.campos_custom}" \\
  --module {entidad.modulo} \\
  --yes'''
```

## 📋 FLUJO DE EJECUCIÓN COMPLETO

### Ejecución del Comando
```bash
# El usuario ejecuta:
@ai-plan "crear sistema de ventas con productos, categorías, clientes y ventas"
```

### Procesamiento Automático
```markdown
🔄 FASE 0 - DETECCIÓN (30 segundos):
├── 🔍 Analizar solicitud: "crear sistema de ventas..."
├── 📖 Consultar docs: auto-generacion-with-python-script.md, 2.Create-Update.md
├── 🎯 Tipo detectado: CREACIÓN_NUEVA
└── 📝 Documentos relevantes cargados

🔄 FASE 1 - IMPLEMENTACIÓN (60 segundos):
├── 🏗️ Extraer entidades: Producto, Categoria, Cliente, Venta
├── 📊 Analizar dependencias:
│   ├── Nivel 0: Categoria, Cliente
│   ├── Nivel 1: Producto (→Categoria)
│   └── Nivel 2: Venta (→Cliente, →Producto)
├── 📁 Crear estructura: implementation/20240306_143022_sistema_ventas/
├── 📝 Generar archivos:
│   ├── plan.md (análisis completo)
│   ├── fase1.md (Categoria, Cliente)
│   ├── fase2.md (Producto)
│   ├── fase3.md (Venta)
│   └── status.md (seguimiento)
└── ✅ Archivos listos para ejecución

📊 RESULTADO:
• Estructura completa generada
• 4 entidades detectadas
• 3 fases de implementación
• Comandos Python listos
• Orden de dependencias resuelto
```

## 🔧 DETECCIÓN INTELIGENTE DE PATRONES

### Reconocimiento de Entidades Comunes
```yaml
PATRONES_NEGOCIO:
  ventas: [cliente, producto, categoria, venta, detalle_venta]
  inventario: [producto, categoria, marca, almacen, movimiento]
  rrhh: [empleado, cargo, departamento, contrato]
  crm: [cliente, contacto, empresa, oportunidad]

RELACIONES_AUTOMÁTICAS:
  producto: [categoria, marca, proveedor]
  venta: [cliente, vendedor]
  empleado: [cargo, departamento]
  
TABLAS_NN_DETECTADAS:
  "venta con productos" → nn_venta_producto
  "usuario con roles" → nn_usuario_rol
  "empleado con proyectos" → nn_empleado_proyecto
```

### Asignación Automática de Módulos
```python
def detectar_modulo_automatico(entidad_nombre):
    """
    Detecta módulo apropiado basado en el nombre de la entidad
    """
    
    mapeo_modulos = {
        # Catálogo y productos
        ["producto", "categoria", "marca"]: "Catalogo",
        
        # Ventas y comercial
        ["venta", "cliente", "cotizacion", "factura"]: "Ventas",
        
        # Recursos humanos
        ["empleado", "cargo", "departamento"]: "RRHH",
        
        # Localidades
        ["region", "comuna", "direccion"]: "Core.Localidades",
        
        # Configuración
        ["configuracion", "parametro"]: "Admin.Config"
    }
    
    for palabras_clave, modulo in mapeo_modulos.items():
        if any(palabra in entidad_nombre.lower() for palabra in palabras_clave):
            return modulo
    
    return "General"  # Módulo por defecto
```

## ✅ VALIDACIONES Y CONTROLES

### Validaciones Pre-Implementación
```python
def validar_solicitud(entidades_detectadas):
    """
    Validaciones antes de generar archivos de implementación
    """
    
    errores = []
    advertencias = []
    
    for entidad in entidades_detectadas:
        # Validar nombres
        if not es_nombre_valido(entidad.nombre):
            errores.append(f"Nombre inválido: {entidad.nombre}")
        
        # Validar dependencias
        for dep in entidad.dependencias:
            if not existe_dependencia(dep):
                errores.append(f"Dependencia faltante: {dep}")
    
    return {
        "valida": len(errores) == 0,
        "errores": errores,
        "advertencias": advertencias
    }
```

## 📊 EJEMPLO DE ARCHIVO GENERADO

### plan.md
```markdown
# Plan de Implementación: Sistema de Ventas

## 📋 Análisis de Solicitud
**Solicitud Original**: "crear sistema de ventas con productos, categorías, clientes y ventas"

## 🎯 Entidades Detectadas
1. **Categoria** (Catálogo) - Base
2. **Cliente** (Ventas) - Base  
3. **Producto** (Catálogo) - Dependiente de Categoria
4. **Venta** (Ventas) - Dependiente de Cliente

## 📊 Fases de Implementación

### Fase 1: Entidades Base
- Categoria (sin dependencias)
- Cliente (sin dependencias)

### Fase 2: Entidades Dependientes
- Producto (→ Categoria)

### Fase 3: Entidades Transaccionales
- Venta (→ Cliente)

## 🔄 Estado de Ejecución
- [ ] Fase 1 completada
- [ ] Fase 2 completada  
- [ ] Fase 3 completada
- [ ] Compilación exitosa
- [ ] Migración aplicada
```

## 📊 EJEMPLOS COMPLETOS CON COMANDOS REALES

### Sistema E-commerce Básico

**SOLICITUD**: "crear sistema de ventas con productos, categorías, clientes y ventas"

**ENTIDADES DETECTADAS**: Categoria, Cliente, Producto, Venta
**DEPENDENCIAS**: Producto→Categoria, Venta→Cliente

#### Fase 1: Entidades Base (sin dependencias)

##### 1. Categoria - Entidad Simple
```bash
python3 tools/forms/entity-generator.py \
    --entity "Categoria" \
    --plural "Categorias" \
    --module "Catalogo.Core" \
    --target todo \
    --fields "nombre:string:100" "descripcion:text" "orden:int" \
    --form-fields "nombre:required:placeholder=Nombre de la categoría:min_length=2" \
                  "descripcion:placeholder=Descripción opcional" \
                  "orden:default=1:min=0:placeholder=Orden de visualización" \
    --grid-fields "nombre:200px:left:sf" \
                  "descripcion:300px:left:f" \
                  "orden:80px:center:s" \
                  "active:80px:center:f" \
    --search-fields "nombre,descripcion"
```

##### 2. Cliente - Entidad con Validaciones
```bash
python3 tools/forms/entity-generator.py \
    --entity "Cliente" \
    --plural "Clientes" \
    --module "Ventas.Core" \
    --target todo \
    --fields "razon_social:string:200" "ruc:string:20" "telefono:string:20" "email:string:100" "direccion:text" \
    --form-fields "razon_social:required:placeholder=Razón social o nombre completo:min_length=3" \
                  "ruc:required:unique:placeholder=RUC o Cédula de identidad" \
                  "telefono:placeholder=Número de teléfono" \
                  "email:placeholder=correo@ejemplo.com" \
                  "direccion:placeholder=Dirección completa" \
    --grid-fields "razon_social:250px:left:sf" \
                  "ruc:120px:left:sf" \
                  "telefono:120px:left:f" \
                  "email:180px:left:f" \
                  "active:80px:center:f" \
    --search-fields "razon_social,ruc,telefono,email"
```

#### Fase 2: Entidades con Dependencias

##### 3. Producto - Entidad con Foreign Key y Lookup
```bash
python3 tools/forms/entity-generator.py \
    --entity "Producto" \
    --plural "Productos" \
    --module "Catalogo.Core" \
    --target todo \
    --fields "nombre:string:255" "codigo:string:50" "precio:decimal:18,2" "descripcion:text" "stock_minimo:int" \
    --fk "categoria_id:categoria" \
    --form-fields "nombre:required:placeholder=Nombre del producto:min_length=3" \
                  "codigo:required:unique:placeholder=Código único del producto" \
                  "precio:required:min=0:placeholder=Precio de venta" \
                  "stock_minimo:default=5:min=0:placeholder=Stock mínimo requerido" \
    --grid-fields "nombre:200px:left:sf" \
                  "codigo:120px:left:s" \
                  "precio:120px:right:sf" \
                  "categoria_id->Categoria.Nombre:150px:left:f" \
                  "stock_minimo:100px:right:s" \
                  "active:80px:center:f" \
    --lookups "categoria_id:categoria:Nombre:required:cache:form,grid" \
    --search-fields "nombre,codigo,descripcion"
```

##### 4. Venta - Entidad Transaccional
```bash
python3 tools/forms/entity-generator.py \
    --entity "Venta" \
    --plural "Ventas" \
    --module "Ventas.Core" \
    --target todo \
    --fields "numero:string:20" "fecha:datetime" "subtotal:decimal:18,2" "impuesto:decimal:18,2" "total:decimal:18,2" "observaciones:text" \
    --fk "cliente_id:cliente" \
    --form-fields "numero:required:unique:placeholder=Número de venta" \
                  "fecha:required:label=Fecha de venta" \
                  "subtotal:required:min=0:placeholder=Subtotal" \
                  "impuesto:default=0:min=0:placeholder=Impuestos" \
                  "total:required:min=0:placeholder=Total de la venta" \
                  "observaciones:placeholder=Observaciones adicionales" \
    --grid-fields "numero:120px:left:sf" \
                  "fecha:120px:center:sf" \
                  "cliente_id->Cliente.RazonSocial:200px:left:f" \
                  "total:120px:right:sf" \
                  "active:80px:center:f" \
    --readonly-fields "total:decimal:label=Total calculado:format=currency" \
    --lookups "cliente_id:cliente:RazonSocial:required:fast:form,grid" \
    --search-fields "numero,observaciones"
```

### Ejemplo de Entidad Solo Base de Datos

**Para casos donde solo necesitas la tabla (ej: configuraciones):**

```bash
# Solo crear tabla y permisos
python3 tools/forms/entity-generator.py \
    --entity "TipoDocumento" \
    --module "Admin.Config" \
    --target db \
    --fields "nombre:string:50" "codigo:string:10" "descripcion:text"

# Después crear la interfaz si es necesaria
python3 tools/forms/entity-generator.py \
    --entity "TipoDocumento" \
    --module "Admin.Config" \
    --target interfaz
```

## 🏗️ CAMPOS BASE ENTITY AUTOMÁTICOS

**IMPORTANTE**: Todos los comandos generan automáticamente estos campos:

```csharp
public Guid Id { get; set; }                    // Primary Key
public Guid? OrganizationId { get; set; }       // Multi-tenant (nullable)
public DateTime FechaCreacion { get; set; }     // Fecha de creación
public DateTime FechaModificacion { get; set; } // Fecha de modificación
public Guid CreadorId { get; set; }             // Usuario que creó el registro
public Guid ModificadorId { get; set; }         // Usuario que modificó por última vez
public bool Active { get; set; }                // Estado activo/inactivo
```

**No es necesario especificar estos campos en `--fields`**, se agregan automáticamente a todas las entidades.

## 🎯 ARCHIVOS GENERADOS AUTOMÁTICAMENTE

Cada comando `--target todo` genera:

### Backend
- `Backend/Modules/{Modulo}/{Entidad}Controller.cs`
- `Backend/Modules/{Modulo}/{Entidad}Service.cs`
- Actualización de `ServiceRegistry.cs`

### Frontend
- `Frontend/Modules/{Modulo}/{Entidad}Service.cs`
- `Frontend/Modules/{Modulo}/{Entidad}ViewManager.cs`
- `Frontend/Modules/{Modulo}/{Entidad}List.razor + .cs`
- `Frontend/Modules/{Modulo}/{Entidad}Fast.razor + .cs`
- `Frontend/Modules/{Modulo}/{Entidad}Formulario.razor + .cs`
- Actualización de `ServiceRegistry.cs`

### Base de Datos
- Tabla SQL con BaseEntity + campos personalizados
- 6 permisos del sistema automáticos
- Modelos EF Core sincronizados

El comando estará listo para usar con `@ai-plan "tu solicitud aquí"` y generará toda la estructura de implementación automáticamente.

<function_calls>
<invoke name="TodoWrite">
<parameter name="todos">[{"id": "1", "content": "Analyze the example ai-plan-v2.md file structure", "status": "completed"}, {"id": "2", "content": "Review docs folder to understand available markdown files for requirements detection", "status": "completed"}, {"id": "3", "content": "Create the ai-plan.md command file with Phase 0 (Detection) and Phase 1 (Implementation)", "status": "completed"}]