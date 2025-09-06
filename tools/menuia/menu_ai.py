import asyncio
import sys
import os
from pathlib import Path
from claude_code_sdk import ClaudeSDKClient, ClaudeCodeOptions

async def auto_confirm(client):
    """Auto-confirma permisos y retorna respuestas"""
    responses = []
    async for message in client.receive_response():
        if hasattr(message, 'content'):
            for block in message.content:
                if hasattr(block, 'text'):
                    text = block.text
                    print(text)
                    responses.append(text)
                    
                    # Auto-confirmar permisos
                    if any(word in text.lower() for word in ["permission", "permisos", "proceed", "continuar"]):
                        await client.query("Sí, procede.")
    return responses

def load_context():
    """Carga el contexto desde context.md"""
    context_path = Path(__file__).parent / "context.md"
    if context_path.exists():
        with open(context_path, 'r', encoding='utf-8') as f:
            return f.read()
    return ""

def validate_inputs(path_archivo, nueva_entidad, permiso):
    """Valida que los inputs sean correctos"""
    if not path_archivo or not nueva_entidad or not permiso:
        print("❌ Error: Todos los parámetros son requeridos")
        print("Uso: python menu_ai.py <path_archivo> <nueva_entidad> <permiso>")
        print("Ejemplo: python menu_ai.py Frontend/Layout/Menu/Modules/PrincipalMenuConfig.cs Productos PRODUCTOS.*")
        return False
    
    # Verificar que el archivo existe
    if not os.path.exists(path_archivo):
        print(f"❌ Error: El archivo '{path_archivo}' no existe")
        return False
        
    return True

async def main():
    # Verificar argumentos
    if len(sys.argv) != 4:
        print("❌ Error: Número incorrecto de argumentos")
        print("Uso: python menu_ai.py <path_archivo> <nueva_entidad> <permiso>")
        print("Ejemplo: python menu_ai.py Frontend/Layout/Menu/Modules/PrincipalMenuConfig.cs Productos PRODUCTOS.*")
        return
    
    path_archivo = sys.argv[1]
    nueva_entidad = sys.argv[2]
    permiso = sys.argv[3]
    
    # Validar inputs
    if not validate_inputs(path_archivo, nueva_entidad, permiso):
        return
    
    print("🤖 MenuIA - Agregando entidad al menú de forma inteligente")
    print(f"📁 Archivo: {path_archivo}")
    print(f"🆕 Nueva Entidad: {nueva_entidad}")
    print(f"🔐 Permiso: {permiso}")
    print("-" * 60)
    
    # Cargar contexto
    context = load_context()
    
    async with ClaudeSDKClient(
        options=ClaudeCodeOptions(
            max_turns=3,
            allowed_tools=["Read", "Edit", "MultiEdit"]
        )
    ) as client:
        
        print("=== PASO 1: Analizando archivo actual ===")
        
        # Crear el prompt inteligente con contexto
        prompt = f"""
{context}

Por favor realiza la siguiente tarea:

1. Lee el archivo: {path_archivo}
2. Analiza su estructura, patrones e iconos existentes
3. Agrega inteligentemente la nueva entidad "{nueva_entidad}" con permiso "{permiso}"
4. Decide el mejor módulo, icono y posición según las reglas del contexto
5. Mantén el formato y estilo original del archivo

Inputs:
- Archivo a modificar: {path_archivo}
- Nueva entidad: {nueva_entidad}
- Permiso requerido: {permiso}

Sigue todas las reglas de decisión inteligente del contexto para elegir el icono correcto, la ubicación apropiada y la ruta siguiendo el patrón existente.
"""
        
        # Enviar tarea a Claude
        await client.query(prompt)
        responses = await auto_confirm(client)
        
        print("\n" + "="*60)
        print("✅ MenuIA completado!")
        print("🔍 Revisa los cambios realizados en el archivo")
        print("📝 La entidad fue agregada inteligentemente según el contexto")
        
        # Mostrar resumen del archivo modificado
        if os.path.exists(path_archivo):
            print(f"\n📄 Contenido actualizado de {path_archivo}:")
            print("-" * 40)
            with open(path_archivo, 'r', encoding='utf-8') as f:
                content = f.read()
                # Mostrar solo las líneas relevantes (MenuItems)
                lines = content.split('\n')
                in_menu_items = False
                for line in lines:
                    if 'MenuItems = new List<MenuItem>' in line:
                        in_menu_items = True
                    if in_menu_items:
                        print(line)
                    if in_menu_items and line.strip() == '};' and 'MenuItems' not in line:
                        break
            print("-" * 40)

def show_help():
    """Muestra ayuda de uso"""
    print("🤖 MenuIA - Herramienta inteligente para agregar entidades al menú")
    print()
    print("Uso:")
    print("  python menu_ai.py <path_archivo> <nueva_entidad> <permiso>")
    print()
    print("Ejemplos:")
    print("  python menu_ai.py Frontend/Layout/Menu/Modules/PrincipalMenuConfig.cs Productos PRODUCTOS.*")
    print("  python menu_ai.py Frontend/Layout/Menu/Modules/AdministracionMenuConfig.cs Usuarios ADMIN.USUARIOS")
    print("  python menu_ai.py Frontend/Layout/Menu/Modules/PrincipalMenuConfig.cs Reportes REPORTES.VIEW")
    print()
    print("La IA decidirá automáticamente:")
    print("  ✅ El icono más apropiado")
    print("  ✅ La ubicación correcta en el módulo")
    print("  ✅ El formato de ruta siguiendo patrones")
    print("  ✅ El orden alfabético o lógico")
    print()
    print("Para más detalles, revisa el archivo context.md")

if __name__ == "__main__":
    if len(sys.argv) == 2 and sys.argv[1] in ["-h", "--help", "help"]:
        show_help()
    else:
        asyncio.run(main())