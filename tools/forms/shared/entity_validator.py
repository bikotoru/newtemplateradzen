#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
✅ Entity Validator - Validador de coherencia cruzada para configuración de entidades
Valida que todos los campos referenciados existan y sean coherentes
"""

from typing import List, Set
from .entity_config import EntityConfiguration

class EntityConfigValidator:
    """Validador de coherencia para configuración de entidades"""
    
    def validate_full_configuration(self, config: EntityConfiguration) -> List[str]:
        """
        Validar configuración completa y retornar lista de errores
        """
        errors = []
        
        # Validaciones básicas
        errors.extend(self._validate_basic_requirements(config))
        
        # Validaciones de coherencia cruzada
        errors.extend(self._validate_field_coherence(config))
        
        # Validaciones de lookups
        errors.extend(self._validate_lookup_coherence(config))
        
        # Validaciones de búsqueda
        errors.extend(self._validate_search_coherence(config))
        
        # Validaciones de tablas referenciadas (solo para targets que crean BD)
        if config.target in ['db', 'todo']:
            errors.extend(self._validate_referenced_tables_exist(config))
        
        return errors
    
    def _validate_basic_requirements(self, config: EntityConfiguration) -> List[str]:
        """Validar requerimientos básicos"""
        errors = []
        
        if not config.entity_name:
            errors.append("entity_name es requerido")
        
        if not config.module:
            errors.append("module es requerido")
        
        if config.target not in ['db', 'interfaz', 'todo']:
            errors.append(f"target '{config.target}' inválido. Opciones: db, interfaz, todo")
        
        # Para targets que requieren BD
        if config.target in ['db', 'todo']:
            if not config.regular_fields and not config.foreign_keys:
                errors.append("targets 'db' y 'todo' requieren al menos un campo en --fields o --fk")
        
        return errors
    
    def _validate_field_coherence(self, config: EntityConfiguration) -> List[str]:
        """Validar coherencia entre campos de BD y campos de UI"""
        errors = []
        
        all_db_fields = config.get_all_db_fields()
        
        # 1. Validar form-fields
        for field_name in config.get_form_field_names():
            if field_name not in all_db_fields:
                errors.append(f"form-field '{field_name}' no existe en --fields o --fk")
        
        # 2. Validar grid-fields
        for field_name in config.get_grid_field_names():
            # Para campos con lookup, validar el campo base
            base_field = field_name.split('->')[0] if '->' in field_name else field_name
            
            if base_field not in all_db_fields:
                errors.append(f"grid-field '{base_field}' no existe en --fields o --fk")
        
        # 3. Validar readonly-fields
        for field_name in config.get_readonly_field_names():
            if field_name not in all_db_fields:
                errors.append(f"readonly-field '{field_name}' no existe en --fields o --fk")
        
        return errors
    
    def _validate_lookup_coherence(self, config: EntityConfiguration) -> List[str]:
        """Validar coherencia de lookups con foreign keys"""
        errors = []
        
        fk_fields = set(fk.field for fk in config.foreign_keys)
        
        for lookup_field, lookup_config in config.lookups.items():
            # 1. El campo del lookup debe ser un FK
            if lookup_field not in fk_fields:
                errors.append(f"lookup '{lookup_field}' debe ser definido como FK en --fk")
            
            # 2. Validar que la tabla objetivo coincida
            matching_fk = None
            for fk in config.foreign_keys:
                if fk.field == lookup_field:
                    matching_fk = fk
                    break
            
            if matching_fk and matching_fk.ref_table != lookup_config.target_table:
                errors.append(f"lookup '{lookup_field}': tabla '{lookup_config.target_table}' no coincide con FK '{matching_fk.ref_table}'")
            
            # 3. Validar show_in values
            valid_show_in = ['form', 'grid', 'both']
            for show_option in lookup_config.show_in:
                if show_option not in valid_show_in:
                    errors.append(f"lookup '{lookup_field}': show_in '{show_option}' inválido. Opciones: {valid_show_in}")
        
        return errors
    
    def _validate_search_coherence(self, config: EntityConfiguration) -> List[str]:
        """Validar coherencia de campos de búsqueda"""
        errors = []
        
        all_db_fields = config.get_all_db_fields()
        
        for search_field in config.search_fields:
            if search_field not in all_db_fields:
                errors.append(f"search-field '{search_field}' no existe en --fields o --fk")
        
        return errors
    
    def _validate_field_types_coherence(self, config: EntityConfiguration) -> List[str]:
        """Validar que los tipos de campos sean coherentes entre BD y UI"""
        errors = []
        
        # Mapear campos de BD por nombre para fácil lookup
        db_fields_map = {f.name: f for f in config.regular_fields}
        
        # Validar readonly fields tienen tipos coherentes
        for field_name, readonly_config in config.readonly_fields.items():
            if field_name in db_fields_map:
                db_field = db_fields_map[field_name]
                if db_field.field_type != readonly_config.field_type:
                    errors.append(f"readonly-field '{field_name}': tipo '{readonly_config.field_type}' no coincide con BD '{db_field.field_type}'")
        
        return errors
    
    def _validate_referenced_tables_exist(self, config: EntityConfiguration) -> List[str]:
        """Validar que las tablas referenciadas en FKs existan en la base de datos"""
        errors = []
        
        if not config.foreign_keys:
            return errors
        
        try:
            # Importar table generator para verificar existencia de tablas
            import sys
            from pathlib import Path
            
            # Agregar path de db tools
            root_path = Path.cwd()
            tools_path = root_path / "tools" / "db"
            sys.path.append(str(tools_path))
            
            from table import DatabaseTableGenerator
            db_generator = DatabaseTableGenerator()
            
            for fk in config.foreign_keys:
                table_name = fk.ref_table
                
                # Verificar si la tabla existe
                if not db_generator.table_exists(table_name):
                    errors.append(f"Tabla referenciada '{table_name}' no existe en la base de datos (FK: {fk.field})")
                    errors.append(f"💡 Crea primero la tabla '{table_name}' o usa una tabla existente")
        
        except Exception as e:
            # Si no podemos verificar, advertir pero no bloquear
            errors.append(f"⚠️ No se pudo verificar existencia de tablas referenciadas: {e}")
            errors.append("💡 Verifica manualmente que las tablas en --fk existan antes de continuar")
        
        return errors
    
    def validate_and_raise(self, config: EntityConfiguration):
        """Validar configuración y lanzar excepción si hay errores"""
        errors = self.validate_full_configuration(config)
        
        if errors:
            error_message = "❌ ERRORES DE CONFIGURACIÓN:\n" + "\n".join(f"  • {error}" for error in errors)
            raise ValueError(error_message)
    
    def print_validation_summary(self, config: EntityConfiguration):
        """Imprimir resumen de validación"""
        errors = self.validate_full_configuration(config)
        
        if not errors:
            print("✅ Configuración validada correctamente")
            print(f"📊 BD: {len(config.regular_fields)} campos regulares + {len(config.foreign_keys)} FKs")
            print(f"📝 UI: {len(config.form_fields)} form + {len(config.grid_fields)} grid + {len(config.readonly_fields)} readonly")
            print(f"🔗 Lookups: {len(config.lookups)}")
            print()
        else:
            print("❌ ERRORES DE CONFIGURACIÓN:")
            for error in errors:
                print(f"  • {error}")
            print()
            raise ValueError("Configuración inválida")