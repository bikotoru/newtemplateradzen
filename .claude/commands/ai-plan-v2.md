# Asistente de Planificación Inteligente de Entidades

## Variables
- **SOLICITUD_USUARIO**: $ARGUMENTS (descripción de entidades a crear)
- **IDIOMA**: Español (SIEMPRE responder en español)
- **DOCS_FOLDER**: ./docs/ (documentación de referencia)

## 🎯 Propósito
Asistente conversacional que:
1. Lee la documentación en `docs/` para entender entity-generator.py
2. Analiza la solicitud del usuario
3. Explica específicamente qué va a crear
4. Crea carpeta `implementation/` con archivos de fases para que el usuario ejecute manualmente

## 📋 FLUJO PRINCIPAL

### PASO 1: Leer Documentación
```markdown
LEER AUTOMÁTICAMENTE:
1. docs/auto-generacion-with-python-script.md (entity-generator.py)
2. Otros docs según el tipo de solicitud detectado

PROPÓSITO: Entender cómo funciona entity-generator.py y qué parámetros usar
```

### PASO 2: Análizar Solicitud del Usuario
```python
def procesar_solicitud(solicitud_usuario):
    """
    Analiza la solicitud y extrae entidades específicas basándose en la documentación leída
    """
    
    # Detectar entidades mencionadas
    entidades = extraer_entidades_de_texto(solicitud_usuario)
    
    # Analizar campos especificados
    campos_por_entidad = extraer_campos_por_entidad(solicitud_usuario)
    
    # Detectar relaciones
    relaciones = detectar_relaciones_entre_entidades(entidades, campos_por_entidad)
    
    # Determinar orden de dependencias
    orden_creacion = calcular_orden_dependencias(entidades, relaciones)
    
    return generar_respuesta_conversacional_y_crear_archivos(entidades, campos_por_entidad, orden_creacion)
```

### PASO 3: Respuesta Conversacional + Crear Archivos

#### Template de Respuesta
```markdown
Perfecto, voy a crear un sistema de {TIPO_SISTEMA} con {NUMERO} entidades:

📋 **ENTIDADES A CREAR:**

{PARA_CADA_ENTIDAD}:
• **{NOMBRE_ENTIDAD}**
  - Campos: {LISTA_CAMPOS}
  - Módulo: {MODULO_DETECTADO}
  {SI_TIENE_RELACIONES}: - Relaciones: → {ENTIDADES_RELACIONADAS}

🔧 **QUÉ VOY A GENERAR:**
- {NUMERO} tablas en la base de datos
- {NUMERO} controladores en Backend/
- {NUMERO} servicios en Backend/
- {NUMERO} formularios en Frontend/
- {NUMERO} listas/grillas en Frontend/
- Permisos del sistema automáticos para cada entidad
- Registros automáticos en system_form_entities para FormDesigner

⚡ **ORDEN DE CREACIÓN:**
{FASES_ORDENADAS}

📁 **ARCHIVOS DE IMPLEMENTACIÓN:**
Voy a crear la carpeta `implementation/` con los comandos organizados por fases:
- implementation/fase1.md (entidades base)
- implementation/fase2.md (entidades con relaciones)
- implementation/faseN.md (según dependencias)

**¿Procedo con la creación de los archivos de implementación? (s/n)**
```

#### Crear Carpeta y Archivos
```python
def crear_archivos_implementacion(fases):
    """
    Crea carpeta implementation/ y archivos fase1.md, fase2.md, etc.
    """
    
    # Crear carpeta implementation/
    crear_carpeta("implementation")
    
    for i, fase in enumerate(fases, 1):
        archivo_fase = f"implementation/fase{i}.md"
        
        contenido_fase = generar_contenido_fase(fase, i)
        
        escribir_archivo(archivo_fase, contenido_fase)
```

#### Template de Archivo de Fase
```markdown
# {DESCRIPCION}

## 📋 Comandos (copiar y pegar uno por uno):

```bash
# 1. {ENTIDAD1} (Entidad normal - completa)
python3 tools/forms/entity-generator.py --entity "{NOMBRE}" --plural "{PLURAL}" --module "{MODULO}" --target todo --auto-register --system-entity --icon "{ICON}" --category "{CATEGORIA}" --fields {CAMPOS} --form-fields {FORM} --grid-fields {GRID} --search-fields "{SEARCH}"

# 2. {ENTIDAD2} (Entidad normal - completa)
python3 tools/forms/entity-generator.py --entity "{NOMBRE}" --plural "{PLURAL}" --module "{MODULO}" --target todo --auto-register --system-entity --icon "{ICON}" --category "{CATEGORIA}" --fields {CAMPOS} --form-fields {FORM} --grid-fields {GRID} --search-fields "{SEARCH}"

# 3. {ENTIDAD_CON_FK} (Entidad con relaciones - completa)
python3 tools/forms/entity-generator.py --entity "{NOMBRE}" --plural "{PLURAL}" --module "{MODULO}" --target todo --auto-register --system-entity --icon "{ICON}" --category "{CATEGORIA}" --fields {CAMPOS} --fk {FK} --form-fields {FORM} --grid-fields {GRID} --lookups {LOOKUPS} --search-fields "{SEARCH}"

# 4. {TABLA_NN} (Relación N:N - SINTAXIS ELEGANTE)
python3 tools/forms/entity-generator.py --source {tabla1} --to {tabla2} --module "{MODULO}" --target db --fields {CAMPOS} --fk {FK}
```

## ℹ️ Info:
- **Ejecutar uno por uno en orden**
- **Entidades normales** (`--target todo`): crea tabla BD + backend + frontend completo
- **Tablas NN** (`--target db`): crea SOLO tabla en base de datos (sin interfaz)
- URLs generadas: `/modulo/entidad/list` y `/modulo/entidad/formulario` (solo para entidades normales)

## 📝 Notas específicas:
- Las tablas NN (Many-to-Many) son solo para almacenar relaciones
- No tienen interfaz de usuario propia
- Se gestionan a través de las entidades principales relacionadas
```

## 🔧 LÓGICA DE DETECCIÓN

### Extracción de Entidades
- Buscar patrones: "crear entidades:", "- Entidad", nombres propios
- Extraer campos mencionados: "campos: nombre string, precio int"
- Detectar relaciones: "marca: Rel: Marca", campos terminados en "_id"
- **Detectar tablas N:N**: buscar patrones como "NNTabla1_Tabla2", "tabla1 ↔ tabla2", etc.
  - **SINTAXIS NUEVA**: Usar `--source tabla1 --to tabla2` en lugar de `--entity`
  - **IMPORTANTE**: Las tablas NN solo crean la tabla de BD, NO generan interfaz ni servicios
  - Solo usar `--target db` para tablas NN
  - **Generación automática**: Se crea `nn_tabla1_tabla2` automáticamente
  - **ALIAS solo para casos especiales**: `--alias` únicamente cuando necesitas múltiples relaciones entre las mismas tablas
    - ❌ MAL: `--source venta --to producto --alias productos` (redundante)
    - ✅ BIEN: `--source producto --to categoria --alias principal` (para diferenciar de `categoria_secundaria`)

### Organización de Fases
```yaml
CRITERIO_PARA_FASES:
  UNA_SOLA_FASE:
    - Sistema relacionado (ej: ventas, inventario, RRHH)
    - Entidades del mismo dominio
    - Hasta 10-12 entidades
  
  MÚLTIPLES_FASES:
    - Sistemas muy diferentes (ej: ventas + contabilidad + RRHH)
    - Más de 12 entidades
    - Fases por módulos de negocio

EJEMPLO_UNA_FASE:
  "crear sistema de ventas": Marca, Categoria, Producto, Cliente, Venta, NNVenta_Productos
  
EJEMPLO_MÚLTIPLES_FASES:
  "crear sistema completo": 
    - Fase 1: Entidades de Ventas (6 entidades)
    - Fase 2: Entidades de RRHH (8 entidades)  
    - Fase 3: Entidades de Contabilidad (5 entidades)
```

### Asignación de Módulos (INTELIGENCIA ARTIFICIAL)

La IA debe analizar cada entidad y determinar su módulo usando estos criterios:

```python
def determinar_modulo_inteligente(nombre_entidad, campos, relaciones, contexto):
    """
    Determina el módulo correcto usando análisis inteligente
    """
    
    # 1. ANÁLISIS DEL NOMBRE DE LA ENTIDAD
    if nombre_entidad.startswith(("Tipo", "Estado", "Modalidad")):
        # Es un mantenedor - determinar submódulo por contexto
        if "Empleado" in nombre_entidad or "Ausencia" in nombre_entidad:
            return "RRHH.Configuracion.Mantenedores.Empleado"
        elif "Solicitud" in nombre_entidad:
            return "RRHH.Configuracion.Mantenedores.Solicitudes"
        elif "Contrato" in nombre_entidad or "Cargo" in nombre_entidad:
            return "RRHH.Configuracion.Mantenedores.Organizacion"
        elif "Asistencia" in nombre_entidad or "Horas" in nombre_entidad:
            return "RRHH.Configuracion.Mantenedores.Tiempo"
        else:
            return determinar_por_dominio(contexto) + ".Configuracion.Mantenedores"
    
    # 2. CONFIGURACIONES GLOBALES
    if "Configuracion" in nombre_entidad and "General" in nombre_entidad:
        return determinar_por_dominio(contexto) + ".Configuracion.Global"
    
    # 3. ENTIDADES CORE/COMPARTIDAS
    if nombre_entidad in ["Region", "Comuna", "Ciudad"]:
        return "Core.Localidades"
    elif nombre_entidad in ["Banco", "TipoCuenta", "FormaPagoBancario"]:
        return "Core.Bancario"
    elif nombre_entidad in ["CentroDeCosto", "Areas", "Empresa"]:
        return "Core.General"
    
    # 4. ANÁLISIS POR DOMINIO Y COMPLEJIDAD
    dominio = determinar_dominio(nombre_entidad, contexto)
    
    if dominio == "RRHH":
        # Análisis específico para RRHH
        if es_entidad_central(nombre_entidad, relaciones):
            return "RRHH.Empleado"  # Empleado y entidades centrales
        elif es_transaccional(campos, relaciones):
            if "Solicitud" in nombre_entidad:
                return "RRHH.Solicitudes"
            elif "Asistencia" in nombre_entidad or "Horas" in nombre_entidad:
                return "RRHH.AsistenciayTiempo"
            else:
                return "RRHH.Empleado"  # Otras transaccionales van con empleado
        elif es_previsional(nombre_entidad):
            return "RRHH.Previsional"
        else:
            return "RRHH.Configuracion.Mantenedores"
    
    # 5. OTROS DOMINIOS
    elif dominio == "Ventas":
        if es_mantenedor(campos, relaciones):
            return "Ventas.Configuracion"
        else:
            return "Ventas"
    
    elif dominio == "Catalogo":
        return "Catalogo"
    
    # 6. DEFAULT
    return "General"

def determinar_dominio(nombre_entidad, contexto):
    """Detecta el dominio principal"""
    # Buscar en contexto palabras clave
    contexto_lower = contexto.lower()
    
    if any(palabra in contexto_lower for palabra in ["rrhh", "empleado", "personal", "recursos humanos"]):
        return "RRHH"
    elif any(palabra in contexto_lower for palabra in ["venta", "cliente", "factura"]):
        return "Ventas"
    elif any(palabra in contexto_lower for palabra in ["producto", "categoria", "marca"]):
        return "Catalogo"
    
    # Analizar por nombre de entidad
    nombre_lower = nombre_entidad.lower()
    if "empleado" in nombre_lower or "rrhh" in nombre_lower:
        return "RRHH"
    elif "venta" in nombre_lower or "cliente" in nombre_lower:
        return "Ventas"
    elif "producto" in nombre_lower or "categoria" in nombre_lower:
        return "Catalogo"
    
    return "General"

def es_entidad_central(nombre, relaciones):
    """Una entidad central tiene muchas relaciones O es la entidad principal del dominio"""
    return len(relaciones) >= 10 or nombre in ["Empleado", "Cliente", "Producto"]

def es_transaccional(campos, relaciones):
    """Entidades transaccionales tienen fechas y FK a entidades centrales"""
    tiene_fechas = any("fecha" in campo.lower() for campo in campos)
    tiene_fk_central = len(relaciones) >= 2
    return tiene_fechas and tiene_fk_central

def es_mantenedor(campos, relaciones):
    """Mantenedores típicamente tienen pocos campos básicos y pocas/ninguna relación"""
    return len(relaciones) <= 1 and len(campos) <= 4

def es_previsional(nombre):
    """Detectar entidades previsionales"""
    return any(palabra in nombre.lower() for palabra in ["afp", "salud", "seguro", "previsional"])
```

**EJEMPLOS DE APLICACIÓN:**
- **TipoBonificacion** → Tipo + Empleado en contexto → `RRHH.Configuracion.Mantenedores.Empleado`
- **ConfiguracionGeneralRRHH** → Configuracion + General → `RRHH.Configuracion.Global`
- **TipoSolicitudEmpleado** → Tipo + Solicitud → `RRHH.Configuracion.Mantenedores.Solicitudes`
- **Empleado** → Entidad central + muchas FK → `RRHH.Empleado`
- **Region** → Entidad Core → `Core.Localidades`

### Generación de Comandos
- Usar parámetros de `docs/auto-generacion-with-python-script.md`
- **Entidades normales**: usar `--target todo` (completo con interfaz)
- **Tablas NN/N:N**: usar `--target db` (solo tabla, sin interfaz)
- **IMPORTANTE**: Comandos en UNA SOLA LÍNEA (no usar `\` multilínea)
- Incluir `--fields`, `--fk`, `--form-fields`, `--grid-fields`, etc.
- **Orden secuencial** - las dependencias se resuelven automáticamente si se ejecuta en orden

## 🔧 EJEMPLOS DE RESPUESTA

### **EJEMPLO 1**: Sistema con tabla NN

**ENTRADA**: "crear entidades: Marca, Categoria, Producto, Venta, NNVenta_Productos"

**SALIDA ESPERADA**:
```
Perfecto, voy a crear un sistema de ventas con 5 entidades:

📋 **ENTIDADES A CREAR:**
• **Marca** - campos: nombre, codigointerno (Módulo: Catalogo) 🏷️ inventory
• **Categoria** - campos: nombre, codigointerno (Módulo: Catalogo) 📂 category
• **Producto** - campos: nombre, codigosku, precioventa, preciocompra + relaciones → Marca, Categoria (Módulo: Catalogo) 📦 shopping_bag
• **Venta** - campos: numventa (autoincremental), montototal (Módulo: Ventas) 💰 point_of_sale
• **NNVenta_Productos** - tabla N:N con cantidad, precioneto, descuentopeso, etc. (Módulo: Ventas)
  ⚠️ *Solo tabla BD, sin interfaz*

🔧 **QUÉ VOY A GENERAR:**
- 5 tablas en la base de datos
- 4 controladores y servicios en Backend/ (Marca, Categoria, Producto, Venta)
- 12 archivos de interfaz en Frontend/ (4 entidades × 3 componentes c/u)
- 24 permisos del sistema automáticos (6 por cada entidad normal)
- 4 registros automáticos en system_form_entities para FormDesigner

⚡ **ORDEN DE CREACIÓN:**
**Fase 1 - Sistema de Ventas**: Marca, Categoria, Producto, Venta, NNVenta_Productos
(Se ejecutan en orden secuencial para resolver dependencias automáticamente)

📁 **ARCHIVOS DE IMPLEMENTACIÓN:**
Voy a crear la carpeta `implementation/` con los comandos organizados por fases.

¿Procedo con la creación de los archivos de implementación? (s/n)
```

### **EJEMPLO 2**: Sistema simple sin NN

**ENTRADA**: "crear entidades: Cliente, Factura con campos numero, fecha, total"

**SALIDA ESPERADA**:
```
Perfecto, voy a crear un sistema de facturación con 2 entidades:

📋 **ENTIDADES A CREAR:**
• **Cliente** - campos: nombre, telefono, email (Módulo: Ventas)
• **Factura** - campos: numero, fecha, total + relación → Cliente (Módulo: Ventas)

🔧 **QUÉ VOY A GENERAR:**
- 2 tablas en la base de datos
- 2 controladores y servicios en Backend/
- 6 archivos de interfaz en Frontend/
- 12 permisos del sistema automáticos
- 2 registros automáticos en system_form_entities para FormDesigner

⚡ **ORDEN DE CREACIÓN:**
**Fase 1 - Sistema Facturación**: Cliente, Factura
(Cliente primero, luego Factura por dependencia)

¿Procedo con la creación de los archivos de implementación? (s/n)
```

### **EJEMPLO 3**: Tu solicitud específica corregida

**ENTRADA**: Tu comando actual problemático
```bash
python3 tools/forms/entity-generator.py --entity "NNVenta_Productos" --plural "NNVenta_Productos" --module "Ventas" --target todo --fields "cantidad:int" "precioneto:int" "descuentopeso:int" "descuentoporcentaje:decimal:5,2" "montototal:int" --fk "venta_id:venta" "producto_id:producto" --form-fields "cantidad:required:min=1:placeholder=Cantidad" --lookups "venta_id:venta:Numventa:required:fast:form,grid" --grid-fields "venta_id->Venta.Numventa:120px:left:f"
```

**COMANDO CORREGIDO**:
```bash
# CORRECTO ✅ - Solo tabla NN, sin interfaz
python3 tools/forms/entity-generator.py --entity "NNVenta_Productos" --plural "NNVenta_Productos" --module "Ventas" --target db --fields "cantidad:int" "precioneto:int" "descuentopeso:int" "descuentoporcentaje:decimal:5,2" "montototal:int" --fk "venta_id:venta" "producto_id:producto"
```

**Lo que cambió**:
- ❌ `--target todo` → ✅ `--target db`
- ❌ Eliminé `--form-fields`
- ❌ Eliminé `--lookups` 
- ❌ Eliminé `--grid-fields`

**Resultado**: Solo se crea la tabla en BD para almacenar la relación Many-to-Many entre Venta y Productos, sin generar interfaz de usuario.

## 📋 REGLAS ESPECÍFICAS PARA TABLAS N:N

### ¿Cuándo es una tabla N:N?
- Detectar patrones: "NNVenta_Productos", "venta ↔ productos", "tabla1 con tabla2"
- Representan relaciones Many-to-Many entre dos entidades
- **NUEVA SINTAXIS ELEGANTE**: `--source tabla1 --to tabla2` genera automáticamente `nn_tabla1_tabla2`

### Comando para tablas N:N - NUEVA SINTAXIS
```bash
# SINTAXIS ELEGANTE ✅ (recomendada):
python3 tools/forms/entity-generator.py --source venta --to producto --module "Ventas" --target db --fields "cantidad:int" "precio:decimal:10,2" "descuento:decimal:5,2" --fk "venta_id:venta" "producto_id:producto"

# Con alias SOLO para casos especiales ✅:
python3 tools/forms/entity-generator.py --source producto --to categoria --alias principal --module "Catalogo" --target db --fields "orden:int" "activo:bool" --fk "producto_id:producto" "categoria_id:categoria"

# FORMATO ANTIGUO ❌ (aún soportado pero no recomendado):
# python3 tools/forms/entity-generator.py --entity "nn_venta_productos" --target db ...
```

**Resultado automático:**
- **Tabla BD:** `nn_venta_producto` o `nn_producto_categoria_principal` (con alias)
- **Modelo:** `Shared.Models/Entities/NN/NnVentaProducto.cs` o `NnProductoCategoriaPrincipal.cs`
- **Namespace:** `Shared.Models.Entities.NN`
- **Permisos:** `VENTA.ADDTARGET`, `VENTA.DELETETARGET`, `VENTA.EDITTARGET` (basados en source table)

### 📋 Cuándo usar --alias:
- ✅ **SÍ usar**: Múltiples relaciones entre las mismas tablas
  - `producto ↔ categoria (principal)` y `producto ↔ categoria (secundaria)`
  - `usuario ↔ proyecto (owner)` y `usuario ↔ proyecto (colaborador)`
- ❌ **NO usar**: Para hacer plural o "mejorar" el nombre
  - `--source venta --to producto` → `nn_venta_producto` (perfecto así)

### Lo que NO hacer con tablas NN:
- ❌ NO usar `--entity` (usar `--source --to` en su lugar)
- ❌ NO usar `--target todo` o `--target interfaz`
- ❌ NO usar `--lookups`, `--form-fields`, `--grid-fields`, `--search-fields`
- ❌ NO usar formato antiguo `--entity "nn_tabla1_tabla2"`

### Lo que SÍ hacer con tablas NN:
- ✅ **NUEVA SINTAXIS**: `--source tabla1 --to tabla2`
- ✅ Usar `--target db` únicamente
- ✅ Usar `--fields` para campos propios de la relación
- ✅ Usar `--fk` para las dos claves foráneas
- ✅ Usar `--alias nombre` para casos especiales (opcional)
- ✅ Colocar al final del orden de creación
- ✅ **Generación automática**: tabla `nn_source_to[_alias]` y modelo en `NN/`

## 🧠 APLICAR INTELIGENCIA ARTIFICIAL

### INSTRUCCIONES PARA LA IA:
1. **ANALIZAR CADA ENTIDAD INDIVIDUALMENTE**: 
   - Examinar nombre, campos mencionados, relaciones detectadas
   - Aplicar la lógica de `determinar_modulo_inteligente()`
   - No usar mapeos hardcodeados

2. **DETECTAR PATRONES INTELIGENTEMENTE**:
   - Prefijos: "Tipo", "Estado", "Modalidad" → Mantenedores
   - Contenido: "Configuracion" + "General" → Global  
   - Contexto: palabras clave en la solicitud del usuario
   - Complejidad: cantidad de campos y relaciones

3. **AGRUPAR COHERENTEMENTE**:
   - Entidades similares van al mismo submódulo
   - Mantener jerarquía lógica (Configuracion.Mantenedores.Empleado)
   - Separar transaccionales de mantenedores

4. **EJEMPLOS DE RAZONAMIENTO**:
```
"TipoBonificacion" → 
  ✓ Empieza con "Tipo" (es mantenedor)
  ✓ Contexto menciona "empleado" 
  → RESULTADO: "RRHH.Configuracion.Mantenedores.Empleado"

"ConfiguracionGeneralRRHH" →
  ✓ Contiene "Configuracion" + "General"
  ✓ Contexto es RRHH
  → RESULTADO: "RRHH.Configuracion.Global"

"Empleado" →
  ✓ Entidad central con muchas FK (15+)
  ✓ Es la entidad principal del dominio RRHH
  → RESULTADO: "RRHH.Empleado"
```

## 🚨 REGLAS CRÍTICAS

### 1. LEER DOCUMENTACIÓN PRIMERO
- **OBLIGATORIO**: Leer `docs/auto-generacion-with-python-script.md` antes de procesar
- Usar solo parámetros documentados de entity-generator.py
- Basarse en ejemplos reales de la documentación

### 2. SIEMPRE EN ESPAÑOL
- Todas las respuestas en español
- Preguntar confirmación en español: "¿Procedo...? (s/n)"
- Nombres técnicos en inglés (Controller, Service)

### 3. CREAR ARCHIVOS, NO EJECUTAR
- **NO ejecutar comandos** - solo crear archivos .md
- Crear carpeta `implementation/`
- Crear archivos `fase1.md`, `fase2.md`, etc.
- **Formato simple**: Comandos arriba para copiar/pegar, explicación mínima abajo
- **Una sola línea por comando** (no multilínea con `\`)
- El usuario ejecuta manualmente los comandos

### 4. SER ESPECÍFICO Y CLARO
- Decir exactamente qué archivos se crearán
- Mostrar orden de fases con justificación
- Contar archivos específicos (8 controladores, 24 interfaces, etc.)
- Explicar relaciones detectadas

### 5. CONVERSACIONAL PERO TÉCNICO
- Usar lenguaje natural y amigable
- Ser directo sobre qué se va a hacer
- Estructurar información claramente
- Usar emojis para legibilidad

### 6. PEDIR CONFIRMACIÓN
- SIEMPRE terminar con "¿Procedo con la creación de los archivos de implementación? (s/n)"
- No crear archivos sin confirmación explícita
- Si respuesta no es clara, preguntar nuevamente

## 🎯 RESULTADO FINAL

Después de confirmación, el sistema:
1. Crea carpeta `implementation/`
2. Genera archivos `fase1.md`, `fase2.md`, etc. con comandos listos
3. El usuario ejecuta manualmente cada fase
4. Los comandos están basados en la documentación real de entity-generator.py