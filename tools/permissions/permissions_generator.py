#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
🔐 Permissions Generator
Genera permisos del sistema para una entidad en la base de datos
"""

import sys
import os
import uuid
import argparse
import json
from datetime import datetime
from pathlib import Path
import pyodbc

# Configurar encoding UTF-8 para Windows
if sys.platform == "win32":
    import codecs
    sys.stdout = codecs.getwriter('utf-8')(sys.stdout.buffer)
    sys.stderr = codecs.getwriter('utf-8')(sys.stderr.buffer)

class PermissionsGenerator:
    def __init__(self, project_path="Backend"):
        # Configuración de conexión - lee de launchSettings.json como table.py
        self.connection_string = None
        self.root_path = Path.cwd()
        
        # Si corremos desde tools/, ir a la raíz del proyecto
        if self.root_path.name == "permissions":
            self.root_path = self.root_path.parent.parent
        elif self.root_path.name == "tools":
            self.root_path = self.root_path.parent
        
        self.project_path = self.root_path / project_path
        
        # Permisos por defecto para cada entidad
        self.default_permissions = [
            {
                'action': 'CREATE',
                'description': 'Crear {entity_plural_lower}'
            },
            {
                'action': 'VIEW',
                'description': 'Ver {entity_plural_lower}'
            },
            {
                'action': 'UPDATE',
                'description': 'Actualizar {entity_plural_lower}'
            },
            {
                'action': 'DELETE',
                'description': 'Eliminar {entity_plural_lower}'
            },
            {
                'action': 'VIEWMENU',
                'description': 'Ver menú de {entity_plural_lower}'
            },
            {
                'action': 'RESTORE',
                'description': 'Restaurar {entity_plural_lower}'
            }
        ]
    
    def set_connection_string(self, connection_string):
        """Configurar cadena de conexión personalizada"""
        self.connection_string = connection_string
    
    def read_connection_string(self):
        """Lee la connection string desde launchSettings.json - mismo método que table.py"""
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

    def convert_to_odbc_connection_string(self, ef_connection_string):
        """Convierte una cadena de conexión de Entity Framework a formato ODBC"""
        # Parsear la cadena EF
        parts = {}
        for part in ef_connection_string.split(';'):
            if '=' in part and part.strip():
                key, value = part.split('=', 1)
                parts[key.strip().lower()] = value.strip()
        
        # Extraer componentes
        server = parts.get('server', 'localhost')
        database = parts.get('database', parts.get('initial catalog', 'master'))
        user_id = parts.get('user id', parts.get('uid'))
        password = parts.get('password', parts.get('pwd'))
        trusted_connection = parts.get('trusted_connection', 'false').lower() == 'true'
        
        # Construir cadena ODBC
        if user_id and password:
            return f"Driver={{ODBC Driver 17 for SQL Server}};Server={server};Database={database};UID={user_id};PWD={password};"
        elif trusted_connection:
            return f"Driver={{ODBC Driver 17 for SQL Server}};Server={server};Database={database};Trusted_Connection=yes;"
        else:
            # Default a autenticación SQL con las credenciales encontradas
            return f"Driver={{ODBC Driver 17 for SQL Server}};Server={server};Database={database};UID={user_id or 'sa'};PWD={password or ''};"

    def get_connection_string(self):
        """Obtener cadena de conexión - mismo patrón que table.py"""
        if self.connection_string:
            return self.connection_string
        
        # Prioridad 1: Leer desde launchSettings.json (como table.py)
        ef_connection_string = self.read_connection_string()
        if ef_connection_string:
            return self.convert_to_odbc_connection_string(ef_connection_string)
            
        # Prioridad 2: Variables de entorno (fallback)
        print("⚠️ Usando configuración de variables de entorno como fallback")
        db_server = os.getenv('DATABASE_SERVER', 'localhost')
        db_name = os.getenv('DATABASE_NAME', 'NuevoProyectoDB')
        db_user = os.getenv('DATABASE_USER')
        db_password = os.getenv('DATABASE_PASSWORD')
        
        if db_user and db_password:
            return f"Driver={{ODBC Driver 17 for SQL Server}};Server={db_server};Database={db_name};UID={db_user};PWD={db_password};"
        else:
            return f"Driver={{ODBC Driver 17 for SQL Server}};Server={db_server};Database={db_name};Trusted_Connection=yes;"
    
    def get_organization_id(self, cursor):
        """Obtener el OrganizationId de la base de datos (asume que hay uno por defecto)"""
        try:
            cursor.execute("SELECT TOP 1 Id FROM system_organizations WHERE Active = 1")
            row = cursor.fetchone()
            if row:
                return str(row[0])
            else:
                # Si no hay organizaciones, crear una por defecto
                print("⚠️ No se encontró una organización activa, usando GUID por defecto")
                return "F5B94C07-FAE1-4A2B-90AB-B73D4AAD67DC"
        except Exception:
            # Fallback si la tabla no existe o hay error
            return "F5B94C07-FAE1-4A2B-90AB-B73D4AAD67DC"
    
    def permission_exists(self, cursor, action_key):
        """Verificar si el permiso ya existe"""
        cursor.execute("SELECT COUNT(*) FROM system_permissions WHERE ActionKey = ?", action_key)
        count = cursor.fetchone()[0]
        return count > 0
    
    def generate_permissions(self, entity_name, entity_plural=None, preview=False):
        """Generar permisos para una entidad"""
        
        # Generar plural si no se proporciona
        if not entity_plural:
            entity_plural = entity_name + "s"
        
        entity_upper = entity_name.upper()
        entity_plural_lower = entity_plural.lower()
        
        print(f"🔐 Generando permisos para entidad: {entity_name}")
        print(f"📝 Plural: {entity_plural}")
        print(f"🏷️ Grupo: {entity_upper}")
        print()
        
        # Preparar datos
        now = datetime.now()
        permissions_to_create = []
        
        try:
            # Conectar a la base de datos
            connection_string = self.get_connection_string()
            print(f"🔌 Conectando a base de datos...")
            
            # Mostrar info de conexión (sin credenciales)  
            if "UID=" in connection_string:
                print(f"🔑 Usando autenticación SQL")
            else:
                print(f"🔑 Usando autenticación de Windows")
            
            if preview:
                print("👀 MODO PREVIEW - No se ejecutarán cambios")
                organization_id = "F5B94C07-FAE1-4A2B-90AB-B73D4AAD67DC"
            else:
                conn = pyodbc.connect(connection_string)
                cursor = conn.cursor()
                organization_id = self.get_organization_id(cursor)
            
            print(f"🏢 Organization ID: {organization_id}")
            print()
            
            # Generar cada permiso
            for perm_template in self.default_permissions:
                action_key = f"{entity_upper}.{perm_template['action']}"
                permission_name = action_key
                description = perm_template['description'].format(entity_plural_lower=entity_plural_lower)
                
                permission_data = {
                    'id': str(uuid.uuid4()).upper(),
                    'name': permission_name,
                    'description': description,
                    'action_key': action_key,
                    'group_key': entity_upper,
                    'group_name': entity_plural,
                    'organization_id': organization_id,
                    'fecha_creacion': now,
                    'fecha_modificacion': now
                }
                
                # Verificar si ya existe (solo si no es preview)
                if not preview:
                    if self.permission_exists(cursor, action_key):
                        print(f"⚠️ Ya existe: {action_key}")
                        continue
                
                permissions_to_create.append(permission_data)
                print(f"✅ Preparado: {action_key} - {description}")
            
            print()
            
            if not permissions_to_create:
                print("💡 Todos los permisos ya existen. No hay nada que crear.")
                return True
            
            print(f"📊 Total a crear: {len(permissions_to_create)} permisos")
            
            if preview:
                print()
                print("📋 SQL QUE SE EJECUTARÍA:")
                print("-" * 60)
                for perm in permissions_to_create:
                    sql = f"""INSERT INTO [dbo].[system_permissions] 
([Id], [Nombre], [Descripcion], [FechaCreacion], [FechaModificacion], 
[OrganizationId], [CreadorId], [ModificadorId], [Active], [ActionKey], [GroupKey], [GrupoNombre]) 
VALUES ('{perm['id']}', N'{perm['name']}', N'{perm['description']}', 
'{perm['fecha_creacion'].strftime('%Y-%m-%d %H:%M:%S.%f')}', 
'{perm['fecha_modificacion'].strftime('%Y-%m-%d %H:%M:%S.%f')}', 
'{perm['organization_id']}', NULL, NULL, '1', '{perm['action_key']}', 
'{perm['group_key']}', N'{perm['group_name']}');"""
                    print(sql)
                    print()
                return True
            
            # Ejecutar inserts
            print("💾 Ejecutando inserts...")
            
            for perm in permissions_to_create:
                sql = """INSERT INTO [dbo].[system_permissions] 
([Id], [Nombre], [Descripcion], [FechaCreacion], [FechaModificacion], 
[OrganizationId], [CreadorId], [ModificadorId], [Active], [ActionKey], [GroupKey], [GrupoNombre]) 
VALUES (?, ?, ?, ?, ?, ?, NULL, NULL, '1', ?, ?, ?)"""
                
                cursor.execute(sql, 
                    perm['id'], 
                    perm['name'], 
                    perm['description'],
                    perm['fecha_creacion'], 
                    perm['fecha_modificacion'],
                    perm['organization_id'],
                    perm['action_key'],
                    perm['group_key'],
                    perm['group_name']
                )
            
            # Confirmar cambios
            conn.commit()
            conn.close()
            
            print()
            print("🎉 PERMISOS CREADOS EXITOSAMENTE!")
            print(f"✅ {len(permissions_to_create)} permisos insertados en system_permissions")
            print()
            print("📋 PERMISOS CREADOS:")
            for perm in permissions_to_create:
                print(f"   • {perm['action_key']} - {perm['description']}")
            
            return True
            
        except Exception as e:
            print(f"❌ ERROR: {e}")
            return False

def main():
    parser = argparse.ArgumentParser(description='🔐 Permissions Generator - Crear permisos de sistema')
    
    parser.add_argument('--entity', required=True,
                       help='Nombre de la entidad (ej: Marca, Categoria)')
    parser.add_argument('--plural',
                       help='Plural de la entidad (ej: Marcas, Categorias)')
    parser.add_argument('--preview', action='store_true',
                       help='Solo mostrar SQL sin ejecutar')
    parser.add_argument('--connection-string',
                       help='Cadena de conexión personalizada')
    
    args = parser.parse_args()
    
    generator = PermissionsGenerator()
    
    if args.connection_string:
        generator.set_connection_string(args.connection_string)
    
    try:
        success = generator.generate_permissions(
            entity_name=args.entity,
            entity_plural=args.plural,
            preview=args.preview
        )
        sys.exit(0 if success else 1)
    except KeyboardInterrupt:
        print("\n\n⏹️ Proceso cancelado por el usuario")
        sys.exit(1)
    except Exception as e:
        print(f"\n❌ ERROR inesperado: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()