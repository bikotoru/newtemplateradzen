#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
🔧 Field Parsers - Parsers especializados para cada tipo de configuración de campo
Convierte strings de argumentos en objetos de configuración tipados
"""

import re
from typing import List, Dict, Optional
from .entity_config import (
    RegularFieldConfig, ForeignKeyConfig, FormFieldConfig, 
    GridFieldConfig, ReadOnlyFieldConfig, LookupConfig,
    FieldType, GridAlign
)

class FieldParsers:
    """Parsers especializados para diferentes tipos de campos"""
    
    # Mapeo de tipos de datos
    TYPE_MAPPING = {
        'string': FieldType.STRING,
        'text': FieldType.TEXT,
        'int': FieldType.INT,
        'decimal': FieldType.DECIMAL,
        'datetime': FieldType.DATETIME,
        'bool': FieldType.BOOL,
        'guid': FieldType.GUID,
    }
    
    def parse_regular_field(self, field_str: str) -> RegularFieldConfig:
        """
        Parse campo regular: "nombre:string:255"
        """
        parts = field_str.split(':')
        if len(parts) < 2:
            raise ValueError(f"Formato de campo inválido: {field_str}. Use 'nombre:tipo' o 'nombre:tipo:tamaño'")
        
        field_name = parts[0]
        field_type_str = parts[1].lower()
        field_size = parts[2] if len(parts) > 2 else None
        
        if field_type_str not in self.TYPE_MAPPING:
            raise ValueError(f"Tipo de dato no soportado: {field_type_str}. Tipos válidos: {list(self.TYPE_MAPPING.keys())}")
        
        field_type = self.TYPE_MAPPING[field_type_str]
        sql_type = self._get_sql_type(field_type, field_size)
        nullable = field_type != FieldType.BOOL  # bool siempre NOT NULL por defecto
        
        return RegularFieldConfig(
            name=field_name,
            field_type=field_type,
            size=field_size,
            sql_type=sql_type,
            nullable=nullable
        )
    
    def parse_foreign_key(self, fk_str: str) -> ForeignKeyConfig:
        """
        Parse FK: "categoria_id:categorias"
        """
        parts = fk_str.split(':')
        if len(parts) != 2:
            raise ValueError(f"Formato de FK inválido: {fk_str}. Use 'campo:tabla_referencia'")
        
        field_name = parts[0]
        ref_table = parts[1].lower()
        
        return ForeignKeyConfig(
            field=field_name,
            ref_table=ref_table,
            sql_type="UNIQUEIDENTIFIER"
        )
    
    def parse_form_field(self, form_str: str) -> FormFieldConfig:
        """
        Parse campo formulario: "nombre:required:placeholder=Ingrese nombre:min_length=3"
        """
        parts = form_str.split(':')
        field_name = parts[0]
        
        config = FormFieldConfig(name=field_name)
        
        for part in parts[1:]:
            part = part.strip()
            
            if part == "required":
                config.required = True
            elif part == "unique":
                config.unique = True
            elif part == "nullable":
                config.nullable = True
            elif "=" in part:
                key, value = part.split('=', 1)
                key = key.strip()
                value = value.strip()
                
                if key == "placeholder":
                    config.placeholder = value
                elif key == "label":
                    config.label = value
                elif key == "default":
                    config.default = value
                elif key == "min":
                    config.min_value = float(value)
                elif key == "max":
                    config.max_value = float(value)
                elif key == "min_length":
                    config.min_length = int(value)
                elif key == "max_length":
                    config.max_length = int(value)
            else:
                # Agregar como regla de validación genérica
                config.validation_rules.append(part)
        
        return config
    
    def parse_grid_field(self, grid_str: str) -> GridFieldConfig:
        """
        Parse campo grilla: "nombre:200px:left:sortable,filterable"
        o con lookup: "categoria_id->Categoria.Nombre:150px:left:filterable"
        """
        parts = grid_str.split(':')
        if len(parts) < 1:
            raise ValueError(f"Formato de grid-field inválido: {grid_str}")
        
        field_spec = parts[0]
        width = parts[1] if len(parts) > 1 else "150px"
        align_str = parts[2] if len(parts) > 2 else "left"
        options_str = parts[3] if len(parts) > 3 else ""
        
        # Parsear campo y display_field si es lookup
        field_name = field_spec
        display_field = None
        
        if "->" in field_spec:
            field_name, display_field = field_spec.split('->', 1)
        
        # Parsear alineación
        align = GridAlign.LEFT
        if align_str.lower() == "right":
            align = GridAlign.RIGHT
        elif align_str.lower() == "center":
            align = GridAlign.CENTER
        
        # Parsear opciones (s=sortable, f=filterable)
        sortable = 's' in options_str.lower()
        filterable = 'f' in options_str.lower()
        
        return GridFieldConfig(
            name=field_name,
            width=width,
            align=align,
            sortable=sortable,
            filterable=filterable,
            display_field=display_field
        )
    
    def parse_readonly_field(self, readonly_str: str) -> ReadOnlyFieldConfig:
        """
        Parse campo readonly: "stock_actual:int:label=Stock disponible"
        """
        parts = readonly_str.split(':')
        if len(parts) < 1:
            raise ValueError(f"Formato de readonly-field inválido: {readonly_str}")
        
        field_name = parts[0]
        field_type_str = parts[1] if len(parts) > 1 else "string"
        
        field_type = self.TYPE_MAPPING.get(field_type_str.lower(), FieldType.STRING)
        
        config = ReadOnlyFieldConfig(
            name=field_name,
            field_type=field_type
        )
        
        # Parsear opciones adicionales
        for part in parts[2:]:
            if "=" in part:
                key, value = part.split('=', 1)
                if key.strip() == "label":
                    config.label = value.strip()
                elif key.strip() == "format":
                    config.format = value.strip()
        
        return config
    
    def parse_lookup(self, lookup_str: str) -> LookupConfig:
        """
        Parse lookup: "categoria_id:categorias:Nombre:required:form,grid:cache:fast"
        """
        parts = lookup_str.split(':')
        if len(parts) < 3:
            raise ValueError(f"Formato de lookup inválido: {lookup_str}. Use 'campo:tabla:campo_display[:opciones]'")
        
        field_name = parts[0]
        target_table = parts[1]
        display_field = parts[2]
        
        config = LookupConfig(
            field=field_name,
            target_table=target_table,
            display_field=display_field
        )
        
        # Parsear opciones
        for part in parts[3:]:
            part = part.strip()
            
            if part == "required":
                config.required = True
            elif part == "cache":
                config.cache = True
            elif part == "fast":
                config.fast_lookup = True
            elif "," in part:
                # show_in: "form,grid"
                config.show_in = [s.strip() for s in part.split(',')]
            else:
                # Otras opciones como strings simples
                if part not in config.show_in:
                    config.show_in.append(part)
        
        return config
    
    def parse_search_fields(self, search_str: str) -> List[str]:
        """
        Parse campos de búsqueda: "nombre,codigo,descripcion"
        """
        return [f.strip() for f in search_str.split(',') if f.strip()]
    
    def _get_sql_type(self, field_type: FieldType, size: Optional[str]) -> str:
        """Convertir FieldType a SQL type"""
        if field_type == FieldType.STRING:
            if size and size.isdigit():
                return f"NVARCHAR({size})"
            return "NVARCHAR(255)"
        elif field_type == FieldType.TEXT:
            return "NVARCHAR(MAX)"
        elif field_type == FieldType.INT:
            return "INT"
        elif field_type == FieldType.DECIMAL:
            if size and ',' in size:
                return f"DECIMAL({size})"
            return "DECIMAL(18,2)"
        elif field_type == FieldType.DATETIME:
            return "DATETIME2"
        elif field_type == FieldType.BOOL:
            return "BIT"
        elif field_type == FieldType.GUID:
            return "UNIQUEIDENTIFIER"
        else:
            return "NVARCHAR(255)"