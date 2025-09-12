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
    
    def detect_navigation_properties(self, entity_name):
        """Detectar propiedades de navegación reales del modelo"""
        try:
            entity_file = self.root_path / "Shared.Models" / "Entities" / f"{entity_name}.cs"
            
            if not entity_file.exists():
                return {}
            
            content = entity_file.read_text(encoding='utf-8')
            navigation_props = {}
            
            lines = content.split('\n')
            for line in lines:
                line = line.strip()
                # Buscar propiedades virtuales (navegación): public virtual EntityName? PropName { get; set; }
                if line.startswith('public virtual ') and '{ get; set; }' in line and 'ICollection' not in line:
                    # Extraer información
                    parts = line.split()
                    if len(parts) >= 4:
                        entity_type = parts[2].replace('?', '').replace(' ', '')  # "Areas", "Centrodecosto"
                        prop_name = parts[3].replace('?', '').replace(' ', '')   # "Area", "Centrodecosto", etc.
                        
                        # Mapear: nombre de entidad → nombre real de propiedad
                        navigation_props[entity_type] = prop_name
            
            return navigation_props
            
        except Exception as e:
            return {}
    
    def _find_navigation_property(self, entity_name, navigation_props):
        """Buscar propiedad de navegación con fuzzy matching"""
        # 1. Búsqueda exacta
        if entity_name in navigation_props:
            return navigation_props[entity_name]
        
        # 2. Búsqueda case-insensitive
        entity_lower = entity_name.lower()
        for nav_entity, nav_prop in navigation_props.items():
            if nav_entity.lower() == entity_lower:
                return nav_prop
        
        # 3. Búsqueda por similitud (CentroDeCosto vs Centrodecosto)
        entity_clean = entity_name.lower().replace('de', '')  # "centrodecosto"
        for nav_entity, nav_prop in navigation_props.items():
            nav_clean = nav_entity.lower().replace('de', '')
            if nav_clean == entity_clean:
                return nav_prop
        
        return None
    
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
    
    def generate_from_config(self, config):
        """Generar configuraciones desde config parseado de grid_fields"""
        column_configs = []
        includes = []
        primary_field = 'Nombre'  # Default
        
        # IMPORTANTE: Detectar propiedades de navegación reales del modelo
        navigation_props = self.detect_navigation_properties(config.entity_name)
        
        # Procesar grid_fields de la configuración
        order = 1
        for field_name, grid_field in config.grid_fields.items():
            # Determinar Property y Title
            if grid_field.display_field:
                # Navegación: region_id -> Region.Nombre
                property_name = grid_field.display_field  # "Region.Nombre"
                
                # Extraer nombre de entidad para Title (Region.Nombre -> Region)
                entity_name = grid_field.display_field.split('.')[0]  # "Region"
                title = entity_name
                
                # CORRECCIÓN: Usar propiedades de navegación reales
                real_nav_prop = self._find_navigation_property(entity_name, navigation_props)
                if real_nav_prop:
                    include_statement = f".Include(x=>x.{real_nav_prop})"
                    print(f"✅ Include corregido: {entity_name} -> x.{real_nav_prop}")
                else:
                    # Fallback al método anterior si no se encuentra
                    include_statement = f".Include(x=>x.{entity_name})"
                    print(f"⚠️ Include fallback para {entity_name}: x.{entity_name}")
                
                if include_statement not in includes:
                    includes.append(include_statement)
                    
            else:
                # Campo directo - normalizar siguiendo reglas de EF Core
                property_name = self.normalize_field_name_for_ef(field_name)
                title = self.get_title_for_field(field_name)
                
                # Si es el primer campo, usarlo como primary_field (normalizado)
                if order == 1:
                    primary_field = property_name
            
            # Generar configuración de columna
            sortable = "true" if grid_field.sortable else "false"
            filterable = "true" if grid_field.filterable else "false"
            
            # Mapear alineación
            align_map = {
                'left': 'TextAlign.Left',
                'right': 'TextAlign.Right', 
                'center': 'TextAlign.Center'
            }
            text_align = align_map.get(grid_field.align.value, 'TextAlign.Left')
            
            config_text = f"""new ColumnConfig<Shared.Models.Entities.ENTITY_NAME_PLACEHOLDER>
                    {{
                        Property = "{property_name}",
                        Title = "{title}",
                        Width = "{grid_field.width}",
                        Sortable = {sortable},
                        Filterable = {filterable},
                        TextAlign = {text_align},
                        Visible = true,
                        Order = {order}
                    }}"""
            
            column_configs.append(config_text)
            order += 1
        
        return column_configs, includes, primary_field
    
    def get_title_for_field(self, field_name):
        """Obtener título apropiado para un campo"""
        title_map = {
            'nombre': 'Nombre',
            'descripcion': 'Descripción', 
            'precio': 'Precio',
            'codigo_interno': 'Código',
            'cantidad': 'Cantidad',
            'stock': 'Stock'
        }
        return title_map.get(field_name.lower(), field_name)
    
    def normalize_field_name_for_ef(self, field_name):
        """Normalizar nombre de campo siguiendo reglas de Entity Framework Core"""
        # Dividir por _ y convertir cada parte a PascalCase
        parts = field_name.split('_')
        ef_field_name = ''.join(part.capitalize() for part in parts if part)
        return ef_field_name
    
    def generate_viewmanager(self, entity_name, module, module_path, config=None):
        """Generar ViewManager completo"""
        try:
            # Si tenemos configuración, usar grid_fields; sino detectar campos automáticamente
            if config and config.grid_fields:
                column_configs, includes, primary_field = self.generate_from_config(config)
            else:
                # Detectar campos de la entidad (comportamiento original)
                fields = self.detect_entity_fields(entity_name)
                
                # Campo principal (primer campo, generalmente Nombre)
                primary_field = fields[0]['name'] if fields else 'Nombre'
                
                # Generar configuraciones de columna
                column_configs = []
                for i, field in enumerate(fields, 1):
                    column_configs.append(self.generate_column_config(field, i))
                
                includes = []  # Sin includes automáticos en modo legacy
            
            # Preparar variables para el template
            variables = self.template_engine.prepare_entity_variables(entity_name, module)
            variables.update({
                'PRIMARY_FIELD': primary_field,
                'PRIMARY_FIELD_TITLE': primary_field,
                'COLUMN_CONFIGS': ',\n                    '.join(column_configs),
                'INCLUDES': '\n                    '.join(includes) if includes else ''
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