# Sistema de Análisis y Generación de Prompts por Etapas

## Variables
- **SOLICITUD_USUARIO**: $ARGUMENTS (descripción en lenguaje natural)
- **RAIZ_PROYECTO**: Directorio actual de trabajo
- **ANALISIS_DIR**: ./analisis/ (directorio de análisis generados)

## 🎯 Propósito
Sistema inteligente que analiza solicitudes complejas del usuario y las divide en etapas manejables, generando prompts específicos para cada fase que pueden ser ejecutados secuencialmente con @ai-plan.

## 🚨 REGLAS CRÍTICAS

### 1. Gestión de Archivos de Análisis
```yaml
ESTRUCTURA_OBLIGATORIA:
  analisis/
  ├── {timestamp}_{nombre_solicitud}/
  │   ├── analisis.md              # Análisis completo de la solicitud
  │   ├── etapa1_prompt.md         # Prompt para etapa 1
  │   ├── etapa2_prompt.md         # Prompt para etapa 2  
  │   ├── etapa3_prompt.md         # Prompt para etapa N
  │   ├── secuencia.md             # Orden de ejecución
  │   └── validaciones.md          # Checkpoints entre etapas

NAMING_CONVENTION:
  timestamp: YYYYMMDD_HHMMSS
  nombre_solicitud: snake_case_descriptivo
```

### 2. Principios de División por Etapas
```yaml
CRITERIOS_DIVISION:
  ETAPA_1_BASE: [entidades_independientes, configuraciones_basicas]
  ETAPA_2_RELACIONES: [entidades_con_fk, lookups_simples] 
  ETAPA_3_TRANSACCIONAL: [procesos_negocio, validaciones_complejas]
  ETAPA_4_INTEGRACION: [reportes, exportacion, apis_externas]
  ETAPA_5_OPTIMIZACION: [performance, ui_avanzada, dashboards]

MAXIMAS_POR_ETAPA:
  - Max 4 entidades por etapa
  - Max 2 módulos nuevos por etapa
  - Max 1 funcionalidad compleja por etapa
  - Cada etapa debe ser completamente funcional
```

### 3. Detección de Complejidad
```yaml
INDICADORES_COMPLEJIDAD_ALTA:
  palabras_clave: [sistema, plataforma, completo, integral, avanzado]
  cantidad_entidades: >5
  multiples_modulos: >2
  integraciones: [api, reportes, notificaciones, workflows]
  
ESTRATEGIAS_SIMPLIFICACION:
  horizontal: dividir por módulos funcionales
  vertical: dividir por capas (datos → lógica → ui)
  temporal: dividir por fases de desarrollo (mvp → features → optimización)
```

## 🔍 FASE 0: ANÁLISIS DE COMPLEJIDAD

### Detector de Complejidad
```python
def analizar_complejidad_solicitud(solicitud_usuario):
    """
    Analiza la complejidad y determina estrategia de división
    """
    
    indicadores = {
        "entidades_detectadas": extraer_entidades(solicitud_usuario),
        "modulos_implicados": detectar_modulos(solicitud_usuario),
        "integraciones": detectar_integraciones(solicitud_usuario),
        "procesos_complejos": detectar_procesos(solicitud_usuario)
    }
    
    nivel_complejidad = calcular_nivel_complejidad(indicadores)
    
    if nivel_complejidad == "BAJA":
        return {"etapas": 1, "estrategia": "DIRECTA"}
    elif nivel_complejidad == "MEDIA":
        return {"etapas": 2-3, "estrategia": "POR_DEPENDENCIAS"}
    else:  # ALTA
        return {"etapas": 3-5, "estrategia": "POR_FUNCIONALIDAD"}

def extraer_entidades(solicitud):
    """Detecta entidades mencionadas explícita o implícitamente"""
    
    # Patrones explícitos
    entidades_explicitas = re.findall(r'\b([A-Z][a-z]+(?:[A-Z][a-z]+)*)\b', solicitud)
    
    # Patrones implícitos por dominio
    patrones_negocio = {
        "ventas": ["cliente", "producto", "categoria", "venta", "factura"],
        "inventario": ["producto", "categoria", "almacen", "movimiento", "proveedor"],
        "rrhh": ["empleado", "cargo", "departamento", "contrato", "nomina"],
        "crm": ["cliente", "contacto", "empresa", "oportunidad", "seguimiento"],
        "escolar": ["estudiante", "curso", "profesor", "materia", "calificacion"],
        "medico": ["paciente", "doctor", "cita", "diagnostico", "tratamiento"],
        "logistico": ["vehiculo", "ruta", "conductor", "envio", "tracking"]
    }
    
    entidades_implicitas = []
    for dominio, entidades in patrones_negocio.items():
        if dominio in solicitud.lower():
            entidades_implicitas.extend(entidades)
    
    return list(set(entidades_explicitas + entidades_implicitas))

def detectar_integraciones(solicitud):
    """Detecta integraciones y funcionalidades complejas"""
    
    integraciones = []
    
    patrones_integracion = {
        "reportes": ["reporte", "excel", "pdf", "dashboard", "grafico"],
        "notificaciones": ["email", "sms", "notificacion", "alerta"],
        "apis": ["api", "servicio", "integracion", "webhook"],
        "workflow": ["aprobacion", "flujo", "workflow", "proceso"],
        "facturacion": ["factura", "boleta", "tributario", "sunat"],
        "pagos": ["pago", "tarjeta", "banco", "transferencia"],
        "auditoria": ["log", "auditoria", "seguimiento", "trazabilidad"]
    }
    
    for categoria, palabras in patrones_integracion.items():
        if any(palabra in solicitud.lower() for palabra in palabras):
            integraciones.append(categoria)
    
    return integraciones
```

## 🏗️ FASE 1: GENERACIÓN DE ETAPAS

### Estrategias de División
```python
def generar_etapas_por_estrategia(entidades, integraciones, estrategia):
    """
    Genera etapas según la estrategia detectada
    """
    
    if estrategia == "POR_DEPENDENCIAS":
        return dividir_por_dependencias(entidades)
    elif estrategia == "POR_FUNCIONALIDAD":  
        return dividir_por_funcionalidad(entidades, integraciones)
    else:  # DIRECTA
        return [{"etapa": 1, "entidades": entidades, "descripcion": "Implementación completa"}]

def dividir_por_dependencias(entidades):
    """División basada en dependencias entre entidades"""
    
    # Construir grafo de dependencias
    grafo = construir_grafo_dependencias(entidades)
    niveles = ordenar_por_niveles(grafo)
    
    etapas = []
    for i, nivel in enumerate(niveles):
        etapas.append({
            "etapa": i + 1,
            "entidades": nivel,
            "descripcion": f"Entidades de nivel {i + 1}",
            "justificacion": "Basado en dependencias de foreign keys"
        })
    
    return etapas

def dividir_por_funcionalidad(entidades, integraciones):
    """División por funcionalidades de negocio"""
    
    etapas = [
        {
            "etapa": 1,
            "titulo": "Base de Datos y Entidades Core",
            "entidades": filtrar_entidades_core(entidades),
            "descripcion": "Crear entidades base y estructura fundamental",
            "incluye": ["tablas", "permisos", "crud_basico"]
        },
        {
            "etapa": 2, 
            "titulo": "Lógica de Negocio y Relaciones",
            "entidades": filtrar_entidades_relacionales(entidades),
            "descripcion": "Implementar relaciones y validaciones complejas",
            "incluye": ["foreign_keys", "validaciones", "lookups"]
        },
        {
            "etapa": 3,
            "titulo": "Procesos Transaccionales", 
            "entidades": filtrar_entidades_transaccionales(entidades),
            "descripcion": "Crear procesos de negocio y workflows",
            "incluye": ["transacciones", "workflows", "calculos"]
        }
    ]
    
    # Agregar etapas de integración si se detectaron
    if integraciones:
        etapas.append({
            "etapa": 4,
            "titulo": "Integraciones y Reportes",
            "entidades": [],
            "descripcion": "Implementar integraciones externas y reportería",
            "incluye": integraciones
        })
    
    return etapas
```

### Generador de Prompts por Etapa
```python
def generar_prompt_para_etapa(etapa_info, contexto_solicitud):
    """
    Genera prompt específico y detallado para una etapa
    """
    
    prompt_base = f"""
# ETAPA {etapa_info['etapa']}: {etapa_info['titulo']}

## Contexto Original
{contexto_solicitud['solicitud_original']}

## Objetivo de Esta Etapa
{etapa_info['descripcion']}

## Entidades a Implementar en Esta Etapa
{generar_lista_entidades_detallada(etapa_info['entidades'])}

## Restricciones Específicas
- SOLO implementar las entidades listadas arriba
- NO crear dependencias hacia entidades de etapas futuras
- Usar campos string temporales para relaciones futuras si es necesario
- Validar compilación exitosa antes de continuar

## Validaciones Post-Implementación
{generar_checklist_validacion(etapa_info)}

## Preparación para Siguiente Etapa
{generar_notas_siguiente_etapa(etapa_info)}
"""
    
    return prompt_base

def generar_lista_entidades_detallada(entidades):
    """Genera descripción detallada de entidades para la etapa"""
    
    descripcion = ""
    for entidad in entidades:
        descripcion += f"""
### {entidad['nombre']}
- **Módulo**: {entidad['modulo']}
- **Campos sugeridos**: {', '.join(entidad['campos'])}
- **Relaciones**: {entidad['relaciones'] if entidad['relaciones'] else 'Ninguna en esta etapa'}
- **Justificación**: {entidad['justificacion']}
"""
    
    return descripcion
```

## 📋 TEMPLATES DE PROMPTS GENERADOS

### Template para Etapa Base
```markdown
# ETAPA 1: Entidades Base y Configuración

## Objetivo
Crear la estructura fundamental del sistema con entidades independientes que no tienen dependencias externas.

## Implementar:
- Categoria (Catálogo)
- Cliente (Ventas) 
- TipoDocumento (Admin.Config)

## Prompt para @ai-plan:
```
crear las siguientes entidades base:

1. Categoria en módulo Catalogo.Core con campos: nombre (string 100), descripcion (text), activo (bool)
2. Cliente en módulo Ventas.Core con campos: razon_social (string 200), ruc (string 20), telefono (string 15), email (string 100) 
3. TipoDocumento en módulo Admin.Config con campos: nombre (string 50), codigo (string 10), descripcion (text)

Configurar formularios con validaciones básicas y grillas de listado estándar.
```

## Validaciones Post-Etapa:
- [ ] 3 tablas creadas correctamente
- [ ] Compilación exitosa: dotnet build
- [ ] Interfaces CRUD funcionando
- [ ] 18 permisos creados (6 por entidad)

## Notas para Etapa 2:
La siguiente etapa podrá referenciar estas entidades mediante foreign keys.
```

### Template para Etapa Relacional
```markdown
# ETAPA 2: Entidades con Relaciones

## Prerequisitos:
✅ Etapa 1 completada exitosamente
✅ Entidades base: Categoria, Cliente disponibles

## Implementar:
- Producto (→ Categoria)
- Proveedor (Catálogo)
- ContactoCliente (→ Cliente)

## Prompt para @ai-plan:
```
crear las siguientes entidades con relaciones:

1. Producto en módulo Catalogo.Core con campos: nombre (string 255), codigo (string 50), precio (decimal 18,2), stock_minimo (int) y relación con categoria_id mediante lookup
2. Proveedor en módulo Catalogo.Core con campos: razon_social (string 200), ruc (string 20), telefono (string 15), email (string 100)
3. ContactoCliente en módulo Ventas.Core con campos: nombre (string 100), telefono (string 15), email (string 100), cargo (string 50) y relación con cliente_id

Configurar lookups funcionales en formularios y grillas con nombres descriptivos.
```

## Validaciones Post-Etapa:
- [ ] Foreign keys funcionando correctamente  
- [ ] Lookups mostrando nombres en lugar de IDs
- [ ] Validación de integridad referencial
- [ ] Compilación y funcionamiento correcto
```

## 🔄 FLUJO DE EJECUCIÓN COMPLETO

### Comando del Usuario
```bash
@ai-analisis "crear un sistema completo de gestión escolar con estudiantes, profesores, cursos, materias, calificaciones, horarios, reportes académicos y dashboard administrativo"
```

### Procesamiento Automático
```markdown
🔍 ANÁLISIS DE COMPLEJIDAD (15 segundos):
├── 📊 Entidades detectadas: 8 (estudiante, profesor, curso, materia, calificacion, horario, reporte, dashboard)
├── 🏗️ Módulos implicados: 3 (Academico, Admin, Reportes)
├── 🔧 Integraciones: reportes, dashboard
├── 📈 Nivel complejidad: ALTA
├── 🎯 Estrategia: POR_FUNCIONALIDAD  
└── 📋 Etapas sugeridas: 4

🏗️ GENERACIÓN DE ETAPAS (30 segundos):
├── 📁 Crear: analisis/20240306_143530_sistema_escolar/
├── 📝 Generar análisis completo
├── 🎯 Crear 4 prompts específicos
├── 📋 Definir secuencia de ejecución
└── ✅ Validaciones entre etapas

📊 RESULTADO:
• 4 etapas planificadas
• Prompts específicos listos
• Secuencia de ejecución definida
• Checkpoints de validación
• Estimación: 2-3 días de desarrollo
```

### Archivos Generados
```
analisis/20240306_143530_sistema_escolar/
├── analisis.md              # "Detecté 8 entidades, 3 módulos, complejidad ALTA"
├── etapa1_prompt.md         # "crear estudiante, profesor, materia (entidades base)"  
├── etapa2_prompt.md         # "crear curso, horario con relaciones a etapa 1"
├── etapa3_prompt.md         # "crear calificacion, sistema de notas"
├── etapa4_prompt.md         # "crear reportes académicos y dashboard"
├── secuencia.md             # Orden exacto de ejecución
└── validaciones.md          # Checkpoints entre cada etapa
```

## 🎯 EJEMPLOS DE USO

### Solicitud Simple → 1 Etapa
**Input:** `"crear producto y categoria"`
**Output:** 1 prompt directo para @ai-plan

### Solicitud Media → 2-3 Etapas  
**Input:** `"sistema de ventas básico"`
**Output:** Etapa 1: entidades base, Etapa 2: relaciones, Etapa 3: procesos

### Solicitud Compleja → 4-5 Etapas
**Input:** `"plataforma e-commerce completa"`
**Output:** Etapa 1: catálogo, Etapa 2: usuarios, Etapa 3: ventas, Etapa 4: pagos, Etapa 5: reportes

## 🔧 CARACTERÍSTICAS INTELIGENTES

### Análisis Contextual
- **Detecta dominios**: "escolar" → infiere entidades académicas
- **Reconoce patrones**: "completo/integral" → divide en más etapas
- **Identifica dependencias**: orden automático de implementación

### Generación de Prompts Inteligente
- **Específicos por etapa**: solo entidades correspondientes
- **Restricciones claras**: evita dependencias futuras  
- **Validaciones incluidas**: checkpoints automáticos
- **Preparación siguiente etapa**: notas para continuidad

### Control de Complejidad
- **Máximo 4 entidades por etapa**
- **Etapas completamente funcionales**
- **Validación entre etapas**
- **Rollback si falla una etapa**

El sistema convierte solicitudes complejas ambiciosas en planes de implementación estructurados y manejables, eliminando la parálisis por análisis y garantizando progreso constante.