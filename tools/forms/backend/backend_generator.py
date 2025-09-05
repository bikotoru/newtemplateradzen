#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
🔧 Backend Generator
Genera Service y Controller para el backend usando templates
"""

import sys
from pathlib import Path

class BackendGenerator:
    def __init__(self, root_path):
        self.root_path = Path(root_path)
        self.forms_path = self.root_path / "tools" / "forms"
        
        # Importar template engine
        sys.path.append(str(self.forms_path))
        from shared.template_engine import TemplateEngine
        
        # Inicializar motor de templates
        templates_path = self.forms_path / "templates"
        self.template_engine = TemplateEngine(templates_path)
    
    def generate_service(self, entity_name, module, module_path):
        """Generar archivo Service del backend usando template"""
        try:
            # Preparar variables para el template
            variables = self.template_engine.prepare_entity_variables(entity_name, module)
            
            # Renderizar template
            service_content = self.template_engine.render_template("backend/service.cs.template", variables)
            
            # Escribir archivo
            service_file = module_path / f"{entity_name}Service.cs"
            service_file.write_text(service_content, encoding='utf-8')
            print(f"✅ {entity_name}Service.cs generado")
            return True
            
        except Exception as e:
            print(f"❌ ERROR generando Service: {e}")
            return False
    
    def generate_controller(self, entity_name, module, module_path):
        """Generar archivo Controller del backend usando template"""
        try:
            # Preparar variables para el template
            variables = self.template_engine.prepare_entity_variables(entity_name, module)
            
            # Renderizar template
            controller_content = self.template_engine.render_template("backend/controller.cs.template", variables)
            
            # Escribir archivo
            controller_file = module_path / f"{entity_name}Controller.cs"
            controller_file.write_text(controller_content, encoding='utf-8')
            print(f"✅ {entity_name}Controller.cs generado")
            return True
            
        except Exception as e:
            print(f"❌ ERROR generando Controller: {e}")
            return False
    
    def create_module_directory(self, module):
        """Crear directorio del módulo (manejar subdirectorios con punto)"""
        module_parts = module.split('.')
        backend_module_path = self.root_path / "Backend" / "Modules"
        for part in module_parts:
            backend_module_path = backend_module_path / part
        backend_module_path.mkdir(parents=True, exist_ok=True)
        return backend_module_path
    
    def generate(self, entity_name, module):
        """Generar backend completo"""
        try:
            print(f"🔧 Generando backend para entidad: {entity_name}")
            print(f"📁 Módulo: {module}")
            
            # 1. Crear directorio
            module_path = self.create_module_directory(module)
            print(f"📁 Directorio creado: {module_path}")
            
            # 2. Generar Service
            if not self.generate_service(entity_name, module, module_path):
                return False
            
            # 3. Generar Controller
            if not self.generate_controller(entity_name, module, module_path):
                return False
            
            return True
            
        except Exception as e:
            print(f"❌ ERROR en backend generator: {e}")
            return False