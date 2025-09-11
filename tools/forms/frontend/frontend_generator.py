#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
🎨 Frontend Generator
Genera Service y ViewManager del frontend usando templates
"""

import sys
from pathlib import Path

class FrontendGenerator:
    def __init__(self, root_path):
        self.root_path = Path(root_path)
        self.forms_path = self.root_path / "tools" / "forms"
        
        # Importar template engine y generadores
        sys.path.append(str(self.forms_path))
        from shared.template_engine import TemplateEngine
        from frontend.viewmanager import ViewManagerGenerator
        from frontend.fast_generator import FastFieldGenerator
        from frontend.formulario_generator import FormularioFieldGenerator
        
        # Inicializar componentes
        templates_path = self.forms_path / "templates"
        self.template_engine = TemplateEngine(templates_path)
        self.viewmanager_generator = ViewManagerGenerator(self.root_path)
        self.fast_generator = FastFieldGenerator(templates_path, self.root_path)
        self.formulario_generator = FormularioFieldGenerator(templates_path, self.root_path)
    
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
    
    def create_module_directory(self, module, entity_name):
        """Crear directorio del módulo frontend con subcarpeta por entidad"""
        module_parts = module.split('.')
        frontend_module_path = self.root_path / "Frontend" / "Modules"
        for part in module_parts:
            frontend_module_path = frontend_module_path / part
        
        # Agregar subcarpeta por entidad (usar plural)
        entity_plural = f"{entity_name}s" if not entity_name.endswith('s') else entity_name
        frontend_module_path = frontend_module_path / entity_plural
        
        frontend_module_path.mkdir(parents=True, exist_ok=True)
        return frontend_module_path
    
    def generate_viewmanager(self, entity_name, module, module_path, config=None):
        """Generar ViewManager del frontend"""
        return self.viewmanager_generator.generate_viewmanager(entity_name, module, module_path, config)
    
    def generate_service_only(self, entity_name, module):
        """Generar solo el service frontend (FASE 3.1)"""
        try:
            print(f"🎨 Generando service frontend para entidad: {entity_name}")
            print(f"📁 Módulo: {module}")
            
            # 1. Crear directorio
            module_path = self.create_module_directory(module, entity_name)
            print(f"📁 Directorio creado: {module_path}")
            
            # 2. Generar Service
            if not self.generate_service(entity_name, module, module_path):
                return False
            
            return True
            
        except Exception as e:
            print(f"❌ ERROR en frontend generator: {e}")
            return False
    
    def generate_razor_component(self, entity_name, module, module_path, component_type):
        """Generar un componente Razor específico"""
        try:
            # Preparar variables básicas para el template
            variables = self.template_engine.prepare_entity_variables(entity_name, module)
            
            # Variables adicionales específicas para componentes
            module_path_url = module.lower().replace('.', '/')
            entity_plural = f"{entity_name}s"  # Simple pluralización
            entity_display_name = entity_name  # Para mostrar en UI
            
            variables.update({
                'MODULE_PATH': module_path_url,
                'ENTITY_LOWER': entity_name.lower(),
                'ENTITY_PLURAL': entity_plural,
                'ENTITY_DISPLAY_NAME': entity_display_name,
                'MODULE_NAMESPACE': module.replace('.', '.')
            })
            
            # Variables específicas por tipo de componente
            if component_type == 'fast':
                self._prepare_fast_variables(entity_name, variables)
            elif component_type == 'formulario':
                self._prepare_formulario_variables(entity_name, variables)
            
            # Renderizar templates
            razor_content = self.template_engine.render_template(f"frontend/components/{component_type}.razor.template", variables)
            cs_content = self.template_engine.render_template(f"frontend/components/{component_type}.razor.cs.template", variables)
            
            # Escribir archivos
            razor_file = module_path / f"{entity_name}{component_type.title()}.razor"
            cs_file = module_path / f"{entity_name}{component_type.title()}.razor.cs"
            
            razor_file.write_text(razor_content, encoding='utf-8')
            cs_file.write_text(cs_content, encoding='utf-8')
            
            print(f"✅ {entity_name}{component_type.title()}.razor generado")
            print(f"✅ {entity_name}{component_type.title()}.razor.cs generado")
            return True
            
        except Exception as e:
            print(f"❌ ERROR generando {component_type}: {e}")
            return False
    
    def _prepare_fast_variables(self, entity_name, variables):
        """Preparar variables específicas para componente Fast"""
        # Detectar campos de la entidad
        fields = self.viewmanager_generator.detect_entity_fields(entity_name)
        
        # Generar campos de formulario
        form_fields = []
        validation_rules = []
        field_validations = []
        
        for field in fields:
            form_fields.append(self.fast_generator.generate_form_field(field))
            validation_rules.append(self.fast_generator.generate_validation_rule(field))
            field_validations.append(self.fast_generator.generate_field_validation_check(field))
        
        # Recopilar dependencias de lookups
        lookup_deps = self.formulario_generator.collect_lookup_dependencies(fields)
        
        # Agregar a variables
        variables.update({
            'FORM_FIELDS': '\n                '.join(form_fields),
            'VALIDATION_RULES': '\n            '.join(validation_rules),
            'FIELD_VALIDATIONS': '\n\n            '.join(field_validations),
            'LOOKUP_SERVICE_INJECTIONS': lookup_deps['service_injections'],
            'LOOKUP_SEARCH_FIELDS': lookup_deps['search_fields'],
            'LOOKUP_FIELD_INITIALIZATIONS': lookup_deps['initializations']
        })
    
    def _prepare_formulario_variables(self, entity_name, variables):
        """Preparar variables específicas para componente Formulario"""
        # Detectar campos de la entidad
        fields = self.viewmanager_generator.detect_entity_fields(entity_name)
        
        # Campo principal para título
        primary_field = fields[0]['name'] if fields else 'Nombre'
        variables['PRIMARY_FIELD'] = primary_field
        
        # Generar campos de formulario
        form_fields = []
        validation_rules = []
        field_validations = []
        
        for field in fields:
            form_fields.append(self.formulario_generator.generate_form_field(field))
            validation_rules.append(self.fast_generator.generate_validation_rule(field))  # Reutilizar validaciones
            field_validations.append(self.fast_generator.generate_field_validation_check(field))  # Reutilizar validaciones
        
        # Recopilar dependencias de lookups
        lookup_deps = self.formulario_generator.collect_lookup_dependencies(fields)
        
        # Agregar a variables
        variables.update({
            'FORM_FIELDS_WITH_PERMISSIONS': '\n                                            '.join(form_fields),
            'VALIDATION_RULES': '\n            '.join(validation_rules),
            'FIELD_VALIDATIONS': '\n\n            '.join(field_validations),
            'LOOKUP_SERVICE_INJECTIONS': lookup_deps['service_injections'],
            'LOOKUP_SEARCH_FIELDS': lookup_deps['search_fields'],
            'LOOKUP_FIELD_INITIALIZATIONS': lookup_deps['initializations']
        })

    def generate_service_and_viewmanager(self, entity_name, module):
        """Generar Service + ViewManager frontend (FASE 3.2)"""
        try:
            print(f"🎨 Generando service + viewmanager frontend para entidad: {entity_name}")
            print(f"📁 Módulo: {module}")
            
            # 1. Crear directorio
            module_path = self.create_module_directory(module, entity_name)
            print(f"📁 Directorio creado: {module_path}")
            
            # 2. Generar Service
            if not self.generate_service(entity_name, module, module_path):
                return False
            
            # 3. Generar ViewManager
            if not self.generate_viewmanager(entity_name, module, module_path, None):
                return False
            
            return True
            
        except Exception as e:
            print(f"❌ ERROR en frontend generator: {e}")
            return False
    
    def generate_full_frontend(self, entity_name, module):
        """Generar Service + ViewManager + Componentes Razor (FASE 3.3)"""
        try:
            print(f"🎨 Generando frontend completo para entidad: {entity_name}")
            print(f"📁 Módulo: {module}")
            
            # 1. Crear directorio
            module_path = self.create_module_directory(module, entity_name)
            print(f"📁 Directorio creado: {module_path}")
            
            # 2. Generar Service
            if not self.generate_service(entity_name, module, module_path):
                return False
            
            # 3. Generar ViewManager
            if not self.generate_viewmanager(entity_name, module, module_path, None):
                return False
            
            # 4. Generar componente List
            if not self.generate_razor_component(entity_name, module, module_path, "list"):
                return False
            
            return True
            
        except Exception as e:
            print(f"❌ ERROR en frontend generator: {e}")
            return False
    
    def generate_frontend_with_fast(self, entity_name, module):
        """Generar Service + ViewManager + List + Fast (FASE 3.4)"""
        try:
            print(f"🎨 Generando frontend con componente Fast para entidad: {entity_name}")
            print(f"📁 Módulo: {module}")
            
            # 1. Crear directorio
            module_path = self.create_module_directory(module, entity_name)
            print(f"📁 Directorio creado: {module_path}")
            
            # 2. Generar Service
            if not self.generate_service(entity_name, module, module_path):
                return False
            
            # 3. Generar ViewManager
            if not self.generate_viewmanager(entity_name, module, module_path, None):
                return False
            
            # 4. Generar componente List
            if not self.generate_razor_component(entity_name, module, module_path, "list"):
                return False
            
            # 5. Generar componente Fast
            if not self.generate_razor_component(entity_name, module, module_path, "fast"):
                return False
            
            return True
            
        except Exception as e:
            print(f"❌ ERROR en frontend generator: {e}")
            return False
    
    def generate_frontend_with_formulario(self, entity_name, module, config=None):
        """Generar Service + ViewManager + List + Fast + Formulario (FASE 3.5)"""
        try:
            print(f"🎨 Generando frontend completo con Formulario para entidad: {entity_name}")
            print(f"📁 Módulo: {module}")
            
            # 1. Crear directorio
            module_path = self.create_module_directory(module, entity_name)
            print(f"📁 Directorio creado: {module_path}")
            
            # 2. Generar Service
            if not self.generate_service(entity_name, module, module_path):
                return False
            
            # 3. Generar ViewManager
            if not self.generate_viewmanager(entity_name, module, module_path, config):
                return False
            
            # 4. Generar componente List
            if not self.generate_razor_component(entity_name, module, module_path, "list"):
                return False
            
            # 5. Generar componente Fast
            if not self.generate_razor_component(entity_name, module, module_path, "fast"):
                return False
            
            # 6. Generar componente Formulario
            if not self.generate_razor_component(entity_name, module, module_path, "formulario"):
                return False
            
            return True
            
        except Exception as e:
            print(f"❌ ERROR en frontend generator: {e}")
            return False