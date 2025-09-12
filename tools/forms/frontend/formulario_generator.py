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
    
    def generate_form_field(self, field):
        """Generar campo de formulario para Formulario (con Style width 100%)"""
        field_name = field['name']
        field_type = field['type']
        
        # IMPORTANTE: Detectar si es un campo FK (Lookup)
        if field_type == 'Guid' and self.lookup_resolver:
            lookup_config = self.lookup_resolver.resolve_lookup_config(field_name)
            if lookup_config:
                print(f"✅ Detectado Lookup en Formulario: {field_name} -> {lookup_config['entity_name']}")
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
                                                    <RadzenTextBox @oninput="@(v => entity.{field_name} = v.Value?.ToString())" 
                                                                   Value="@entity.{field_name}"
                                                                   Placeholder="{field_placeholder}" />
                                                </RadzenFormField>
                                            </ValidatedInput>'''
    
    def _generate_formulario_textarea(self, field_name, field_display, field_placeholder):
        """Generar TextArea para formulario con width: 100%"""
        return f'''<ValidatedInput FieldName="{field_name}" Value="@entity.{field_name}">
                                                <RadzenFormField Text="{field_display} (Opcional)" Style="width: 100%">
                                                    <RadzenTextArea @oninput="@(v => entity.{field_name} = v.Value?.ToString())" 
                                                                    Value="@entity.{field_name}"
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
    
    def collect_lookup_dependencies(self, fields):
        """Recopilar dependencias de lookups para inyección de servicios"""
        lookup_services = []
        lookup_search_fields = []
        lookup_initializations = []
        
        if not self.lookup_resolver:
            return {
                'service_injections': '',
                'search_fields': '',
                'initializations': ''
            }
        
        for field in fields:
            if field['type'] == 'Guid':
                lookup_config = self.lookup_resolver.resolve_lookup_config(field['name'])
                if lookup_config:
                    entity_name = lookup_config['entity_name']
                    service_name = lookup_config['service_name']
                    
                    # Inyección de servicio
                    lookup_services.append(f"[Inject] private {service_name} {service_name} {{ get; set; }} = null!;")
                    
                    # Campo de búsqueda
                    search_field_name = f"{entity_name.lower()}SearchFields"
                    lookup_search_fields.append(f"private Expression<Func<{entity_name}, object>>[] {search_field_name} = new Expression<Func<{entity_name}, object>>[] {{ x => x.Nombre }};")
                    
                    # No necesitamos inicialización especial por ahora
        
        return {
            'service_injections': '\n    '.join(lookup_services),
            'search_fields': '\n    '.join(lookup_search_fields),
            'initializations': '\n        '.join(lookup_initializations) if lookup_initializations else '// No lookups to initialize'
        }