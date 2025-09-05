#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
✅ Validaciones globales
Validaciones compartidas para el generador de entidades
"""

import re
from pathlib import Path

class EntityValidator:
    def __init__(self, root_path):
        self.root_path = Path(root_path)
    
    def validate_entity_inputs(self, entity_name, module, phase):
        """Validaciones básicas de entrada"""
        if not entity_name:
            raise ValueError("Entity name es requerido")
        
        if not module:
            raise ValueError("Module es requerido")
        
        if phase not in [1, 2, 3, 3.2, 3.3, 3.4, 3.5]:
            raise ValueError("Phase debe ser 1, 2, 3, 3.2, 3.3, 3.4 o 3.5")
        
        # Validar que la entidad no sea una palabra reservada
        reserved_words = ['user', 'order', 'table', 'index', 'key', 'value']
        if entity_name.lower() in reserved_words:
            raise ValueError(f"'{entity_name}' es una palabra reservada")
        
        # Validar formato del nombre de entidad
        if not re.match(r'^[A-Z][a-zA-Z0-9]*$', entity_name):
            raise ValueError(f"Nombre de entidad inválido: '{entity_name}'. Debe empezar con mayúscula y ser PascalCase")
        
        return True
    
    def validate_project_structure(self):
        """Validar que la estructura del proyecto existe"""
        required_paths = [
            "Backend/Modules",
            "Frontend/Modules",
            "Shared.Models/Entities",
            "Backend/Services/ServiceRegistry.cs",
            "Frontend/Services/ServiceRegistry.cs"
        ]
        
        for path in required_paths:
            full_path = self.root_path / path
            if not full_path.exists():
                raise ValueError(f"Ruta requerida no encontrada: {full_path}")
        
        return True
    
    def validate_phase_requirements(self, phase, fields=None):
        """Validar requerimientos específicos por fase"""
        if phase == 1 and not fields:
            raise ValueError("FASE 1 requiere especificar --fields")
        
        return True