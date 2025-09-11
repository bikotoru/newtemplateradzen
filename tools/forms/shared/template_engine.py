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
        
        # Generar plural de la entidad
        entity_plural = f"{entity_name}s" if not entity_name.endswith('s') else entity_name
        
        return {
            'ENTITY_NAME': entity_name,
            'ENTITY_NAME_LOWER': entity_name.lower(),
            'ENTITY_UPPER': entity_name.upper(),
            'ENTITY_LOWER': entity_name.lower(),
            'ENTITY_DISPLAY_NAME': entity_name,
            'ENTITY_PLURAL': entity_plural,
            'MODULE': module,
            'MODULE_PATH': module.replace('.', '/').lower(),
            'MODULE_ROUTE': module_route,
            'BACKEND_NAMESPACE': f"Backend.Modules.{module}.{entity_plural}",
            'FRONTEND_NAMESPACE': f"Frontend.Modules.{module}.{entity_plural}",
            'MODULE_WITH_ENTITY': f"{module}.{entity_plural}",  # Solo la parte del módulo con entidad
            'NAMESPACE': f"Backend.Modules.{module}.{entity_plural}",  # Para compatibilidad
            'MODEL_NAMESPACE': "Shared.Models.Entities"  # Namespace por defecto del modelo
        }