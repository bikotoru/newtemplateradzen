import asyncio
import sys
import os
import re
import json
from pathlib import Path
from claude_code_sdk import ClaudeSDKClient, ClaudeCodeOptions

# Configurar encoding para Windows
if sys.platform == "win32":
    import codecs
    sys.stdout = codecs.getwriter("utf-8")(sys.stdout.detach())
    sys.stderr = codecs.getwriter("utf-8")(sys.stderr.detach())

# Importar funciones del generate_menu.py original
from generate_menu import (
    parse_input, get_config_path, get_config_filename, 
    generate_config_content, generate_menu_item, 
    get_icon_for_entity, check_module_in_unificado
)

async def auto_confirm(client):
    """Auto-confirma permisos"""
    async for message in client.receive_response():
        if hasattr(message, 'content'):
            for block in message.content:
                if hasattr(block, 'text'):
                    text = block.text
                    if any(word in text.lower() for word in ["permission", "permisos", "proceed", "continuar"]):
                        await client.query("Sí, procede.")

def parse_json_file(json_path):
    """
    Lee el archivo entities-urls.json desde la ruta especificada
    y convierte las entidades al formato requerido
    """
    entities = []
    
    try:
        with open(json_path, 'r', encoding='utf-8') as f:
            json_data = json.load(f)
        
        for item in json_data:
            entidad = item['entidad']
            modulo_json = item['modulo']
            
            # Convertir módulo jerárquico a estructura de paths
            # Ej: "RRHH.Configuracion.Mantenedores.Empleado" -> "rrhh/configuracion/mantenedores/empleado"
            modulo_parts = modulo_json.lower().split('.')
            modulo = modulo_parts[0]  # rrhh, core, ventas, etc.
            
            # Manejar submódulos jerárquicos
            if len(modulo_parts) > 1:
                # Unir todos los submódulos con "/"
                submodulo_path = "/".join(modulo_parts[1:])
                input_line = f"{modulo}/{submodulo_path}/{entidad.lower()}"
            else:
                input_line = f"{modulo}/{entidad.lower()}"
            
            # Para compatibilidad con el sistema actual, usar solo el primer submódulo
            submodulo = modulo_parts[1] if len(modulo_parts) > 1 else None
            
            entities.append({
                'line': len(entities) + 1,
                'input': input_line,
                'modulo': modulo,
                'submodulo': submodulo,
                'entidad': entidad.lower(),
                'entidad_original': entidad,
                'permiso': f"{entidad.upper()}.VIEWMENU"
            })
            
    except FileNotFoundError:
        print(f"❌ Error: No se encontró el archivo {json_path}")
        return []
    except json.JSONDecodeError as e:
        print(f"❌ Error al parsear JSON: {e}")
        return []
    except Exception as e:
        print(f"❌ Error inesperado: {e}")
        return []
    
    return entities

def group_entities_by_module(entities):
    """Agrupa entidades por módulo para procesar eficientemente"""
    modules = {}
    
    for entity in entities:
        modulo = entity['modulo']
        if modulo not in modules:
            modules[modulo] = {
                'config_path': get_config_path(modulo),
                'entities': [],
                'needs_creation': False,
                'needs_registration': False
            }
        modules[modulo]['entities'].append(entity)
    
    return modules

async def process_module_batch(modulo, module_data):
    """Procesa todas las entidades de un módulo de una vez"""
    config_path = module_data['config_path']
    entities = module_data['entities']
    
    print(f"\n📁 Procesando módulo: {modulo}")
    print(f"   Entidades: {len(entities)}")
    
    # 1. Crear archivo de configuración si no existe
    if not config_path.exists():
        print(f"   📝 Creando {config_path.name}...")
        config_path.parent.mkdir(parents=True, exist_ok=True)
        
        with open(config_path, 'w', encoding='utf-8') as f:
            f.write(generate_config_content(modulo))
        
        module_data['needs_creation'] = True
        module_data['needs_registration'] = True
        print(f"   ✅ {config_path.name} creado")
    
    # 2. Preparar datos para la IA (sin pre-generar MenuItems)
    entity_data = []
    for entity in entities:
        entity_data.append({
            'entity': entity
        })
    
    # 3. Agregar todas las entidades usando IA inteligente
    if entity_data:
        await add_multiple_menu_items(config_path, entity_data)
    
    return module_data

async def add_multiple_menu_items(config_path, menu_items):
    """Agrega múltiples MenuItems usando IA inteligente con contexto completo"""
    
    # Preparar contexto completo para la IA
    entities_context = []
    for item_data in menu_items:
        entity = item_data['entity']
        entities_context.append({
            'entidad': entity['entidad'],
            'submodulo': entity['submodulo'],
            'input_original': entity['input'],
            'permiso': entity.get('permiso', f"{entity['entidad'].upper()}.VIEWMENU"),
            'entidad_original': entity.get('entidad_original', entity['entidad'])
        })
    
    # Cargar contexto de MenuIA
    context_path = Path(__file__).parent / "context.md"
    context = ""
    if context_path.exists():
        with open(context_path, 'r', encoding='utf-8') as f:
            context = f.read()
    
    async with ClaudeSDKClient(
        options=ClaudeCodeOptions(
            max_turns=5,
            allowed_tools=["Read", "Edit", "MultiEdit"]
        )
    ) as client:
        
        # Agrupar entidades por submódulo para jerarquía
        submodule_groups = {}
        for ctx in entities_context:
            entity = ctx['entidad']
            submodule = ctx.get('submodulo', '')
            if submodule:
                if submodule not in submodule_groups:
                    submodule_groups[submodule] = []
                submodule_groups[submodule].append(ctx)
            else:
                # Entidades sin submódulo van directo
                if 'direct' not in submodule_groups:
                    submodule_groups['direct'] = []
                submodule_groups['direct'].append(ctx)
        
        # Crear contexto jerárquico
        hierarchical_context = []
        for submodule, entities in submodule_groups.items():
            if submodule == 'direct':
                hierarchical_context.append(f"\n=== ENTIDADES DIRECTAS ===")
                for ctx in entities:
                    hierarchical_context.append(f"- {ctx['entidad']} → Permiso: {ctx['permiso']} → Path: {ctx['input_original']}")
            else:
                submodule_name = submodule.replace('_', ' ').title()
                hierarchical_context.append(f"\n=== SUBMÓDULO: {submodule_name.upper()} ===")
                for ctx in entities:
                    hierarchical_context.append(f"- {ctx['entidad']} → Permiso: {ctx['permiso']} → Path: {ctx['input_original']}")
        
        entities_list = "\n".join(hierarchical_context)
        
        prompt = f"""
{context}

TAREA CRÍTICA: Agregar múltiples entidades al archivo de menú reemplazando el comentario vacío.

Archivo a modificar: {config_path}

Entidades a agregar:
{entities_list}

INSTRUCCIONES CRÍTICAS - GENERAR ESTRUCTURA CON SUBMENÚS:
1. Lee el archivo actual - verás que tiene un comentario: "// Los elementos del menú se agregarán aquí"
2. DEBES REEMPLAZAR ese comentario con los MenuItems organizados jerárquicamente
3. ESTRUCTURA JERÁRQUICA requerida:
   - Para módulos como RRHH: crear MenuItems principales por cada submódulo
   - Cada MenuItem principal tendrá SubItems con las entidades específicas
   - Para módulos simples como Core: crear MenuItems directos

4. FORMATO JERÁRQUICO requerido:
// Para submódulos (ej: RRHH.AsistenciayTiempo)
new MenuItem
{{
    Text = "Asistencia y Tiempo",
    Icon = "schedule",
    Path = "", // Vacío para items padre
    Permissions = new List<string>(),
    SubItems = new List<MenuItem>
    {{
        new MenuItem {{ Text = "Estado Horas Extras", Icon = "timer", Path = "/rrhh/asistenciaytiempo/estadohorasextras/list", Permissions = new List<string> {{ "ESTADOHORASEXTRAS.VIEWMENU" }} }},
        new MenuItem {{ Text = "Tipo Registro Asistencia", Icon = "access_time", Path = "/rrhh/asistenciaytiempo/tiporegistroasistencia/list", Permissions = new List<string> {{ "TIPOREGISTROASISTENCIA.VIEWMENU" }} }}
    }}
}},

5. REGLAS IMPORTANTES:
   - Submódulos se convierten en MenuItems padre con SubItems
   - Items padre tienen Path vacío y Permissions vacío
   - SubItems tienen Path y Permissions reales
   - Ordenar alfabéticamente por Text en cada nivel
   - Usar iconos coherentes por categoría

EJEMPLO COMPLETO para RRHH:
MenuItems = new List<MenuItem>
{{
    new MenuItem
    {{
        Text = "Asistencia y Tiempo",
        Icon = "schedule", 
        Path = "",
        Permissions = new List<string>(),
        SubItems = new List<MenuItem>
        {{
            new MenuItem {{ Text = "Estado Horas Extras", Icon = "timer", Path = "/rrhh/asistenciaytiempo/estadohorasextras/list", Permissions = new List<string> {{ "ESTADOHORASEXTRAS.VIEWMENU" }} }}
        }}
    }}
}}

¡CRÍTICO: Generar estructura jerárquica real con SubItems!
"""
        
        await client.query(prompt)
        await auto_confirm(client)

async def register_modules_in_unificado(modules_to_register):
    """Registra múltiples módulos nuevos en MenuUnificado.razor"""
    if not modules_to_register:
        return
    
    modules_list = []
    usings_list = []
    
    for modulo in modules_to_register:
        modulo_capitalized = modulo.capitalize()
        modules_list.append(f"{modulo_capitalized}MenuConfig.GetMenuModule()")
        usings_list.append(f"@using Frontend.Layout.Menu.Modules")
    
    unificado_path = "Frontend/Layout/Menu/MenuUnificado.razor"
    
    async with ClaudeSDKClient(
        options=ClaudeCodeOptions(
            max_turns=3,
            allowed_tools=["Read", "Edit"]
        )
    ) as client:
        
        modules_to_add = ",\n            ".join(modules_list)
        
        prompt = f"""
Lee el archivo {unificado_path} y agrega las nuevas configuraciones de módulo.

En la sección donde se cargan los módulos (método LoadModules), agrega estas líneas:
{modules_to_add}

Mantén el formato y estructura existente del archivo. Los nuevos módulos deben agregarse a la lista existente.
"""
        await client.query(prompt)
        await auto_confirm(client)

async def main():
    print("🚀 GenerateMenu BATCH - Procesador masivo de menús desde JSON")
    print("=" * 60)
    
    # Verificar argumentos
    if len(sys.argv) != 2:
        print("❌ Error: Se requiere la ruta del archivo JSON")
        print("\nUso: python generate_menu_batch.py <ruta_entities_urls.json>")
        print("\nEjemplo:")
        print("  python generate_menu_batch.py ./entities-urls.json")
        print("  python generate_menu_batch.py ../datos/entities-urls.json")
        return
    
    json_file = sys.argv[1]
    
    # Verificar que existe el archivo
    if not os.path.exists(json_file):
        print(f"❌ Error: El archivo '{json_file}' no existe")
        return
    
    print(f"📄 Leyendo archivo JSON: {json_file}")
    
    # 1. Leer y parsear entidades desde JSON
    entities = parse_json_file(json_file)
    
    if not entities:
        print("❌ No se encontraron entidades válidas en el archivo")
        return
    
    print(f"✅ Se encontraron {len(entities)} entidades válidas")
    
    # Mostrar resumen
    print("\n📋 Resumen de entidades:")
    for entity in entities:
        path_parts = [entity['modulo']]
        if entity['submodulo']:
            path_parts.append(entity['submodulo'])
        path_parts.extend([entity['entidad'], 'list'])
        path = '/' + '/'.join(path_parts)
        
        print(f"  🔹 {entity['entidad'].capitalize()} → {path} (IA elegirá icono)")
    
    # Confirmar procesamiento (auto-confirmar en modo batch)
    print(f"\n✅ Procesando {len(entities)} entidades desde JSON automáticamente...")
    
    # 2. Agrupar por módulos
    modules = group_entities_by_module(entities)
    
    print(f"\n🗂️  Se procesarán {len(modules)} módulos:")
    for modulo, data in modules.items():
        status = "nuevo" if not data['config_path'].exists() else "existente"
        print(f"  📁 {modulo.capitalize()} ({status}) - {len(data['entities'])} entidades")
    
    # 3. Procesar cada módulo
    modules_to_register = []
    
    for modulo, module_data in modules.items():
        result = await process_module_batch(modulo, module_data)
        
        if result['needs_registration']:
            modules_to_register.append(modulo)
    
    # 4. Registrar módulos nuevos en MenuUnificado
    if modules_to_register:
        print(f"\n🔗 Registrando {len(modules_to_register)} módulos nuevos en MenuUnificado.razor...")
        await register_modules_in_unificado(modules_to_register)
        print("✅ Módulos registrados en MenuUnificado.razor")
    
    # 5. Resumen final
    print("\n" + "=" * 60)
    print("🎉 PROCESAMIENTO MASIVO COMPLETADO!")
    print(f"✅ {len(entities)} entidades procesadas")
    print(f"✅ {len(modules)} módulos actualizados")
    if modules_to_register:
        print(f"✅ {len(modules_to_register)} módulos nuevos registrados")
    
    print("\n📊 Módulos actualizados:")
    for modulo, module_data in modules.items():
        config_file = module_data['config_path'].name
        entity_count = len(module_data['entities'])
        print(f"  📁 {config_file} - {entity_count} entidades agregadas")
    
    print("\n🔍 Revisa los archivos generados y prueba el menú en la aplicación!")

def create_sample_file():
    """Crea un archivo de ejemplo para mostrar el formato"""
    sample_content = """# Ejemplo de archivo de entidades para GenerateMenu BATCH
# Formato: modulo/entidad o modulo/submodulo/entidad

# Módulo Principal
principal/productos
principal/categorias
principal/clientes
principal/proveedores

# Módulo Inventario
inventario/core/marcas
inventario/core/unidades
inventario/stock/almacenes
inventario/stock/ubicaciones

# Módulo Administración
administracion/usuarios
administracion/roles
administracion/permisos

# Módulo Ventas
ventas/facturas
ventas/cotizaciones
ventas/clientes

# Módulo Compras
compras/ordenes
compras/recepciones
compras/proveedores

# Módulo Reportes
reportes/ventas
reportes/inventario
reportes/finanzas
"""
    
    with open("ejemplo_entidades.txt", 'w', encoding='utf-8') as f:
        f.write(sample_content)
    
    print("✅ Archivo de ejemplo creado: ejemplo_entidades.txt")

def show_help():
    """Muestra ayuda de uso"""
    print("🚀 GenerateMenu BATCH - Procesador masivo de menús desde JSON")
    print("=" * 60)
    print()
    print("Uso:")
    print("  python generate_menu_batch.py <ruta_entities_urls.json>")
    print()
    print("Ejemplos:")
    print("  python generate_menu_batch.py ./entities-urls.json")
    print("  python generate_menu_batch.py ../datos/entities-urls.json")
    print("  python generate_menu_batch.py C:\\proyecto\\entities-urls.json")
    print()
    print("Comandos especiales:")
    print("  python generate_menu_batch.py --help       # Muestra esta ayuda")
    print()
    print("Formato del archivo JSON:")
    print("  📄 Debe contener array de objetos con: entidad, modulo, urllista")
    print("  🔑 Genera permisos en formato: [ENTIDAD_MAYUSCULA].VIEWMENU")
    print("  📝 Ejemplo: NIVELEDUCACIONALEMPLEADO.VIEWMENU")
    print()
    print("¿Qué hace?")
    print("  ✅ Lee entidades desde el archivo JSON especificado")
    print("  ✅ Convierte módulos: 'RRHH.Core' → 'rrhh/core'")
    print("  ✅ Genera permisos automáticamente en mayúsculas")
    print("  ✅ Crea módulos automáticamente si no existen")
    print("  ✅ Registra módulos nuevos en MenuUnificado.razor")
    print("  ✅ Genera MenuItems con iconos inteligentes")
    print()
    print("Ventajas del procesamiento desde JSON:")
    print("  ⚡ Procesa todas las entidades del JSON")
    print("  🎯 Agrupa por módulos para eficiencia")
    print("  🔄 Una sola operación de IA por módulo")
    print("  📊 Permisos consistentes automáticamente")

if __name__ == "__main__":
    if len(sys.argv) == 1:
        print("❌ Error: Se requiere la ruta del archivo JSON")
        print("Uso: python generate_menu_batch.py <ruta_entities_urls.json>")
        print("Para ayuda: python generate_menu_batch.py --help")
    elif len(sys.argv) == 2:
        if sys.argv[1] in ["-h", "--help", "help"]:
            show_help()
        else:
            # Archivo JSON especificado, ejecutar
            asyncio.run(main())
    else:
        print("❌ Demasiados argumentos.")
        print("Uso: python generate_menu_batch.py <ruta_entities_urls.json>")
        print("Para ayuda: python generate_menu_batch.py --help")