#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
⚡ Fast Component Generator
Genera campos de formulario para componentes Fast
"""

import sys
from pathlib import Path

class FastFieldGenerator:
    def __init__(self, templates_path=None, root_path=None):
        if templates_path:
            self.templates_path = Path(templates_path)
            # Importar template engine
            sys.path.append(str(self.templates_path.parent.parent))
            from shared.template_engine import TemplateEngine
            from shared.lookup_resolver import LookupResolver
            
            self.template_engine = TemplateEngine(self.templates_path)
            self.lookup_resolver = LookupResolver(root_path or self.templates_path.parent.parent.parent)
        else:
            self.template_engine = None
            self.lookup_resolver = None
    
    def generate_form_field(self, field):
        """Generar campo de formulario basado en el tipo usando templates"""
        field_name = field['name']
        field_type = field['type']
        
        # IMPORTANTE: Detectar si es un campo FK (Lookup)
        if field_type == 'Guid' and self.lookup_resolver:
            lookup_config = self.lookup_resolver.resolve_lookup_config(field_name)
            if lookup_config:
                print(f"✅ Detectado Lookup: {field_name} -> {lookup_config['entity_name']}")
                return self.lookup_resolver.generate_lookup_input(lookup_config, self.template_engine)
        
        # Campos normales (no FK)
        field_display = self._get_display_name(field_name)
        field_placeholder = self._get_placeholder(field_name, field_display)
        
        base_variables = {
            'FIELD_NAME': field_name,
            'FIELD_DISPLAY': field_display,
            'FIELD_PLACEHOLDER': field_placeholder
        }
        
        # Generar según tipo usando templates
        if self.template_engine:
            if field_type == 'string':
                if field_name.lower() == 'descripcion':
                    return self._render_input_template('textarea_input', base_variables)
                else:
                    return self._render_input_template('textbox_input', base_variables)
            elif field_type in ['int', 'decimal']:
                return self._render_numeric_template(field_type, base_variables)
            elif field_type == 'DateTime':
                return self._render_datetime_template(base_variables)
            else:
                return self._render_input_template('textbox_input', base_variables)  # Fallback
        else:
            # Fallback a código hardcodeado si no hay templates
            return self._generate_fallback_field(field_name, field_type)
    
    def _render_input_template(self, template_name, variables):
        """Renderizar template de input específico"""
        try:
            return self.template_engine.render_template(f"frontend/inputs/{template_name}.template", variables)
        except Exception as e:
            print(f"⚠️ Error renderizando {template_name}: {e}")
            return f"<!-- Error: {template_name} -->"
    
    def _render_numeric_template(self, field_type, variables):
        """Renderizar template numérico con formato específico"""
        if field_type == 'int':
            variables['NUMERIC_FORMAT'] = ''
            variables['FIELD_PLACEHOLDER'] = '0'
        else:  # decimal
            variables['NUMERIC_FORMAT'] = 'Format="F2"'
            variables['FIELD_PLACEHOLDER'] = '0.00'
        
        return self._render_input_template('numeric_input', variables)
    
    def _render_datetime_template(self, variables):
        """Renderizar template de fecha/hora"""
        variables.update({
            'SHOW_TIME': 'true',
            'DATE_FORMAT': 'dd/MM/yyyy HH:mm'
        })
        return self._render_input_template('datetime_input', variables)
    
    def _get_placeholder(self, field_name, display_name):
        """Generar placeholder apropiado"""
        if field_name.lower() == 'descripcion':
            return f"Ingrese {display_name.lower()} (opcional)"
        else:
            return f"Ingrese {display_name.lower()}"
    
    def _generate_fallback_field(self, field_name, field_type):
        """Generar campo usando código hardcodeado (fallback)"""
        if field_type == 'string':
            if field_name.lower() == 'descripcion':
                return self._generate_textarea_field(field_name)
            else:
                return self._generate_textbox_field(field_name)
        elif field_type in ['int', 'decimal']:
            return self._generate_numeric_field(field_name, field_type)
        elif field_type == 'DateTime':
            return self._generate_datetime_field(field_name)
        else:
            return self._generate_textbox_field(field_name)
    
    def _generate_textbox_field(self, field_name):
        """Generar campo TextBox"""
        display_name = self._get_display_name(field_name)
        placeholder = f"Ingrese {display_name.lower()}"
        
        return f'''<ValidatedInput FieldName="{field_name}" Value="@entity.{field_name}">
                    <RadzenFormField Text="{display_name}">
                        <RadzenTextBox @oninput="@(v => entity.{field_name} = v.Value?.ToString())" 
                                       Value="@entity.{field_name}"
                                       Placeholder="{placeholder}" />
                    </RadzenFormField>
                </ValidatedInput>'''
    
    def _generate_textarea_field(self, field_name):
        """Generar campo TextArea"""
        display_name = self._get_display_name(field_name)
        placeholder = f"Ingrese {display_name.lower()} (opcional)"
        
        return f'''<ValidatedInput FieldName="{field_name}" Value="@entity.{field_name}">
                    <RadzenFormField Text="{display_name}">
                        <RadzenTextArea @oninput="@(v => entity.{field_name} = v.Value?.ToString())" 
                                        Value="@entity.{field_name}"
                                        Placeholder="{placeholder}" 
                                        Rows="3" />
                    </RadzenFormField>
                </ValidatedInput>'''
    
    def _generate_numeric_field(self, field_name, field_type):
        """Generar campo numérico"""
        display_name = self._get_display_name(field_name)
        
        if field_type == 'int':
            return f'''<ValidatedInput FieldName="{field_name}" Value="@entity.{field_name}">
                        <RadzenFormField Text="{display_name}">
                            <RadzenNumeric @bind-Value="entity.{field_name}" 
                                           Placeholder="0"
                                           ShowUpDown="false" />
                        </RadzenFormField>
                    </ValidatedInput>'''
        else:  # decimal
            return f'''<ValidatedInput FieldName="{field_name}" Value="@entity.{field_name}">
                        <RadzenFormField Text="{display_name}">
                            <RadzenNumeric @bind-Value="entity.{field_name}" 
                                           Placeholder="0.00"
                                           Format="F2"
                                           ShowUpDown="false" />
                        </RadzenFormField>
                    </ValidatedInput>'''
    
    def _generate_datetime_field(self, field_name):
        """Generar campo DateTime"""
        display_name = self._get_display_name(field_name)
        
        return f'''<ValidatedInput FieldName="{field_name}" Value="@entity.{field_name}">
                    <RadzenFormField Text="{display_name}">
                        <RadzenDatePicker @bind-Value="entity.{field_name}" 
                                          ShowTime="true"
                                          DateFormat="dd/MM/yyyy HH:mm" />
                    </RadzenFormField>
                </ValidatedInput>'''
    
    def _generate_checkbox_field(self, field_name):
        """Generar campo CheckBox"""
        display_name = self._get_display_name(field_name)
        
        return f'''<RadzenFormField Text="{display_name}">
                    <RadzenCheckBox @bind-Value="entity.{field_name}" />
                </RadzenFormField>'''
    
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
    
    def generate_validation_rule(self, field):
        """Generar regla de validación"""
        field_name = field['name']
        field_type = field['type']
        
        # Reglas básicas por tipo
        if field_type == 'string':
            if field_name.lower() == 'nombre':
                return f'''.Field("{field_name}", field => field
                .Required("El {field_name.lower()} es obligatorio")
                .Length(3, 100, "El {field_name.lower()} debe tener entre 3 y 100 caracteres"))'''
            elif field_name.lower() == 'descripcion':
                return f'''.Field("{field_name}", field => field
                .MaxLength(500, "La descripción no puede exceder 500 caracteres"))'''
            elif 'codigo' in field_name.lower():
                return f'''.Field("{field_name}", field => field
                .Required("El código es obligatorio")
                .Length(2, 50, "El código debe tener entre 2 y 50 caracteres"))'''
            else:
                return f'''.Field("{field_name}", field => field
                .MaxLength(255, "El campo no puede exceder 255 caracteres"))'''
        
        elif field_type in ['int', 'decimal']:
            return f'''.Field("{field_name}", field => field
                .Range(0, 999999, "El valor debe ser mayor a 0"))'''
        
        else:
            return f'''.Field("{field_name}", field => field
                .Required("El campo es obligatorio"))'''
    
    def generate_field_validation_check(self, field):
        """Generar validación manual en código C#"""
        field_name = field['name']
        field_type = field['type']
        
        if field_type == 'string':
            if field_name.lower() == 'nombre':
                return f'''// Validación {field_name}
            if (string.IsNullOrWhiteSpace(entity.{field_name}))
            {{
                NotificationService.Notify(new NotificationMessage
                {{
                    Severity = NotificationSeverity.Warning,
                    Summary = "Validación",
                    Detail = "El {field_name.lower()} es obligatorio",
                    Duration = 4000
                }});
                return;
            }}
            
            if (entity.{field_name}.Length < 3 || entity.{field_name}.Length > 100)
            {{
                NotificationService.Notify(new NotificationMessage
                {{
                    Severity = NotificationSeverity.Warning,
                    Summary = "Validación",
                    Detail = "El {field_name.lower()} debe tener entre 3 y 100 caracteres",
                    Duration = 4000
                }});
                return;
            }}'''
            elif field_name.lower() == 'descripcion':
                return f'''// Validación {field_name}
            if (!string.IsNullOrEmpty(entity.{field_name}) && entity.{field_name}.Length > 500)
            {{
                NotificationService.Notify(new NotificationMessage
                {{
                    Severity = NotificationSeverity.Warning,
                    Summary = "Validación",
                    Detail = "La descripción no puede exceder 500 caracteres",
                    Duration = 4000
                }});
                return;
            }}'''
            elif 'codigo' in field_name.lower():
                return f'''// Validación {field_name}
            if (string.IsNullOrWhiteSpace(entity.{field_name}))
            {{
                NotificationService.Notify(new NotificationMessage
                {{
                    Severity = NotificationSeverity.Warning,
                    Summary = "Validación",
                    Detail = "El código es obligatorio",
                    Duration = 4000
                }});
                return;
            }}
            
            if (entity.{field_name}.Length < 2 || entity.{field_name}.Length > 50)
            {{
                NotificationService.Notify(new NotificationMessage
                {{
                    Severity = NotificationSeverity.Warning,
                    Summary = "Validación",
                    Detail = "El código debe tener entre 2 y 50 caracteres",
                    Duration = 4000
                }});
                return;
            }}'''
        
        return f"// {field_name} - Sin validación específica"