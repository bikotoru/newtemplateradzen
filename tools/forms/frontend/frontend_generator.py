#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
🎨 Frontend Generator
Genera Service del frontend usando templates
"""

import sys
from pathlib import Path

class FrontendGenerator:
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
        """Generar archivo Service del frontend usando template"""
        try:
            # Preparar variables para el template
            variables = self.template_engine.prepare_entity_variables(entity_name, module)
            
            # Renderizar template
            service_content = self.template_engine.render_template("frontend/services/service.cs.template", variables)
            
            # Escribir archivo
            service_file = module_path / f"{entity_name}Service.cs"
            service_file.write_text(service_content, encoding='utf-8')
            print(f"✅ Frontend {entity_name}Service.cs generado")
            return True
            
        except Exception as e:
            print(f"❌ ERROR generando Frontend Service: {e}")
            return False
    
    def create_module_directory(self, module):
        """Crear directorio del módulo frontend (manejar subdirectorios con punto)"""
        module_parts = module.split('.')
        frontend_module_path = self.root_path / "Frontend" / "Modules"
        for part in module_parts:
            frontend_module_path = frontend_module_path / part
        frontend_module_path.mkdir(parents=True, exist_ok=True)
        return frontend_module_path
    
    def generate_service_only(self, entity_name, module):
        """Generar solo el service frontend (FASE 3.1)"""
        try:
            print(f"🎨 Generando service frontend para entidad: {entity_name}")
            print(f"📁 Módulo: {module}")
            
            # 1. Crear directorio
            module_path = self.create_module_directory(module)
            print(f"📁 Directorio creado: {module_path}")
            
            # 2. Generar Service
            if not self.generate_service(entity_name, module, module_path):
                return False
            
            return True
            
        except Exception as e:
            print(f"❌ ERROR en frontend generator: {e}")
            return False