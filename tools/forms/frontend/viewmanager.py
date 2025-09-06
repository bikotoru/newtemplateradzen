#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
👁️ ViewManager Generator
Genera ViewManager basado en campos de entidad y patrones conocidos
"""

import sys
from pathlib import Path

class ViewManagerGenerator:
    def __init__(self, root_path):
        self.root_path = Path(root_path)
        self.forms_path = self.root_path / "tools" / "forms"
        
        # Importar template engine
        sys.path.append(str(self.forms_path))
        from shared.template_engine import TemplateEngine
        
        # Inicializar motor de templates
        templates_path = self.forms_path / "templates"
        self.template_engine = TemplateEngine(templates_path)
    
    def detect_entity_fields(self, entity_name):
        """Detectar campos de la entidad desde el modelo generado"""
        try:
            entity_file = self.root_path / "Shared.Models" / "Entities" / f"{entity_name}.cs"
            
            if not entity_file.exists():
                print(f"⚠️ Modelo {entity_name}.cs no encontrado, usando campos por defecto")
                return self.get_default_fields(entity_name)
            
            content = entity_file.read_text(encoding='utf-8')
            fields = []
            
            lines = content.split('\n')
            for line in lines:
                line = line.strip()
                # Buscar propiedades públicas NO virtuales (ej: public string? Nombre { get; set; })
                if line.startswith('public ') and '{ get; set; }' in line and 'virtual' not in line:
                    # Extraer nombre de propiedad
                    parts = line.split()
                    if len(parts) >= 3:
                        prop_name = parts[2].replace('?', '').replace(' ', '')
                        type_name = parts[1].replace('?', '')
                        
                        # Filtrar propiedades base (heredadas) y navegación
                        base_props = ['Id', 'OrganizationId', 'FechaCreacion', 'FechaModificacion', 
                                    'CreadorId', 'ModificadorId', 'Active']
                        
                        if prop_name not in base_props:
                            fields.append({
                                'name': prop_name,
                                'type': type_name,
                                'is_nullable': '?' in parts[1]
                            })
            
            if not fields:
                return self.get_default_fields(entity_name)
            
            print(f"✅ Campos detectados de {entity_name}: {[f['name'] for f in fields]}")
            return fields
            
        except Exception as e:
            print(f"⚠️ Error detectando campos de {entity_name}: {e}")
            return self.get_default_fields(entity_name)
    
    def get_default_fields(self, entity_name):
        """Campos por defecto si no se puede detectar"""
        return [
            {'name': 'Nombre', 'type': 'string', 'is_nullable': True},
            {'name': 'Descripcion', 'type': 'string', 'is_nullable': True}
        ]
    
    def generate_column_config(self, field, order):
        """Generar configuración de columna para un campo"""
        field_name = field['name']
        field_type = field['type']
        
        # Determinar ancho por tipo de campo
        width_map = {
            'string': '200px' if field_name.lower() != 'descripcion' else '300px',
            'int': '100px',
            'decimal': '120px',
            'DateTime': '120px',
            'bool': '80px',
            'Guid': '120px'
        }
        width = width_map.get(field_type, '150px')
        
        # Determinar título
        title_map = {
            'Nombre': 'Nombre',
            'Descripcion': 'Descripción', 
            'Precio': 'Precio',
            'CodigoInterno': 'Código',
            'Cantidad': 'Cantidad',
            'Stock': 'Stock'
        }
        title = title_map.get(field_name, field_name)
        
        # Determinar capacidades de filtro/ordenamiento
        sortable = "true"
        filterable = "true" 
        
        # Descripción solo filtrable, no ordenable
        if field_name.lower() == 'descripcion':
            sortable = "false"
        
        # Template base - usar placeholder temporal
        config = f"""new ColumnConfig<Shared.Models.Entities.ENTITY_NAME_PLACEHOLDER>
                    {{
                        Property = "{field_name}",
                        Title = "{title}",
                        Width = "{width}",
                        Sortable = {sortable},
                        Filterable = {filterable},
                        TextAlign = TextAlign.Left,
                        Visible = true,
                        Order = {order}
                    }}"""
        
        return config
    
    
    def generate_viewmanager(self, entity_name, module, module_path):
        """Generar ViewManager completo"""
        try:
            # Detectar campos de la entidad
            fields = self.detect_entity_fields(entity_name)
            
            # Campo principal (primer campo, generalmente Nombre)
            primary_field = fields[0]['name'] if fields else 'Nombre'
            primary_field_title = primary_field
            
            # Generar configuraciones de columna
            column_configs = []
            
            for i, field in enumerate(fields, 1):
                column_configs.append(self.generate_column_config(field, i))
            
            # Preparar variables para el template
            variables = self.template_engine.prepare_entity_variables(entity_name, module)
            variables.update({
                'PRIMARY_FIELD': primary_field,
                'PRIMARY_FIELD_TITLE': primary_field_title,
                'COLUMN_CONFIGS': ',\n                    '.join(column_configs)
            })
            
            # Renderizar template
            viewmanager_content = self.template_engine.render_template("frontend/services/viewmanager.cs.template", variables)
            
            # Reemplazar placeholder final
            viewmanager_content = viewmanager_content.replace("ENTITY_NAME_PLACEHOLDER", entity_name)
            
            # Escribir archivo
            viewmanager_file = module_path / f"{entity_name}ViewManager.cs"
            viewmanager_file.write_text(viewmanager_content, encoding='utf-8')
            print(f"✅ {entity_name}ViewManager.cs generado")
            return True
            
        except Exception as e:
            print(f"❌ ERROR generando ViewManager: {e}")
            return False