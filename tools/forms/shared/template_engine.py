#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
📝 Template Engine
Motor de templates simple para generar código
"""

from pathlib import Path
import re

class TemplateEngine:
    def __init__(self, templates_path):
        self.templates_path = Path(templates_path)
    
    def render_template(self, template_name, variables):
        """Renderizar un template con las variables especificadas"""
        template_file = self.templates_path / template_name
        
        if not template_file.exists():
            raise FileNotFoundError(f"Template no encontrado: {template_file}")
        
        # Leer template
        template_content = template_file.read_text(encoding='utf-8')
        
        # Reemplazar variables {{VARIABLE}}
        for var_name, var_value in variables.items():
            placeholder = f"{{{{{var_name}}}}}"
            template_content = template_content.replace(placeholder, str(var_value))
        
        return template_content
    
    def prepare_entity_variables(self, entity_name, module):
        """Preparar variables estándar para entidades"""
        # Convertir módulo a ruta de API (Inventario.Core -> inventario/core)
        module_route = module.replace('.', '/').lower()
        
        return {
            'ENTITY_NAME': entity_name,
            'ENTITY_NAME_LOWER': entity_name.lower(),
            'MODULE': module,
            'MODULE_PATH': module.replace('.', '/'),
            'MODULE_ROUTE': module_route,
            'NAMESPACE': f"Backend.Modules.{module}"
        }