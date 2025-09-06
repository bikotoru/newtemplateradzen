#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
⚙️ Entity Configurator - Configurador principal que integra parsers y validadores
Convierte argumentos de línea de comandos en configuración tipada y validada
"""

from typing import List, Optional
from .entity_config import EntityConfiguration
from .field_parsers import FieldParsers
from .entity_validator import EntityConfigValidator

class EntityConfigurator:
    """Configurador principal para entidades avanzadas"""
    
    def __init__(self):
        self.parsers = FieldParsers()
        self.validator = EntityConfigValidator()
    
    def configure_from_args(self, args) -> EntityConfiguration:
        """
        Crear configuración completa desde argumentos de línea de comandos
        """
        # Crear configuración base
        config = EntityConfiguration(
            entity_name=args.entity,
            entity_plural=args.plural or f"{args.entity}s",
            module=args.module,
            target=args.target
        )
        
        # Parsear campos de base de datos
        if hasattr(args, 'fields') and args.fields:
            for field_str in args.fields:
                regular_field = self.parsers.parse_regular_field(field_str)
                config.regular_fields.append(regular_field)
        
        # Parsear foreign keys
        if hasattr(args, 'fk') and args.fk:
            for fk_str in args.fk:
                fk_field = self.parsers.parse_foreign_key(fk_str)
                config.foreign_keys.append(fk_field)
        
        # Parsear form fields
        if hasattr(args, 'form_fields') and args.form_fields:
            for form_str in args.form_fields:
                form_field = self.parsers.parse_form_field(form_str)
                config.form_fields[form_field.name] = form_field
        
        # Parsear grid fields
        if hasattr(args, 'grid_fields') and args.grid_fields:
            for grid_str in args.grid_fields:
                grid_field = self.parsers.parse_grid_field(grid_str)
                config.grid_fields[grid_field.name] = grid_field
        
        # Parsear readonly fields
        if hasattr(args, 'readonly_fields') and args.readonly_fields:
            for readonly_str in args.readonly_fields:
                readonly_field = self.parsers.parse_readonly_field(readonly_str)
                config.readonly_fields[readonly_field.name] = readonly_field
        
        # Parsear lookups
        if hasattr(args, 'lookups') and args.lookups:
            for lookup_str in args.lookups:
                lookup = self.parsers.parse_lookup(lookup_str)
                config.lookups[lookup.field] = lookup
        
        # Parsear search fields
        if hasattr(args, 'search_fields') and args.search_fields:
            config.search_fields = self.parsers.parse_search_fields(args.search_fields)
        
        # Validar configuración completa
        self.validator.validate_and_raise(config)
        
        return config
    
    def print_configuration_summary(self, config: EntityConfiguration):
        """Imprimir resumen detallado de la configuración"""
        print("=" * 70)
        print(f"🎯 CONFIGURACIÓN ENTIDAD: {config.entity_name}")
        print("=" * 70)
        print(f"📝 Plural: {config.entity_plural}")
        print(f"📁 Módulo: {config.module}")
        print(f"🎯 Target: {config.target}")
        print()
        
        # Base de datos
        if config.regular_fields or config.foreign_keys:
            print("🗄️ BASE DE DATOS:")
            
            if config.regular_fields:
                print(f"   📝 Campos regulares ({len(config.regular_fields)}):")
                for field in config.regular_fields:
                    size_info = f"({field.size})" if field.size else ""
                    nullable_info = " NULL" if field.nullable else " NOT NULL"
                    print(f"      • {field.name}: {field.field_type.value}{size_info}{nullable_info}")
            
            if config.foreign_keys:
                print(f"   🔗 Foreign Keys ({len(config.foreign_keys)}):")
                for fk in config.foreign_keys:
                    print(f"      • {fk.field} → {fk.ref_table}")
            
            print()
        
        # Configuración UI
        if config.form_fields:
            print(f"📝 FORMULARIO ({len(config.form_fields)}):")
            for name, field in config.form_fields.items():
                rules = []
                if field.required:
                    rules.append("required")
                if field.unique:
                    rules.append("unique")
                if field.placeholder:
                    rules.append(f"placeholder='{field.placeholder}'")
                if field.min_value is not None:
                    rules.append(f"min={field.min_value}")
                if field.max_value is not None:
                    rules.append(f"max={field.max_value}")
                
                rules_str = f" [{', '.join(rules)}]" if rules else ""
                print(f"   • {name}{rules_str}")
            print()
        
        if config.grid_fields:
            print(f"📊 GRILLA ({len(config.grid_fields)}):")
            for name, field in config.grid_fields.items():
                features = []
                if field.sortable:
                    features.append("sortable")
                if field.filterable:
                    features.append("filterable")
                
                display_info = f" → {field.display_field}" if field.display_field else ""
                features_str = f" [{', '.join(features)}]" if features else ""
                
                print(f"   • {name}{display_info}: {field.width} {field.align.value}{features_str}")
            print()
        
        if config.readonly_fields:
            print(f"👁️ SOLO LECTURA ({len(config.readonly_fields)}):")
            for name, field in config.readonly_fields.items():
                label_info = f" ('{field.label}')" if field.label else ""
                print(f"   • {name}: {field.field_type.value}{label_info}")
            print()
        
        if config.lookups:
            print(f"🔗 LOOKUPS ({len(config.lookups)}):")
            for name, lookup in config.lookups.items():
                features = []
                if lookup.required:
                    features.append("required")
                if lookup.cache:
                    features.append("cache")
                if lookup.fast_lookup:
                    features.append("fast")
                
                show_in_str = f" [{', '.join(lookup.show_in)}]" if lookup.show_in else ""
                features_str = f" ({', '.join(features)})" if features else ""
                
                print(f"   • {name} → {lookup.target_table}.{lookup.display_field}{show_in_str}{features_str}")
            print()
        
        if config.search_fields:
            print(f"🔍 BÚSQUEDA: {', '.join(config.search_fields)}")
            print()
        
        print("✅ Configuración validada y lista para generar")
        print("=" * 70)
        print()