#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
🔍 Lookup Resolver
Resuelve automáticamente campos FK y sus servicios relacionados
"""

import re
from pathlib import Path

class LookupResolver:
    def __init__(self, root_path, fk_config=None):
        self.root_path = Path(root_path)
        
        # Patrones para detectar FK
        self.fk_patterns = [
            r'(.+)Id$',      # CategoriaId -> Categoria  
            r'(.+)_id$',     # categoria_id -> Categoria
        ]
        
        # Campos base del sistema a ignorar
        self.system_fields = [
            'Id', 'OrganizationId', 'CreadorId', 'ModificadorId'
        ]
        
        # Configuración de FK del usuario (puede venir del constructor o setearse externamente)
        self.user_fks_config = fk_config
    
    def detect_fk_fields(self, fields):
        """Detectar campos que son FK"""
        fk_fields = []
        
        for field in fields:
            field_name = field['name']
            field_type = field['type']
            
            # Solo procesar campos Guid que no sean del sistema
            if field_type == 'Guid' and field_name not in self.system_fields:
                fk_info = self._resolve_fk_info(field_name)
                if fk_info:
                    fk_fields.append({
                        'field': field,
                        'fk_info': fk_info
                    })
        
        return fk_fields
    
    def _resolve_fk_info(self, field_name):
        """Resolver información de FK desde el nombre del campo"""
        # PRIORIDAD 1: Usar configuración de FK del usuario
        if self.user_fks_config:
            # Normalizar nombre del campo para comparar
            field_normalized = self._normalize_field_name(field_name)
            
            for fk_config in self.user_fks_config:
                fk_field_normalized = self._normalize_field_name(fk_config.field)
                if fk_field_normalized == field_normalized:
                    # Convertir nombre de tabla a entidad (capitalize y manejar plurales)
                    table_name = fk_config.ref_table.lower()
                    if table_name.endswith('s'):
                        entity_name = table_name.capitalize()  # "areas" -> "Areas"
                    else:
                        entity_name = table_name.capitalize()  # "categoria" -> "Categoria"
                    
                    print(f"✅ Usando FK configurado por usuario: {field_name} -> {entity_name} (tabla: {fk_config.ref_table})")
                    
                    return {
                        'fk_field': field_name,
                        'entity_name': entity_name,
                        'service_name': f"{entity_name}Service",
                        'display_property': 'Nombre',
                        'search_fields': ['Nombre'],
                        'value_type': 'Guid?'
                    }
        
        # PRIORIDAD 2: Detección automática por patrones
        for pattern in self.fk_patterns:
            match = re.match(pattern, field_name)
            if match:
                entity_base_name = match.group(1)
                
                # Convertir a PascalCase si viene snake_case
                entity_name = self._to_pascal_case(entity_base_name)
                print(f"✅ FK detectado por patrón: {field_name} -> {entity_name}")
                
                return {
                    'fk_field': field_name,
                    'entity_name': entity_name,
                    'service_name': f"{entity_name}Service",
                    'display_property': 'Nombre',  # Asumimos que siempre es Nombre
                    'search_fields': ['Nombre'],   # Campo de búsqueda por defecto
                    'value_type': 'Guid?'
                }
        
        return None
    
    def _normalize_field_name(self, field_name):
        """Normalizar nombre de campo para comparación (snake_case)"""
        # Convertir "AreaId" -> "area_id" y "area_id" -> "area_id"
        import re
        # Si ya está en snake_case, dejarlo
        if '_' in field_name and field_name.islower():
            return field_name
        
        # Si está en PascalCase, convertir a snake_case
        # "AreaId" -> "area_id"
        s1 = re.sub('(.)([A-Z][a-z]+)', r'\1_\2', field_name)
        return re.sub('([a-z0-9])([A-Z])', r'\1_\2', s1).lower()
    
    def _to_pascal_case(self, snake_str):
        """Convertir snake_case a PascalCase"""
        components = snake_str.split('_')
        return ''.join(word.capitalize() for word in components)
    
    def find_service_for_entity(self, entity_name):
        """Buscar si existe el servicio para la entidad"""
        # Buscar en Frontend/Modules
        frontend_modules = self.root_path / "Frontend" / "Modules"
        
        if not frontend_modules.exists():
            return None
        
        # Buscar recursivamente
        for module_dir in frontend_modules.rglob("*"):
            if module_dir.is_dir():
                service_file = module_dir / f"{entity_name}Service.cs"
                if service_file.exists():
                    return {
                        'service_file': service_file,
                        'module_path': module_dir.relative_to(frontend_modules),
                        'namespace': self._get_namespace_from_path(module_dir.relative_to(frontend_modules))
                    }
        
        return None
    
    def _get_namespace_from_path(self, relative_path):
        """Convertir ruta de módulo a namespace"""
        parts = relative_path.parts
        return '.'.join(parts)
    
    def entity_exists(self, entity_name):
        """Verificar si existe la entidad en Shared.Models"""
        entity_file = self.root_path / "Shared.Models" / "Entities" / f"{entity_name}.cs"
        return entity_file.exists()
    
    def resolve_lookup_config(self, field_name):
        """Resolver configuración completa de lookup"""
        fk_info = self._resolve_fk_info(field_name)
        
        if not fk_info:
            return None
        
        entity_name = fk_info['entity_name']
        
        # Verificar que existe la entidad
        if not self.entity_exists(entity_name):
            print(f"⚠️ Entidad {entity_name} no encontrada para FK {field_name}")
            return None
        
        # Buscar el servicio
        service_info = self.find_service_for_entity(entity_name)
        if not service_info:
            print(f"⚠️ Servicio {entity_name}Service no encontrado para FK {field_name}")
            return None
        
        # Retornar configuración completa
        return {
            'field_name': field_name,
            'entity_name': entity_name,
            'service_name': fk_info['service_name'],
            'service_namespace': f"Frontend.Modules.{service_info['namespace']}",
            'display_property': fk_info['display_property'],
            'search_fields': fk_info['search_fields'],
            'value_type': fk_info['value_type'],
            'entity_display': entity_name,  # Para EntityDisplayName
            'cache_enabled': True,  # Por defecto habilitamos cache
            'service_injection': f"[Inject] private {fk_info['service_name']} {fk_info['service_name']} {{ get; set; }} = null!;"
        }
    
    def generate_lookup_input(self, lookup_config, templates_engine=None):
        """Generar input de lookup usando template"""
        if not lookup_config:
            return "<!-- No lookup config -->"
        
        variables = {
            'FIELD_NAME': lookup_config['field_name'],
            'FIELD_DISPLAY': self._get_display_name(lookup_config['field_name']),
            'LOOKUP_ENTITY': lookup_config['entity_name'],
            'LOOKUP_VALUE_TYPE': lookup_config['value_type'],
            'LOOKUP_SERVICE': lookup_config['service_name'],
            'LOOKUP_DISPLAY_PROPERTY': lookup_config['display_property'],
            'LOOKUP_ENTITY_DISPLAY': lookup_config['entity_display'],
            'LOOKUP_SEARCH_FIELDS': f'SearchableFields="@{lookup_config["entity_name"].lower()}SearchFields"',
            'LOOKUP_CACHE_CONFIG': 'EnableCache="true"' if lookup_config['cache_enabled'] else ''
        }
        
        if templates_engine:
            return templates_engine.render_template("frontend/inputs/lookup_input.template", variables)
        else:
            # Fallback manual
            return f'''<ValidatedInput FieldName="{variables['FIELD_NAME']}" Value="@entity.{variables['FIELD_NAME']}">
    <RadzenFormField Text="{variables['FIELD_DISPLAY']}">
        <Lookup TEntity="{variables['LOOKUP_ENTITY']}" 
               TValue="{variables['LOOKUP_VALUE_TYPE']}" 
               @bind-Value="entity.{variables['FIELD_NAME']}"
               Service="{variables['LOOKUP_SERVICE']}"
               DisplayProperty="{variables['LOOKUP_DISPLAY_PROPERTY']}"
               EntityDisplayName="{variables['LOOKUP_ENTITY_DISPLAY']}"
               {variables['LOOKUP_SEARCH_FIELDS']}
               {variables['LOOKUP_CACHE_CONFIG']} />
    </RadzenFormField>
</ValidatedInput>'''
    
    def _get_display_name(self, field_name):
        """Obtener nombre para mostrar del campo FK"""
        # Remover sufijos Id/_id
        clean_name = re.sub(r'(Id|_id)$', '', field_name)
        
        # Convertir a título amigable
        display_map = {
            'categoria': 'Categoría',
            'producto': 'Producto',
            'cliente': 'Cliente',
            'proveedor': 'Proveedor',
            'marca': 'Marca',
            'usuario': 'Usuario'
        }
        
        return display_map.get(clean_name.lower(), clean_name.capitalize())