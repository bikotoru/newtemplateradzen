#!/usr/bin/env python3
"""
🛠️ Database Table Generator for .NET + Blazor
Creates database tables automatically with BaseEntity fields

Usage:
    python tools/db/table.py --name "products" --fields "nombre:string:255" "precio:decimal:18,2"
    python tools/db/table.py --name "orders" --fk "customer_id:customers" --execute
"""

import os
import sys
import json
import subprocess
import argparse
from pathlib import Path
import re

class DatabaseTableGenerator:
    def __init__(self, project_path="Backend"):
        self.project_path = Path(project_path)
        self.root_path = Path.cwd()
        
        # Mapeo de tipos de datos
        self.type_mapping = {
            'string': self._handle_string_type,
            'text': lambda size: 'NVARCHAR(MAX)',
            'int': lambda size: 'INT',
            'decimal': self._handle_decimal_type,
            'datetime': lambda size: 'DATETIME2',
            'bool': lambda size: 'BIT',
            'guid': lambda size: 'UNIQUEIDENTIFIER'
        }
        
    def print_header(self):
        print("=" * 70)
        print("🛠️  DATABASE TABLE GENERATOR")
        print("=" * 70)
        print()
    
    def read_connection_string(self):
        """Lee la connection string desde launchSettings.json"""
        launch_settings_path = self.project_path / "Properties" / "launchSettings.json"
        
        if not launch_settings_path.exists():
            print(f"❌ ERROR: No se encontró launchSettings.json en {launch_settings_path}")
            return None
            
        try:
            with open(launch_settings_path, 'r', encoding='utf-8') as f:
                settings = json.load(f)
                
            # Buscar en los profiles la variable SQL
            for profile_name, profile_data in settings.get("profiles", {}).items():
                env_vars = profile_data.get("environmentVariables", {})
                sql_connection = env_vars.get("SQL")
                
                if sql_connection:
                    print(f"✅ Connection string encontrado en profile: {profile_name}")
                    return sql_connection
                    
            print(f"❌ ERROR: No se encontró variable 'SQL' en environmentVariables")
            return None
            
        except Exception as e:
            print(f"❌ ERROR leyendo launchSettings.json: {e}")
            return None
    
    def _handle_string_type(self, size):
        """Maneja tipos string con tamaño"""
        if size and size.isdigit():
            return f"NVARCHAR({size})"
        return "NVARCHAR(255)"  # Default
    
    def _handle_decimal_type(self, size):
        """Maneja tipos decimal con precisión"""
        if size and ',' in size:
            return f"DECIMAL({size})"
        return "DECIMAL(18,2)"  # Default
    
    def validate_table_name(self, name):
        """Valida nombre de tabla"""
        if not re.match(r'^[a-zA-Z][a-zA-Z0-9_]*$', name):
            raise ValueError(f"Nombre de tabla inválido: {name}. Debe comenzar con letra y contener solo letras, números y guiones bajos.")
        return name.lower()
    
    def parse_field(self, field_str):
        """Parsea un campo: 'nombre:tipo:tamaño' """
        parts = field_str.split(':')
        if len(parts) < 2:
            raise ValueError(f"Formato de campo inválido: {field_str}. Use 'nombre:tipo' o 'nombre:tipo:tamaño'")
        
        field_name = parts[0].lower()
        field_type = parts[1].lower()
        field_size = parts[2] if len(parts) > 2 else None
        
        if field_type not in self.type_mapping:
            raise ValueError(f"Tipo de dato no soportado: {field_type}. Tipos válidos: {list(self.type_mapping.keys())}")
        
        sql_type = self.type_mapping[field_type](field_size)
        
        return {
            'name': field_name,
            'type': field_type,
            'size': field_size,
            'sql_type': sql_type,
            'nullable': field_type not in ['bool']  # bool siempre NOT NULL por defecto
        }
    
    def parse_foreign_key(self, fk_str):
        """Parsea una FK: 'campo:tabla_referencia' """
        parts = fk_str.split(':')
        if len(parts) != 2:
            raise ValueError(f"Formato de FK inválido: {fk_str}. Use 'campo:tabla_referencia'")
        
        field_name = parts[0].lower()
        ref_table = parts[1].lower()
        
        return {
            'field': field_name,
            'ref_table': ref_table,
            'sql_type': 'UNIQUEIDENTIFIER'
        }
    
    def generate_base_fields(self):
        """Genera los campos base de BaseEntity"""
        return """    Id UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
    OrganizationId UNIQUEIDENTIFIER NULL,
    FechaCreacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
    FechaModificacion DATETIME2 DEFAULT GETUTCDATE() NOT NULL,
    CreadorId UNIQUEIDENTIFIER NULL,
    ModificadorId UNIQUEIDENTIFIER NULL,
    Active BIT DEFAULT 1 NOT NULL"""
    
    def generate_custom_fields(self, fields):
        """Genera campos personalizados"""
        if not fields:
            return ""
        
        field_lines = []
        for field in fields:
            nullable = "NULL" if field['nullable'] else "NOT NULL"
            field_lines.append(f"    {field['name']} {field['sql_type']} {nullable}")
        
        return ",\n" + ",\n".join(field_lines) if field_lines else ""
    
    def generate_fk_fields(self, foreign_keys):
        """Genera campos de Foreign Keys"""
        if not foreign_keys:
            return ""
        
        fk_lines = []
        for fk in foreign_keys:
            fk_lines.append(f"    {fk['field']} {fk['sql_type']} NULL")
        
        return ",\n" + ",\n".join(fk_lines) if fk_lines else ""
    
    def generate_base_constraints(self, table_name):
        """Genera constraints base de BaseEntity"""
        return f"""    
    -- BaseEntity Foreign Keys
    CONSTRAINT FK_{table_name}_OrganizationId 
        FOREIGN KEY (OrganizationId) REFERENCES system_organization(Id),
    CONSTRAINT FK_{table_name}_CreadorId 
        FOREIGN KEY (CreadorId) REFERENCES system_users(Id),
    CONSTRAINT FK_{table_name}_ModificadorId 
        FOREIGN KEY (ModificadorId) REFERENCES system_users(Id)"""
    
    def generate_custom_constraints(self, table_name, foreign_keys, unique_fields):
        """Genera constraints personalizados"""
        constraints = []
        
        # Foreign Keys personalizados
        for fk in foreign_keys:
            constraint = f"""    CONSTRAINT FK_{table_name}_{fk['field']} 
        FOREIGN KEY ({fk['field']}) REFERENCES {fk['ref_table']}(Id)"""
            constraints.append(constraint)
        
        # Unique constraints
        for unique_field in unique_fields:
            constraint = f"    CONSTRAINT UK_{table_name}_{unique_field} UNIQUE ({unique_field})"
            constraints.append(constraint)
        
        return ",\n" + ",\n".join(constraints) if constraints else ""
    
    def generate_sql(self, table_name, fields=None, foreign_keys=None, unique_fields=None):
        """Genera el SQL completo para crear la tabla"""
        fields = fields or []
        foreign_keys = foreign_keys or []
        unique_fields = unique_fields or []
        
        sql = f"""-- ========================================
-- 📊 TABLA: {table_name}
-- Generada automáticamente con BaseEntity
-- ========================================

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{table_name}' AND xtype='U')
BEGIN
    CREATE TABLE {table_name} (
{self.generate_base_fields()}{self.generate_custom_fields(fields)}{self.generate_fk_fields(foreign_keys)},
{self.generate_base_constraints(table_name)}{self.generate_custom_constraints(table_name, foreign_keys, unique_fields)}
    );
    
    PRINT '✅ Tabla {table_name} creada exitosamente';
END
ELSE
BEGIN
    PRINT '📄 Tabla {table_name} ya existe';
END

GO"""
        
        return sql
    
    def execute_sql(self, sql, connection_string):
        """Ejecuta el SQL en la base de datos"""
        print("\n🔧 EJECUTANDO SQL EN BASE DE DATOS")
        print("-" * 50)
        
        try:
            # Crear archivo temporal con el SQL
            temp_file = Path("temp_table.sql")
            temp_file.write_text(sql, encoding='utf-8')
            
            # Parsear connection string para obtener server, database, user, password
            conn_parts = {}
            for part in connection_string.split(';'):
                if '=' in part:
                    key, value = part.split('=', 1)
                    conn_parts[key.strip().lower()] = value.strip()
            
            server = conn_parts.get('server', 'localhost')
            database = conn_parts.get('database', 'master')
            user_id = conn_parts.get('user id', 'sa')
            password = conn_parts.get('password', '')
            
            # Ejecutar con sqlcmd
            cmd = [
                "sqlcmd", 
                "-S", server,
                "-d", database,
                "-U", user_id,
                "-P", password,
                "-i", str(temp_file),
                "-C"  # Trust server certificate
            ]
            
            print(f"   🔗 Servidor: {server}")
            print(f"   📄 Base de datos: {database}")
            print(f"   👤 Usuario: {user_id}")
            print()
            
            result = subprocess.run(cmd, capture_output=True, text=True)
            
            # Limpiar archivo temporal
            temp_file.unlink()
            
            if result.returncode == 0:
                print("   ✅ Tabla creada exitosamente")
                if result.stdout.strip():
                    print(f"   📋 Output: {result.stdout.strip()}")
                return True
            else:
                print("   ❌ Error ejecutando SQL:")
                if result.stdout:
                    print(f"   STDOUT: {result.stdout}")
                if result.stderr:
                    print(f"   STDERR: {result.stderr}")
                return False
                
        except FileNotFoundError:
            print("   ❌ ERROR: sqlcmd no encontrado. Instala SQL Server command line tools.")
            return False
        except Exception as e:
            print(f"   ❌ ERROR ejecutando SQL: {e}")
            return False
    
    def regenerate_models(self):
        """Regenera los modelos usando el script de dbsync"""
        print("\n🔄 REGENERANDO MODELOS .NET")
        print("-" * 50)
        
        try:
            dbsync_script = self.root_path / "tools" / "dbsync" / "generate-models.py"
            if not dbsync_script.exists():
                print("   ⚠️  Script de regeneración no encontrado, omitiendo...")
                return False
            
            result = subprocess.run([
                sys.executable, str(dbsync_script)
            ], capture_output=True, text=True, cwd=self.root_path)
            
            if result.returncode == 0:
                print("   ✅ Modelos regenerados exitosamente")
                return True
            else:
                print("   ⚠️  Error regenerando modelos:")
                if result.stdout:
                    print(f"   STDOUT: {result.stdout}")
                if result.stderr:
                    print(f"   STDERR: {result.stderr}")
                return False
                
        except Exception as e:
            print(f"   ❌ ERROR regenerando modelos: {e}")
            return False
    
    def run(self, table_name, fields=None, foreign_keys=None, unique_fields=None, execute=False, preview=False):
        """Ejecuta el proceso completo"""
        self.print_header()
        
        try:
            # Validar nombre de tabla
            table_name = self.validate_table_name(table_name)
            print(f"📊 Tabla: {table_name}")
            
            # Parsear campos
            parsed_fields = []
            if fields:
                print(f"📝 Campos personalizados: {len(fields)}")
                for field_str in fields:
                    field = self.parse_field(field_str)
                    parsed_fields.append(field)
                    print(f"   • {field['name']}: {field['sql_type']}")
            
            # Parsear Foreign Keys
            parsed_fks = []
            if foreign_keys:
                print(f"🔗 Foreign Keys: {len(foreign_keys)}")
                for fk_str in foreign_keys:
                    fk = self.parse_foreign_key(fk_str)
                    parsed_fks.append(fk)
                    print(f"   • {fk['field']} → {fk['ref_table']}")
            
            # Unique fields
            if unique_fields:
                print(f"🔒 Campos únicos: {', '.join(unique_fields)}")
            
            print()
            
            # Generar SQL
            sql = self.generate_sql(table_name, parsed_fields, parsed_fks, unique_fields)
            
            if preview:
                print("📋 SQL GENERADO:")
                print("=" * 70)
                print(sql)
                print("=" * 70)
                return True
            
            if execute:
                # Leer connection string
                connection_string = self.read_connection_string()
                if not connection_string:
                    return False
                
                # Ejecutar SQL
                if self.execute_sql(sql, connection_string):
                    # Regenerar modelos
                    self.regenerate_models()
                    
                    print("\n🎉 PROCESO COMPLETADO EXITOSAMENTE")
                    print(f"✅ Tabla '{table_name}' creada en base de datos")
                    print(f"✅ Modelos .NET actualizados")
                    print(f"✅ Listo para usar: QueryService.For<{table_name.title()}>()...")
                    return True
                else:
                    return False
            else:
                print("📋 SQL GENERADO (usar --execute para crear en BD):")
                print("=" * 70)
                print(sql)
                print("=" * 70)
                print("\n💡 Para ejecutar en base de datos:")
                print(f"   python tools/db/table.py --name \"{table_name}\" --execute")
                return True
                
        except Exception as e:
            print(f"\n❌ ERROR: {e}")
            return False

def main():
    parser = argparse.ArgumentParser(description='🛠️ Database Table Generator')
    parser.add_argument('--name', required=True, 
                       help='Nombre de la tabla (requerido)')
    parser.add_argument('--fields', nargs='*', default=[],
                       help='Campos adicionales: "nombre:tipo:tamaño"')
    parser.add_argument('--fk', nargs='*', default=[],
                       help='Foreign keys: "campo:tabla_referencia"')
    parser.add_argument('--unique', nargs='*', default=[],
                       help='Campos únicos')
    parser.add_argument('--execute', action='store_true',
                       help='Ejecutar en base de datos')
    parser.add_argument('--preview', action='store_true',
                       help='Solo mostrar SQL generado')
    parser.add_argument('--project', default='Backend',
                       help='Ruta al proyecto Backend (default: Backend)')
    
    args = parser.parse_args()
    
    generator = DatabaseTableGenerator(args.project)
    
    try:
        success = generator.run(
            table_name=args.name,
            fields=args.fields,
            foreign_keys=args.fk,
            unique_fields=args.unique,
            execute=args.execute,
            preview=args.preview
        )
        sys.exit(0 if success else 1)
    except KeyboardInterrupt:
        print("\n\n⏹️  Proceso cancelado por el usuario")
        sys.exit(1)
    except Exception as e:
        print(f"\n❌ ERROR inesperado: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()