#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
📝 Formulario Component Generator
Genera campos de formulario para componentes Formulario (más completo que Fast)
"""

import sys
from pathlib import Path

class FormularioFieldGenerator:
    def __init__(self, templates_path=None, root_path=None):
        if templates_path:
            self.templates_path = Path(templates_path)
            # Importar template engine y lookup resolver
            sys.path.append(str(self.templates_path.parent.parent))
            from shared.template_engine import TemplateEngine
            from shared.lookup_resolver import LookupResolver
            
            self.template_engine = TemplateEngine(self.templates_path)
            self.lookup_resolver = LookupResolver(root_path or self.templates_path.parent.parent.parent)
        else:
            self.template_engine = None
            self.lookup_resolver = None
        
        # Configuración de lookups del usuario (se setea externamente)
        self.user_lookups_config = None
    
    def generate_form_field(self, field):
        """Generar campo de formulario para Formulario (con Style width 100%)"""
        field_name = field['name']
        field_type = field['type']
        
        # PRIORIDAD 1: Usar configuración de lookups del usuario
        if self.user_lookups_config:
            # Buscar el lookup por campo, normalizando nombres
            user_lookup = self._find_user_lookup_for_field(field_name)
            if user_lookup:
                print(f"✅ Usando lookup configurado por usuario: {field_name} -> {user_lookup.target_table}")
                lookup_config = self._convert_user_lookup_to_config(user_lookup, field_name)
                return self._generate_formulario_lookup_input(lookup_config)
        
        # PRIORIDAD 2: Detectar si es un campo FK (Lookup) automáticamente
        if field_type == 'Guid' and self.lookup_resolver:
            lookup_config = self.lookup_resolver.resolve_lookup_config(field_name)
            if lookup_config:
                print(f"✅ Detectado Lookup automático en Formulario: {field_name} -> {lookup_config['entity_name']}")
                return self._generate_formulario_lookup_input(lookup_config)
        
        # Campos normales (no FK)
        field_display = self._get_display_name(field_name)
        field_placeholder = self._get_placeholder(field_name, field_display)
        
        # Generar según tipo usando lógica específica para formulario
        if field_type == 'string':
            if field_name.lower() == 'descripcion':
                return self._generate_formulario_textarea(field_name, field_display, field_placeholder)
            else:
                return self._generate_formulario_textbox(field_name, field_display, field_placeholder)
        elif field_type in ['int', 'decimal']:
            return self._generate_formulario_numeric(field_name, field_display, field_type)
        elif field_type == 'DateTime':
            return self._generate_formulario_datetime(field_name, field_display)
        elif field_type == 'bool':
            return self._generate_formulario_switch(field_name, field_display)
        else:
            return self._generate_formulario_textbox(field_name, field_display, field_placeholder)  # Fallback
    
    def _generate_formulario_textbox(self, field_name, field_display, field_placeholder):
        """Generar TextBox para formulario con width: 100%"""
        return f'''<ValidatedInput FieldName="{field_name}" Value="@entity.{field_name}">
                                                <RadzenFormField Text="{field_display}" Style="width: 100%">
                                                    <RadzenTextBox @bind-Value="@entity.{field_name}" 
                                                                   Placeholder="{field_placeholder}" />
                                                </RadzenFormField>
                                            </ValidatedInput>'''
    
    def _generate_formulario_textarea(self, field_name, field_display, field_placeholder):
        """Generar TextArea para formulario con width: 100%"""
        return f'''<ValidatedInput FieldName="{field_name}" Value="@entity.{field_name}">
                                                <RadzenFormField Text="{field_display} (Opcional)" Style="width: 100%">
                                                    <RadzenTextArea @bind-Value="@entity.{field_name}" 
                                                                    Placeholder="{field_placeholder}" 
                                                                    Rows="3" />
                                                </RadzenFormField>
                                            </ValidatedInput>'''
    
    def _generate_formulario_numeric(self, field_name, field_display, field_type):
        """Generar Numeric para formulario"""
        if field_type == 'int':
            placeholder = '0'
            format_attr = ''
        else:  # decimal
            placeholder = '0.00'
            format_attr = 'Format="F2"'
        
        return f'''<ValidatedInput FieldName="{field_name}" Value="@entity.{field_name}">
                                                <RadzenFormField Text="{field_display}" Style="width: 100%">
                                                    <RadzenNumeric @bind-Value="entity.{field_name}" 
                                                                   Placeholder="{placeholder}"
                                                                   {format_attr}
                                                                   ShowUpDown="false" />
                                                </RadzenFormField>
                                            </ValidatedInput>'''
    
    def _generate_formulario_datetime(self, field_name, field_display):
        """Generar DatePicker para formulario"""
        return f'''<ValidatedInput FieldName="{field_name}" Value="@entity.{field_name}">
                                                <RadzenFormField Text="{field_display}" Style="width: 100%">
                                                    <RadzenDatePicker @bind-Value="entity.{field_name}" 
                                                                      ShowTime="true"
                                                                      DateFormat="dd/MM/yyyy HH:mm" />
                                                </RadzenFormField>
                                            </ValidatedInput>'''
    
    def _generate_formulario_lookup_input(self, lookup_config):
        """Generar Lookup para formulario con width: 100%"""
        field_name = lookup_config['field_name']
        field_display = self._get_display_name(field_name)
        
        return f'''<ValidatedInput FieldName="{field_name}" Value="@entity.{field_name}">
                                                <RadzenFormField Text="{field_display}" Style="width: 100%">
                                                    <Lookup TEntity="{lookup_config['entity_name']}" 
                                                           TValue="{lookup_config['value_type']}" 
                                                           @bind-Value="entity.{field_name}"
                                                           Service="{lookup_config['service_name']}"
                                                           FastCreateComponentType="typeof({lookup_config['entity_name']}Fast)"
                                                           DisplayProperty="{lookup_config['display_property']}"
                                                           EntityDisplayName="{lookup_config['entity_display']}"
                                                           SearchableFields="@{lookup_config['entity_name'].lower()}SearchFields"
                                                           {('EnableCache="true"' if lookup_config['cache_enabled'] else '')} />
                                                </RadzenFormField>
                                            </ValidatedInput>'''
    
    def _generate_formulario_switch(self, field_name, field_display):
        """Generar RadzenSwitch para campos booleanos"""
        return f'''<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
                                                <RadzenLabel Text="{field_display}" Component="{field_name}" />
                                                <RadzenSwitch @bind-Value="entity.{field_name}" Name="{field_name}" />
                                            </RadzenStack>'''
    
    def _get_display_name(self, field_name):
        """Obtener nombre para mostrar"""
        display_map = {
            'Nombre': 'Nombre',
            'Descripcion': 'Descripción',
            'CodigoInterno': 'Código Interno',
            'Precio': 'Precio',
            'Stock': 'Stock',
            'Cantidad': 'Cantidad',
            'FechaVencimiento': 'Fecha de Vencimiento'
        }
        return display_map.get(field_name, field_name)
    
    def _get_placeholder(self, field_name, display_name):
        """Generar placeholder apropiado"""
        if field_name.lower() == 'descripcion':
            return f"Ingrese {display_name.lower()} (opcional)"
        else:
            return f"Ingrese {display_name.lower()}"
    
    def _find_user_lookup_for_field(self, field_name):
        """Buscar configuración de lookup para un campo, normalizando nombres"""
        if not self.user_lookups_config:
            return None
        
        # Normalizar el nombre del campo para comparar
        field_normalized = self._normalize_field_name(field_name)
        
        for lookup_key, lookup_config in self.user_lookups_config.items():
            lookup_key_normalized = self._normalize_field_name(lookup_key)
            if lookup_key_normalized == field_normalized:
                return lookup_config
        
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
    
    def _convert_user_lookup_to_config(self, user_lookup, actual_field_name):
        """Convertir configuración de lookup del usuario al formato esperado"""
        # Convertir nombre de tabla a nombre de entidad (capitalize)
        # Para auto-referencias como "areas" -> usar plural "Areas" 
        table_name = user_lookup.target_table.lower()
        
        # Casos especiales para plurales comunes
        if table_name.endswith('s'):
            entity_name = table_name.capitalize()  # "areas" -> "Areas"
        else:
            entity_name = table_name.capitalize()  # "categoria" -> "Categoria"
        
        return {
            'field_name': actual_field_name,  # Usar el nombre real del campo
            'entity_name': entity_name,
            'service_name': f"{entity_name}Service",
            'display_property': user_lookup.display_field,
            'value_type': 'Guid?',  # Asumir Guid para FKs
            'entity_display': entity_name,
            'cache_enabled': user_lookup.cache
        }
    
    def collect_lookup_dependencies(self, fields, main_entity_name=None):
        """Recopilar dependencias de lookups para inyección de servicios"""
        lookup_services = []
        lookup_search_fields = []
        lookup_initializations = []
        added_services = set()  # Para evitar duplicados
        
        # IMPORTANTE: Excluir el servicio principal que ya está definido en el template
        if main_entity_name:
            main_service_name = f"{main_entity_name}Service"
            added_services.add(main_service_name)  # Marcar como ya agregado
            print(f"🔇 Servicio principal omitido (ya existe en template): {main_service_name}")
        
        if not self.lookup_resolver:
            return {
                'service_injections': '',
                'search_fields': '',
                'initializations': ''
            }
        
        for field in fields:
            field_name = field['name']
            lookup_config = None
            
            # PRIORIDAD 1: Usar configuración de lookups del usuario
            if self.user_lookups_config:
                user_lookup = self._find_user_lookup_for_field(field_name)
                if user_lookup:
                    lookup_config = self._convert_user_lookup_to_config(user_lookup, field_name)
            # PRIORIDAD 2: Usar resolver (incluye FK del usuario + detección automática)
            if not lookup_config and field['type'] == 'Guid' and self.lookup_resolver:
                lookup_config = self.lookup_resolver.resolve_lookup_config(field_name)
            
            if lookup_config:
                entity_name = lookup_config['entity_name']
                service_name = lookup_config['service_name']
                
                # VALIDAR SI YA EXISTE EL SERVICIO ANTES DE AGREGARLO
                if service_name not in added_services:
                    lookup_services.append(f"[Inject] private {service_name} {service_name} {{ get; set; }} = null!;")
                    added_services.add(service_name)
                    print(f"✅ Servicio agregado: {service_name}")
                else:
                    print(f"⚠️ Servicio duplicado omitido: {service_name}")
                
                # Campo de búsqueda (estos pueden repetirse sin problema)
                search_field_name = f"{entity_name.lower()}SearchFields"
                # IMPORTANTE: Usar namespace completo para evitar conflictos
                full_entity_name = f"Shared.Models.Entities.{entity_name}"
                search_field_definition = f"private Expression<Func<{full_entity_name}, object>>[] {search_field_name} = new Expression<Func<{full_entity_name}, object>>[] {{ x => x.Nombre }};"
                
                # También evitar duplicados en search fields
                if search_field_definition not in lookup_search_fields:
                    lookup_search_fields.append(search_field_definition)
                
                # No necesitamos inicialización especial por ahora
        
        return {
            'service_injections': '\n    '.join(lookup_services),
            'search_fields': '\n    '.join(lookup_search_fields),
            'initializations': '\n        '.join(lookup_initializations) if lookup_initializations else '// No lookups to initialize'
        }