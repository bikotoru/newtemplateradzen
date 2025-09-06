#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
🐍 Database-First Model Generator for .NET + Blazor
Reads from launchSettings.json and generates models automatically

Usage:
    python generate-models.py                    # Uses current Backend
    python generate-models.py --project ../OtroBackend
"""

import os
import sys
import json
import shutil
import subprocess
import argparse
from pathlib import Path
import re

# Configurar encoding UTF-8 para Windows
if sys.platform == "win32":
    import codecs
    sys.stdout = codecs.getwriter('utf-8')(sys.stdout.buffer)
    sys.stderr = codecs.getwriter('utf-8')(sys.stderr.buffer)

class DatabaseModelGenerator:
    def __init__(self, project_path="Backend"):
        self.project_path = Path(project_path)
        self.root_path = Path.cwd()
        self.shared_models_path = self.root_path / "Shared.Models"
        self.backend_utils_path = self.root_path / "Backend.Utils"
        self.entities_path = self.shared_models_path / "Entities"
        self.data_path = self.backend_utils_path / "Data"
        
    def print_header(self):
        print("=" * 60)
        print("🐍  DATABASE-FIRST MODEL GENERATOR")
        print("=" * 60)
        print()
    
    def read_connection_string(self):
        """Lee la connection string desde launchSettings.json"""
        launch_settings_path = self.project_path / "Properties" / "launchSettings.json"
        
        if not launch_settings_path.exists():
            print(f"❌ ERROR: No se encontró launchSettings.json en {launch_settings_path}")
            print("   Asegúrate de que el proyecto existe y tiene la estructura correcta")
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
                    print(f"   📄 Archivo: {launch_settings_path}")
                    print(f"   🔗 Conexión: {sql_connection[:50]}...")
                    return sql_connection
                    
            print(f"❌ ERROR: No se encontró variable 'SQL' en environmentVariables")
            print(f"   📄 Archivo: {launch_settings_path}")
            print("   💡 Ejemplo de configuración:")
            print('   "environmentVariables": {')
            print('     "ASPNETCORE_ENVIRONMENT": "Development",')
            print('     "SQL": "Server=localhost;Database=MiTienda;Trusted_Connection=true;"')
            print('   }')
            return None
            
        except json.JSONDecodeError as e:
            print(f"❌ ERROR: El archivo launchSettings.json no es válido: {e}")
            return None
        except Exception as e:
            print(f"❌ ERROR leyendo launchSettings.json: {e}")
            return None
    
    def prepare_directories(self):
        """Prepara directorios necesarios sin eliminar archivos existentes"""
        print("\n📁 PREPARANDO DIRECTORIOS")
        print("-" * 40)
        
        # Crear directorios si no existen (sin eliminar contenido)
        self.entities_path.mkdir(parents=True, exist_ok=True)
        self.data_path.mkdir(parents=True, exist_ok=True)
        
        print("   ✅ Directorios preparados (archivos existentes se sobrescribirán con --force)")
    
    def generate_from_database(self, connection_string):
        """Genera modelos usando EF Core CLI con parámetros correctos"""
        print("\n🏗️  GENERANDO DESDE BASE DE DATOS")
        print("-" * 40)
        
        # Cambiar al directorio Backend.Utils para generar
        original_cwd = os.getcwd()
        os.chdir(self.backend_utils_path)
        
        try:
            # Comando con parámetros correctos para generar directamente en ubicaciones finales
            cmd = [
                "dotnet", "ef", "dbcontext", "scaffold",
                connection_string,
                "Microsoft.EntityFrameworkCore.SqlServer",
                "--output-dir", "../Shared.Models/Entities",
                "--context-dir", "Data", 
                "--namespace", "Shared.Models.Entities",
                "--context-namespace", "Backend.Utils.Data",
                "--context", "AppDbContext",
                "--force",
                "--no-onconfiguring", 
                "--no-pluralize"
            ]
            
            print(f"   🔧 Ejecutando: dotnet ef dbcontext scaffold...")
            print(f"   📂 Directorio: {self.backend_utils_path}")
            
            result = subprocess.run(cmd, capture_output=True, text=True)
            
            if result.returncode != 0:
                print(f"❌ ERROR ejecutando EF Core CLI:")
                print(f"   STDOUT: {result.stdout}")
                print(f"   STDERR: {result.stderr}")
                print("\n💡 Posibles soluciones:")
                print("   - Verifica que SQL Server esté ejecutándose")
                print("   - Verifica que la base de datos exista")
                print("   - Verifica la connection string")
                return False
                
            print("   ✅ Generación completada exitosamente")
            return True
            
        except FileNotFoundError:
            print("❌ ERROR: dotnet ef no está instalado o no se encuentra en PATH")
            print("   💡 Instálalo con: dotnet tool install --global dotnet-ef")
            return False
        except Exception as e:
            print(f"❌ ERROR generando modelos: {e}")
            return False
        finally:
            os.chdir(original_cwd)
    
    
    def compile_solution(self):
        """Compila la solución para verificar que todo funciona"""
        print("\n🔨 COMPILANDO SOLUCIÓN")
        print("-" * 40)
        
        try:
            result = subprocess.run(["dotnet", "build"], capture_output=True, text=True, cwd=self.root_path)
            
            if result.returncode == 0:
                print("   ✅ Compilación exitosa")
                return True
            else:
                print("   ⚠️  Advertencia: Errores de compilación encontrados")
                print(f"   STDOUT: {result.stdout}")
                print(f"   STDERR: {result.stderr}")
                return False
                
        except Exception as e:
            print(f"   ❌ Error compilando: {e}")
            return False
    
    def show_summary(self):
        """Muestra un resumen de los archivos generados"""
        print("\n📋 RESUMEN DE ARCHIVOS GENERADOS")
        print("=" * 60)
        
        # Listar entidades
        entity_files = list(self.entities_path.glob("*.cs"))
        if entity_files:
            print(f"\n📦 Entidades generadas ({len(entity_files)}):")
            print(f"   📂 {self.entities_path}")
            for file in entity_files:
                print(f"   ✅ {file.name}")
        
        # Listar DbContext
        context_files = list(self.data_path.glob("*.cs"))
        if context_files:
            print(f"\n🗄️  DbContext generado ({len(context_files)}):")
            print(f"   📂 {self.data_path}")
            for file in context_files:
                print(f"   ✅ {file.name}")
        
        print(f"\n🎯 PRÓXIMOS PASOS:")
        print(f"   1. Verifica los modelos generados en: {self.entities_path}")
        print(f"   2. DbContext generado automáticamente con namespaces correctos")
        print(f"   3. ¡Las entidades están listas para usar!")
        print(f"   4. ¡Disfruta tus queries type-safe!")
        
    def run(self):
        """Ejecuta el proceso completo"""
        self.print_header()
        
        print(f"🚀 Proyecto: {self.project_path}")
        print(f"📂 Directorio raíz: {self.root_path}")
        print()
        
        # 1. Leer connection string
        connection_string = self.read_connection_string()
        if not connection_string:
            return False
            
        # 2. Preparar directorios (sin limpiar)
        self.prepare_directories()
        
        # 3. Generar desde BD
        if not self.generate_from_database(connection_string):
            return False
            
        # 4. Compilar para verificar
        self.compile_solution()
        
        # 5. Mostrar resumen
        self.show_summary()
        
        print("\n🎉 GENERACIÓN COMPLETADA EXITOSAMENTE")
        return True

def main():
    parser = argparse.ArgumentParser(description='🐍 Database-First Model Generator')
    parser.add_argument('--project', default='Backend', 
                       help='Ruta al proyecto Backend (default: Backend)')
    
    args = parser.parse_args()
    
    generator = DatabaseModelGenerator(args.project)
    
    try:
        success = generator.run()
        sys.exit(0 if success else 1)
    except KeyboardInterrupt:
        print("\n\nProceso cancelado por el usuario")
        sys.exit(1)
    except Exception as e:
        print(f"\nERROR inesperado: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()