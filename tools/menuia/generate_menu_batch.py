import asyncio
import sys
import os
import re
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

def parse_batch_file(file_path):
    """
    Lee un archivo con entidades en formato:
    modulo/entidad
    modulo/submodulo/entidad
    # comentarios ignorados
    """
    entities = []
    
    with open(file_path, 'r', encoding='utf-8') as f:
        for line_num, line in enumerate(f, 1):
            line = line.strip()
            
            # Ignorar líneas vacías y comentarios
            if not line or line.startswith('#'):
                continue
            
            try:
                modulo, submodulo, entidad = parse_input(line)
                entities.append({
                    'line': line_num,
                    'input': line,
                    'modulo': modulo,
                    'submodulo': submodulo,
                    'entidad': entidad
                })
            except ValueError as e:
                print(f"⚠️  Línea {line_num} ignorada: '{line}' - {e}")
    
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
            'input_original': entity['input']
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
        
        # Crear prompt inteligente con TODO el contexto
        entities_list = "\n".join([
            f"- {ctx['entidad']} (desde: {ctx['input_original']})" 
            for ctx in entities_context
        ])
        
        prompt = f"""
{context}

TAREA: Agregar múltiples entidades al archivo de menú de forma inteligente.

Archivo a modificar: {config_path}

Entidades a agregar:
{entities_list}

INSTRUCCIONES INTELIGENTES:
1. Lee el archivo actual para entender el contexto y patrones existentes
2. Para cada entidad, decide inteligentemente:
   - El ICONO más apropiado según su nombre/función
   - La RUTA siguiendo el patrón existente
   - El ORDEN correcto (alfabético o por funcionalidad)
   - El PERMISO en formato [ENTIDAD].VIEWMENU

3. Usa tu conocimiento del contexto para:
   - Elegir iconos coherentes (productos→inventory, usuarios→people, etc.)
   - Mantener consistencia en rutas
   - Agrupar lógicamente si es apropiado
   - Evitar duplicados

4. Genera todos los MenuItems con el formato correcto:
   new MenuItem
   {{
       Text = "[Nombre]",
       Icon = "[icono_inteligente]",
       Path = "/[ruta/siguiendo/patron]/list",
       Permissions = new List<string>{{ "[ENTIDAD].VIEWMENU"}}
   }}

5. Agrega todo al archivo manteniendo formato y orden correcto.

La IA debe tomar decisiones inteligentes sobre iconos y orden basándose en el contexto completo.
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
    print("🚀 GenerateMenu BATCH - Procesador masivo de menús")
    print("=" * 60)
    
    # Verificar argumentos
    if len(sys.argv) != 2:
        print("❌ Error: Se requiere un archivo de entidades")
        print("\nUso: python generate_menu_batch.py <archivo_entidades.txt>")
        print("\nEjemplos:")
        print("  python generate_menu_batch.py entidades.txt")
        print("  python generate_menu_batch.py mis_menus.txt")
        print("\nFormato del archivo:")
        print("  principal/productos")
        print("  principal/categorias")
        print("  inventario/core/marcas")
        print("  administracion/usuarios")
        print("  # comentarios son ignorados")
        return
    
    batch_file = sys.argv[1]
    
    # Verificar que existe el archivo
    if not os.path.exists(batch_file):
        print(f"❌ Error: El archivo '{batch_file}' no existe")
        return
    
    print(f"📄 Procesando archivo: {batch_file}")
    
    # 1. Leer y parsear entidades
    entities = parse_batch_file(batch_file)
    
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
    
    # Confirmar procesamiento
    response = input(f"\n¿Procesar {len(entities)} entidades? (s/n): ")
    if response.lower() not in ['s', 'si', 'sí', 'y', 'yes']:
        print("❌ Operación cancelada")
        return
    
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
    print("🚀 GenerateMenu BATCH - Procesador masivo de menús")
    print("=" * 60)
    print()
    print("Uso:")
    print("  python generate_menu_batch.py <archivo_entidades.txt>")
    print()
    print("Comandos especiales:")
    print("  python generate_menu_batch.py --example    # Crea archivo de ejemplo")
    print("  python generate_menu_batch.py --help       # Muestra esta ayuda")
    print()
    print("Formato del archivo de entidades:")
    print("  principal/productos")
    print("  inventario/core/marcas")
    print("  administracion/usuarios")
    print("  # Los comentarios son ignorados")
    print()
    print("¿Qué hace?")
    print("  ✅ Procesa múltiples entidades de una vez")
    print("  ✅ Crea módulos automáticamente si no existen")
    print("  ✅ Registra módulos nuevos en MenuUnificado.razor")
    print("  ✅ Genera MenuItems con iconos inteligentes")
    print("  ✅ Mantiene orden alfabético en cada módulo")
    print()
    print("Ventajas del procesamiento masivo:")
    print("  ⚡ Procesa 20+ entidades en segundos")
    print("  🎯 Agrupa por módulos para eficiencia")
    print("  🔄 Una sola operación de IA por módulo")
    print("  📊 Resumen completo al final")

if __name__ == "__main__":
    if len(sys.argv) == 2:
        if sys.argv[1] in ["-h", "--help", "help"]:
            show_help()
        elif sys.argv[1] == "--example":
            create_sample_file()
        else:
            asyncio.run(main())
    else:
        show_help()