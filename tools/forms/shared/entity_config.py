#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
🎯 Entity Configuration - Clases para configuración avanzada de entidades
Maneja toda la configuración compleja de campos, validaciones y UI
"""

from dataclasses import dataclass, field
from typing import List, Dict, Optional, Set
from enum import Enum

class FieldType(Enum):
    STRING = "string"
    TEXT = "text" 
    INT = "int"
    DECIMAL = "decimal"
    DATETIME = "datetime"
    BOOL = "bool"
    GUID = "guid"

class GridAlign(Enum):
    LEFT = "left"
    RIGHT = "right"
    CENTER = "center"

@dataclass
class RegularFieldConfig:
    """Configuración de un campo regular de BD"""
    name: str
    field_type: FieldType
    size: Optional[str] = None
    sql_type: Optional[str] = None
    nullable: bool = True

@dataclass
class ForeignKeyConfig:
    """Configuración de una Foreign Key"""
    field: str              # categoria_id
    ref_table: str         # categorias
    sql_type: str = "UNIQUEIDENTIFIER"

@dataclass
class FormFieldConfig:
    """Configuración específica de campo en formulario"""
    name: str
    required: bool = False
    unique: bool = False
    default: Optional[str] = None
    min_value: Optional[float] = None
    max_value: Optional[float] = None
    min_length: Optional[int] = None
    max_length: Optional[int] = None
    placeholder: Optional[str] = None
    label: Optional[str] = None
    nullable: bool = True
    validation_rules: List[str] = field(default_factory=list)

@dataclass
class GridFieldConfig:
    """Configuración específica de campo en grilla"""
    name: str
    width: str = "150px"
    align: GridAlign = GridAlign.LEFT
    sortable: bool = False
    filterable: bool = False
    display_field: Optional[str] = None  # Para lookups: categoria_id->Categoria.Nombre
    order: int = 0

@dataclass
class ReadOnlyFieldConfig:
    """Configuración de campo de solo lectura"""
    name: str
    field_type: FieldType
    label: Optional[str] = None
    format: Optional[str] = None

@dataclass
class LookupConfig:
    """Configuración completa de lookup"""
    field: str              # categoria_id
    target_table: str       # categorias
    display_field: str      # Nombre
    required: bool = False
    cache: bool = False
    fast_lookup: bool = False
    show_in: List[str] = field(default_factory=lambda: ["form"])  # form, grid
    filter_field: Optional[str] = None

@dataclass
class EntityConfiguration:
    """Configuración completa de una entidad"""
    entity_name: str
    entity_plural: str
    module: str
    target: str
    
    # Configuración de base de datos (como table.py)
    regular_fields: List[RegularFieldConfig] = field(default_factory=list)
    foreign_keys: List[ForeignKeyConfig] = field(default_factory=list)
    
    # Configuración de UI
    form_fields: Dict[str, FormFieldConfig] = field(default_factory=dict)
    grid_fields: Dict[str, GridFieldConfig] = field(default_factory=dict)
    readonly_fields: Dict[str, ReadOnlyFieldConfig] = field(default_factory=dict)
    lookups: Dict[str, LookupConfig] = field(default_factory=dict)
    
    # Configuración adicional
    search_fields: List[str] = field(default_factory=list)
    
    def get_all_db_fields(self) -> Set[str]:
        """Obtener todos los campos que van a la BD"""
        regular = set(f.name for f in self.regular_fields)
        fks = set(fk.field for fk in self.foreign_keys)
        return regular.union(fks)
    
    def get_form_field_names(self) -> Set[str]:
        """Obtener nombres de campos del formulario"""
        return set(self.form_fields.keys())
    
    def get_grid_field_names(self) -> Set[str]:
        """Obtener nombres de campos de la grilla"""
        return set(self.grid_fields.keys())
    
    def get_readonly_field_names(self) -> Set[str]:
        """Obtener nombres de campos readonly"""
        return set(self.readonly_fields.keys())
    
    def get_lookup_field_names(self) -> Set[str]:
        """Obtener nombres de campos con lookup"""
        return set(self.lookups.keys())