#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
⚙️ Entity Configurator - Configurador principal que integra parsers y validadores
Convierte argumentos de línea de comandos en configuración tipada y validada
"""

from typing import List, Optional
import subprocess
import json
from pathlib import Path
from .entity_config import EntityConfiguration, NNTableConfig
from .field_parsers import FieldParsers
from .entity_validator import EntityConfigValidator

class EntityConfigurator:
    """Configurador principal para entidades avanzadas"""
    
    def __init__(self):
        self.parsers = FieldParsers()
        self.validator = EntityConfigValidator()
        self.root_path = Path.cwd()
        self.project_path = self.root_path / "Backend"
    
    def read_connection_string(self):
        """Lee la connection string desde launchSettings.json"""
        launch_settings_path = self.project_path / "Properties" / "launchSettings.json"
        
        if not launch_settings_path.exists():
            return None
            
        try:
            with open(launch_settings_path, 'r', encoding='utf-8') as f:
                settings = json.load(f)
                
            # Buscar en los profiles la variable SQL
            for profile_name, profile_data in settings.get("profiles", {}).items():
                env_vars = profile_data.get("environmentVariables", {})
                sql_connection = env_vars.get("SQL")
                
                if sql_connection:
                    return sql_connection
                    
            return None
            
        except Exception:
            return None
    
    def table_exists(self, table_name):
        """Verifica si una tabla existe en la base de datos"""
        try:
            connection_string = self.read_connection_string()
            if not connection_string:
                return False
            
            # Parsear connection string para extraer componentes
            parts = {}
            for part in connection_string.split(';'):
                if '=' in part and part.strip():
                    key, value = part.split('=', 1)
                    parts[key.strip().lower()] = value.strip()
            
            server = parts.get('server', 'localhost')
            database = parts.get('database', 'NewPOC')
            user_id = parts.get('user id', 'sa')
            password = parts.get('password', 'Soporte.2019')
            
            # Query para verificar existencia
            check_query = f"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{table_name}'"
            
            result = subprocess.run([
                'sqlcmd', '-S', server, '-U', user_id, '-P', password, '-d', database,
                '-Q', check_query, '-h', '-1'
            ], capture_output=True, text=True, timeout=30)
            
            if result.returncode == 0:
                count = int(result.stdout.strip())
                return count > 0
            
            return False
            
        except Exception:
            return False
    
    def get_available_tables(self):
        """Obtener lista de tablas disponibles en la base de datos"""
        try:
            connection_string = self.read_connection_string()
            if not connection_string:
                return []
            
            # Parsear connection string
            parts = {}
            for part in connection_string.split(';'):
                if '=' in part and part.strip():
                    key, value = part.split('=', 1)
                    parts[key.strip().lower()] = value.strip()
            
            server = parts.get('server', 'localhost')
            database = parts.get('database', 'NewPOC')
            user_id = parts.get('user id', 'sa')
            password = parts.get('password', 'Soporte.2019')
            
            # Query para obtener tablas
            query = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME NOT LIKE 'system_%' ORDER BY TABLE_NAME"
            
            result = subprocess.run([
                'sqlcmd', '-S', server, '-U', user_id, '-P', password, '-d', database,
                '-Q', query, '-h', '-1'
            ], capture_output=True, text=True, timeout=30)
            
            if result.returncode == 0:
                tables = [line.strip() for line in result.stdout.strip().split('\n') if line.strip()]
                return tables
            
            return []
            
        except Exception:
            return []
    
    def configure_nn_table(self, entity_name):
        """Configurar tabla NN interactivamente"""
        print("🔗 DETECTADA TABLA MUCHOS-A-MUCHOS (NN)")
        print(f"📝 Tabla: {entity_name}")
        print()
        
        # Obtener tablas disponibles
        available_tables = self.get_available_tables()
        if available_tables:
            print("📋 TABLAS DISPONIBLES:")
            for i, table in enumerate(available_tables, 1):
                print(f"   {i}. {table}")
            print()
        
        # Solicitar source table
        while True:
            source_table = input("🎯 Ingrese SOURCE TABLE (ej: venta): ").strip().lower()
            if source_table:
                if available_tables and source_table not in available_tables:
                    print(f"⚠️ Tabla '{source_table}' no existe en la base de datos")
                    continue
                break
            print("❌ Source table es requerido")
        
        # Solicitar target table
        while True:
            target_table = input("🎯 Ingrese TARGET TABLE (ej: producto): ").strip().lower()
            if target_table:
                if available_tables and target_table not in available_tables:
                    print(f"⚠️ Tabla '{target_table}' no existe en la base de datos")
                    continue
                break
            print("❌ Target table es requerido")
        
        # Verificar si ya existe la tabla NN
        expected_table_name = f"nn_{source_table}_{target_table}"
        alias = None
        
        if self.table_exists(expected_table_name):
            print(f"⚠️ La tabla '{expected_table_name}' ya existe")
            while True:
                use_alias = input("🤔 ¿Desea usar un ALIAS? (s/n): ").strip().lower()
                if use_alias in ['s', 'si', 'y', 'yes']:
                    while True:
                        alias = input("📝 Ingrese el ALIAS (ej: promocion): ").strip().lower()
                        if alias:
                            final_table_name = f"nn_{source_table}_{target_table}_{alias}"
                            if self.table_exists(final_table_name):
                                print(f"⚠️ La tabla '{final_table_name}' también existe. Pruebe otro alias.")
                                continue
                            break
                        print("❌ Alias es requerido")
                    break
                elif use_alias in ['n', 'no']:
                    print(f"❌ ERROR: La tabla '{expected_table_name}' ya existe y no se proporcionó alias")
                    raise Exception(f"Tabla NN {expected_table_name} ya existe")
                else:
                    print("❌ Responda 's' o 'n'")
        
        print()
        print("✅ CONFIGURACIÓN NN COMPLETADA:")
        print(f"   🎯 Source: {source_table}")
        print(f"   🎯 Target: {target_table}")
        if alias:
            print(f"   🏷️ Alias: {alias}")
            print(f"   📝 Tabla final: nn_{source_table}_{target_table}_{alias}")
        else:
            print(f"   📝 Tabla final: nn_{source_table}_{target_table}")
        print()
        
        return NNTableConfig(
            source_table=source_table,
            target_table=target_table,
            alias=alias
        )
    
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
        
        # Detectar y configurar tabla NN si es necesario
        if config.is_nn_table():
            if hasattr(args, 'nn_source') and args.nn_source and hasattr(args, 'nn_target') and args.nn_target:
                # Usar argumentos de línea de comandos
                alias = getattr(args, 'nn_alias', None)
                config.nn_config = NNTableConfig(
                    source_table=args.nn_source,
                    target_table=args.nn_target,
                    alias=alias
                )
                print("🔗 CONFIGURACIÓN NN DESDE ARGUMENTOS:")
                print(f"   🎯 Source: {args.nn_source}")
                print(f"   🎯 Target: {args.nn_target}")
                if alias:
                    print(f"   🏷️ Alias: {alias}")
                print()
            else:
                # Configurar interactivamente
                config.nn_config = self.configure_nn_table(args.entity)
            
            # Actualizar el nombre de la entidad con la configuración NN
            config.entity_name = config.get_nn_table_name()
        
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
        
        # Mostrar configuración NN si existe
        if config.is_nn_table() and config.nn_config:
            print()
            print("🔗 CONFIGURACIÓN MUCHOS-A-MUCHOS:")
            print(f"   🎯 Source Table: {config.nn_config.source_table}")
            print(f"   🎯 Target Table: {config.nn_config.target_table}")
            if config.nn_config.alias:
                print(f"   🏷️ Alias: {config.nn_config.alias}")
            print(f"   📝 Tabla BD: {config.get_nn_table_name()}")
        
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