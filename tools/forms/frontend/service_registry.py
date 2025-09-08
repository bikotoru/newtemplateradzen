#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
📝 Frontend Service Registry Updater
Actualiza ServiceRegistry.cs del frontend
"""

from pathlib import Path

class FrontendServiceRegistry:
    def __init__(self, root_path):
        self.root_path = Path(root_path)
    
    def update(self, entity_name, module):
        """Actualizar ServiceRegistry del frontend"""
        registry_file = self.root_path / "Frontend" / "Services" / "ServiceRegistry.cs"
        
        if not registry_file.exists():
            print(f"ERROR Frontend ServiceRegistry no encontrado: {registry_file}")
            return False
        
        try:
            # Leer contenido actual
            content = registry_file.read_text(encoding='utf-8')
            
            # Generar plural de la entidad
            entity_plural = f"{entity_name}s" if not entity_name.endswith('s') else entity_name
            
            # 1. Agregar using al inicio
            using_line = f"using Frontend.Modules.{module}.{entity_plural};"
            if using_line not in content:
                # Buscar donde insertar el using
                lines = content.split('\n')
                insert_index = 0
                for i, line in enumerate(lines):
                    if line.startswith('using Frontend.Modules.'):
                        insert_index = i + 1
                    elif line.startswith('using Frontend.Services') and insert_index == 0:
                        insert_index = i + 1
                        break
                
                lines.insert(insert_index, using_line)
                content = '\n'.join(lines)
                print(f"OK Frontend Using agregado: {using_line}")
            
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
                        print(f"OK Frontend Servicio registrado: {entity_name}Service")
                        break
            
            # Escribir archivo actualizado
            registry_file.write_text(content, encoding='utf-8')
            print(f"OK Frontend ServiceRegistry actualizado")
            
            # Actualizar GlobalUsings.cs
            self.update_global_usings(entity_name, module)
            
            return True
            
        except Exception as e:
            print(f"ERROR actualizando frontend ServiceRegistry: {e}")
            return False
    
    def update_global_usings(self, entity_name, module):
        """Actualizar GlobalUsings.cs del frontend"""
        try:
            global_usings_file = self.root_path / "Frontend" / "GlobalUsings.cs"
            entity_plural = f"{entity_name}s" if not entity_name.endswith('s') else entity_name
            using_line = f"global using Frontend.Modules.{module}.{entity_plural};"
            
            if global_usings_file.exists():
                content = global_usings_file.read_text(encoding='utf-8')
                if using_line not in content:
                    # Buscar la sección correcta para insertar
                    lines = content.split('\n')
                    insert_index = -1
                    
                    # Buscar después del último "global using Frontend.Modules"
                    for i, line in enumerate(lines):
                        if line.startswith('global using Frontend.Modules.'):
                            insert_index = i + 1
                    
                    # Si no hay módulos existentes, buscar después de "// Components" o antes de "// Radzen"
                    if insert_index == -1:
                        for i, line in enumerate(lines):
                            if line.strip() == '// Components':
                                insert_index = i
                                break
                            elif line.strip() == '// Radzen':
                                insert_index = i
                                break
                    
                    # Si aún no encuentra, insertar después de los Shared Models
                    if insert_index == -1:
                        for i, line in enumerate(lines):
                            if line.startswith('global using Shared.Models.'):
                                insert_index = i + 1
                    
                    # Si todo falla, insertar antes de Components
                    if insert_index == -1:
                        for i, line in enumerate(lines):
                            if line.startswith('global using Microsoft.AspNetCore.Components'):
                                insert_index = i
                                break
                    
                    if insert_index > -1:
                        # Agregar comentario si es el primer módulo
                        module_exists = any(line.startswith('global using Frontend.Modules.') for line in lines)
                        if not module_exists:
                            lines.insert(insert_index, "")
                            lines.insert(insert_index + 1, "// Module Services")
                            insert_index += 2
                        
                        lines.insert(insert_index, using_line)
                        global_usings_file.write_text('\n'.join(lines), encoding='utf-8')
                        print(f"OK Frontend GlobalUsings actualizado: {using_line}")
                    else:
                        print(f"WARNING: No se pudo encontrar dónde insertar en Frontend GlobalUsings")
                        
        except Exception as e:
            print(f"WARNING actualizando Frontend GlobalUsings: {e}")
            # No es crítico, continuar