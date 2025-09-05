#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
📝 Backend Service Registry Updater
Actualiza ServiceRegistry.cs del backend
"""

from pathlib import Path

class BackendServiceRegistry:
    def __init__(self, root_path):
        self.root_path = Path(root_path)
    
    def update(self, entity_name, module):
        """Actualizar ServiceRegistry del backend"""
        registry_file = self.root_path / "Backend" / "Services" / "ServiceRegistry.cs"
        
        if not registry_file.exists():
            print(f"❌ ServiceRegistry no encontrado: {registry_file}")
            return False
        
        try:
            # Leer contenido actual
            content = registry_file.read_text(encoding='utf-8')
            
            # 1. Agregar using al inicio
            using_line = f"using Backend.Modules.{module};"
            if using_line not in content:
                # Buscar donde insertar el using
                lines = content.split('\n')
                insert_index = 0
                for i, line in enumerate(lines):
                    if line.startswith('using Backend.Modules.'):
                        insert_index = i + 1
                    elif line.startswith('using Backend.Utils.') and insert_index == 0:
                        insert_index = i
                        break
                
                lines.insert(insert_index, using_line)
                content = '\n'.join(lines)
                print(f"✅ Using agregado: {using_line}")
            
            # 2. Agregar registro del servicio
            service_registration = f"        services.AddScoped<{entity_name}Service>();"
            if service_registration not in content:
                # Buscar donde insertar el servicio
                lines = content.split('\n')
                for i, line in enumerate(lines):
                    if "// Module Services" in line:
                        # Insertar después del comentario
                        lines.insert(i + 1, service_registration)
                        content = '\n'.join(lines)
                        print(f"✅ Servicio registrado: {entity_name}Service")
                        break
            
            # Escribir archivo actualizado
            registry_file.write_text(content, encoding='utf-8')
            print(f"✅ Backend ServiceRegistry actualizado")
            return True
            
        except Exception as e:
            print(f"❌ ERROR actualizando backend ServiceRegistry: {e}")
            return False