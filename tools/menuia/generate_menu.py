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

# Mapeo de iconos inteligente por entidad
ICON_MAPPING = {
    # Gestión básica
    'categoria': 'category', 'categorias': 'category',
    'producto': 'inventory', 'productos': 'inventory',
    'cliente': 'person', 'clientes': 'people',
    'usuario': 'account_box', 'usuarios': 'people',
    'proveedor': 'business', 'proveedores': 'business',
    
    # Administración
    'rol': 'admin_panel_settings', 'roles': 'admin_panel_settings',
    'permiso': 'security', 'permisos': 'security',
    'configuracion': 'settings', 'configuraciones': 'settings',
    
    # Finanzas/Comercio
    'venta': 'point_of_sale', 'ventas': 'point_of_sale',
    'compra': 'shopping_cart', 'compras': 'shopping_cart',
    'factura': 'receipt', 'facturas': 'receipt',
    'pago': 'payment', 'pagos': 'payment',
    
    # Reportes/Analytics
    'reporte': 'analytics', 'reportes': 'analytics',
    'dashboard': 'dashboard', 'dashboards': 'dashboard',
    'estadistica': 'bar_chart', 'estadisticas': 'bar_chart',
    
    # Documentos
    'documento': 'description', 'documentos': 'description',
    'archivo': 'folder', 'archivos': 'folder',
    
    # Default
    'default': 'list'
}

def get_icon_for_entity(entidad):
    """Obtiene el icono apropiado para la entidad"""
    entidad_lower = entidad.lower()
    return ICON_MAPPING.get(entidad_lower, ICON_MAPPING['default'])

def parse_input(input_str):
    """
    Parsea el input en formato jerárquico: modulo/sub1/sub2/.../entidad
    Retorna: (modulo, submodulo_path, entidad)
    """
    parts = input_str.strip().split('/')
    
    if len(parts) < 2:
        raise ValueError("Formato debe ser: modulo/entidad o modulo/submodulo/entidad")
    
    modulo = parts[0]
    entidad = parts[-1]  # La entidad siempre es el último elemento
    
    # Si hay elementos intermedios, crear el path de submódulos
    if len(parts) > 2:
        submodulo_path = "/".join(parts[1:-1])  # Todos los elementos entre modulo y entidad
        return modulo, submodulo_path, entidad
    else:
        return modulo, None, entidad

def get_config_filename(modulo):
    """Genera el nombre del archivo de configuración"""
    modulo_capitalized = modulo.capitalize()
    return f"{modulo_capitalized}MenuConfig.cs"

def get_config_path(modulo):
    """Obtiene la ruta completa del archivo de configuración"""
    base_path = Path("Frontend/Layout/Menu/Modules")
    config_filename = get_config_filename(modulo)
    return base_path / config_filename

def generate_config_content(modulo):
    """Genera el contenido del archivo de configuración del módulo"""
    modulo_capitalized = modulo.capitalize()
    modulo_title = modulo.title()
    
    # Elegir icono por módulo
    module_icons = {
        'principal': 'dashboard',
        'administracion': 'settings',
        'admin': 'settings',
        'ventas': 'point_of_sale',
        'compras': 'shopping_cart',
        'inventario': 'inventory',
        'reportes': 'analytics',
        'finanzas': 'account_balance',
        'configuracion': 'settings'
    }
    
    module_icon = module_icons.get(modulo.lower(), 'folder')
    
    return f"""using Frontend.Components.Base;

namespace Frontend.Layout.Menu.Modules;

public static class {modulo_capitalized}MenuConfig
{{
    public static MenuModule GetMenuModule()
    {{
        return new MenuModule
        {{
            Text = "📊 {modulo_title}",
            Icon = "{module_icon}",
            MenuItems = new List<MenuItem>
            {{
                // Los elementos del menú se agregarán aquí
            }}
        }};
    }}
}}"""

def generate_menu_item(modulo, submodulo_path, entidad):
    """Genera el MenuItem listo para insertar"""
    entidad_capitalized = entidad.capitalize()
    entidad_upper = entidad.upper()
    icon = get_icon_for_entity(entidad)
    
    # Generar path jerárquico
    if submodulo_path:
        path = f"/{modulo.lower()}/{submodulo_path.lower()}/{entidad.lower()}/list"
    else:
        path = f"/{modulo.lower()}/{entidad.lower()}/list"
    
    return f"""new MenuItem
{{
    Text = "{entidad_capitalized}",
    Icon = "{icon}",
    Path = "{path}",
    Permissions = new List<string>{{ "{entidad_upper}.VIEWMENU"}}
}}"""

async def auto_confirm(client):
    """Auto-confirma permisos"""
    async for message in client.receive_response():
        if hasattr(message, 'content'):
            for block in message.content:
                if hasattr(block, 'text'):
                    text = block.text
                    print(text)
                    if any(word in text.lower() for word in ["permission", "permisos", "proceed", "continuar"]):
                        await client.query("Sí, procede.")

def check_module_in_unificado(modulo):
    """Verifica si el módulo ya está registrado en MenuUnificado.razor"""
    unificado_path = Path("Frontend/Layout/Menu/MenuUnificado.razor")
    if not unificado_path.exists():
        return False
    
    with open(unificado_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    modulo_capitalized = modulo.capitalize()
    return f"{modulo_capitalized}MenuConfig.GetMenuModule()" in content

async def add_module_to_unificado(modulo):
    """Agrega el módulo al MenuUnificado.razor"""
    modulo_capitalized = modulo.capitalize()
    unificado_path = "Frontend/Layout/Menu/MenuUnificado.razor"
    
    async with ClaudeSDKClient(
        options=ClaudeCodeOptions(
            max_turns=2,
            allowed_tools=["Read", "Edit"]
        )
    ) as client:
        
        prompt = f"""
Lee el archivo {unificado_path} y agrega la nueva configuración de módulo.

En la sección donde se cargan los módulos (método LoadModules), agrega esta línea:
{modulo_capitalized}MenuConfig.GetMenuModule(),

También agrega el using correspondiente en la parte superior:
@using Frontend.Layout.Menu.Modules

Mantén el formato y estructura existente del archivo.
"""
        await client.query(prompt)
        await auto_confirm(client)

async def main():
    if len(sys.argv) != 2:
        print("❌ Error: Se requiere un argumento")
        print("Uso: python generate_menu.py <modulo/[submódulos]/entidad>")
        print("Ejemplos:")
        print("  python generate_menu.py principal/productos")
        print("  python generate_menu.py administracion/usuarios")
        print("  python generate_menu.py rrhh/configuracion/mantenedores/empleado/tipobonificacion")
        print("  python generate_menu.py rrhh/configuracion/global/configuraciongeneralrrhh")
        print("  python generate_menu.py core/localidades/region")
        return
    
    try:
        modulo, submodulo, entidad = parse_input(sys.argv[1])
    except ValueError as e:
        print(f"❌ Error: {e}")
        return
    
    print("🚀 GenerateMenu - Creador inteligente de menús")
    print(f"📁 Módulo: {modulo}")
    if submodulo:
        print(f"📂 Submódulo: {submodulo}")
    print(f"🆕 Entidad: {entidad}")
    print("-" * 60)
    
    # 1. Verificar/crear archivo de configuración
    config_path = get_config_path(modulo)
    config_created = False
    
    if not config_path.exists():
        print(f"📝 Creando archivo de configuración: {config_path}")
        
        # Crear directorio si no existe
        config_path.parent.mkdir(parents=True, exist_ok=True)
        
        # Crear archivo
        with open(config_path, 'w', encoding='utf-8') as f:
            f.write(generate_config_content(modulo))
        
        config_created = True
        print(f"✅ Archivo creado: {config_path}")
    else:
        print(f"✅ Archivo ya existe: {config_path}")
    
    # 2. Registrar módulo en MenuUnificado si es nuevo
    if config_created:
        print("🔗 Registrando módulo en MenuUnificado.razor...")
        if not check_module_in_unificado(modulo):
            await add_module_to_unificado(modulo)
            print("✅ Módulo registrado en MenuUnificado.razor")
        else:
            print("✅ Módulo ya estaba registrado en MenuUnificado.razor")
    
    # 3. Generar MenuItem
    menu_item = generate_menu_item(modulo, submodulo, entidad)
    
    print("\n📋 MenuItem generado:")
    print("-" * 40)
    print(menu_item)
    print("-" * 40)
    
    # 4. Usar MenuIA para agregarlo
    print("\n🤖 Usando MenuIA para agregar el elemento...")
    
    # Ejecutar MenuIA
    menu_ai_path = Path(__file__).parent / "menu_ai.py"
    entidad_upper = entidad.upper()
    
    import subprocess
    
    cmd = [
        sys.executable, 
        str(menu_ai_path), 
        str(config_path),
        f"Código MenuItem: {menu_item}",
        f"{entidad_upper}.VIEWMENU"
    ]
    
    # Usar MenuIA con cliente directo
    async with ClaudeSDKClient(
        options=ClaudeCodeOptions(
            max_turns=3,
            allowed_tools=["Read", "Edit", "MultiEdit"]
        )
    ) as client:
        
        prompt = f"""
Lee el archivo {config_path} y agrega este MenuItem en la posición correcta:

{menu_item}

Reglas:
1. Agregarlo dentro del array MenuItems
2. Mantener orden alfabético
3. Si el array está vacío (solo tiene comentario), reemplazar el comentario
4. Mantener la indentación y formato existente
5. Cada MenuItem debe estar separado por comas

El archivo debe quedar bien formateado y funcional.
"""
        
        await client.query(prompt)
        await auto_confirm(client)
    
    print("\n" + "="*60)
    print("✅ GenerateMenu completado!")
    print(f"📁 Módulo: {config_path}")
    print(f"🆕 Entidad agregada: {entidad.capitalize()}")
    print(f"🔐 Permiso: {entidad.upper()}.VIEWMENU")
    
    # Mostrar resultado final
    if config_path.exists():
        print(f"\n📄 Contenido final de {config_path.name}:")
        print("-" * 40)
        with open(config_path, 'r', encoding='utf-8') as f:
            print(f.read())
        print("-" * 40)

def show_help():
    """Muestra ayuda de uso"""
    print("🚀 GenerateMenu - Creador inteligente de menús")
    print()
    print("Uso:")
    print("  python generate_menu.py <modulo/entidad>")
    print("  python generate_menu.py <modulo/submodulo/entidad>")
    print()
    print("Ejemplos:")
    print("  python generate_menu.py principal/productos")
    print("  python generate_menu.py administracion/usuarios")
    print("  python generate_menu.py ventas/clientes/clientes")
    print("  python generate_menu.py inventario/categorias")
    print()
    print("¿Qué hace?")
    print("  ✅ Crea el archivo [Modulo]MenuConfig.cs si no existe")
    print("  ✅ Lo registra en MenuUnificado.razor automáticamente")
    print("  ✅ Genera el MenuItem con icono inteligente")
    print("  ✅ Usa MenuIA para agregarlo en la posición correcta")
    print("  ✅ Configura permisos automáticamente ([ENTIDAD].VIEWMENU)")
    print()
    print("Iconos inteligentes:")
    print("  📦 productos → inventory")
    print("  👥 usuarios → people")
    print("  📊 reportes → analytics")
    print("  🏢 clientes → person")
    print("  ⚙️ configuracion → settings")

if __name__ == "__main__":
    if len(sys.argv) == 2 and sys.argv[1] in ["-h", "--help", "help"]:
        show_help()
    else:
        asyncio.run(main())