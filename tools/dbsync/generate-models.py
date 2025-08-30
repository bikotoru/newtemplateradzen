#!/usr/bin/env python3
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
    
    def clean_previous_files(self):
        """Limpia archivos anteriores"""
        print("\n🧹 LIMPIANDO ARCHIVOS ANTERIORES")
        print("-" * 40)
        
        # Limpiar entidades anteriores
        if self.entities_path.exists():
            print(f"   🗑️  Eliminando entidades anteriores: {self.entities_path}")
            shutil.rmtree(self.entities_path)
            
        # Limpiar DbContext anterior en Backend.Utils
        if self.data_path.exists():
            print(f"   🗑️  Eliminando DbContext anterior: {self.data_path}")
            shutil.rmtree(self.data_path)
            
        # Crear directorios
        self.entities_path.mkdir(parents=True, exist_ok=True)
        self.data_path.mkdir(parents=True, exist_ok=True)
        
        print("   ✅ Limpieza completada")
    
    def generate_from_database(self, connection_string):
        """Genera modelos usando EF Core CLI"""
        print("\n🏗️  GENERANDO DESDE BASE DE DATOS")
        print("-" * 40)
        
        # Cambiar al directorio Backend.Utils para generar
        original_cwd = os.getcwd()
        os.chdir(self.backend_utils_path)
        
        try:
            # Comando para generar modelos
            cmd = [
                "dotnet", "ef", "dbcontext", "scaffold",
                connection_string,
                "Microsoft.EntityFrameworkCore.SqlServer",
                "-o", "TempEntities",
                "-c", "AppDbContext",
                "--context-dir", "TempData",
                "--force",
                "--no-onconfiguring"
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
    
    def organize_files(self):
        """Organiza los archivos generados en la estructura correcta"""
        print("\n📁 ORGANIZANDO ARCHIVOS")
        print("-" * 40)
        
        temp_entities_path = self.backend_utils_path / "TempEntities"
        temp_data_path = self.backend_utils_path / "TempData"
        
        # Mover entidades a Shared.Models
        if temp_entities_path.exists():
            entity_files = list(temp_entities_path.glob("*.cs"))
            print(f"   📦 Moviendo {len(entity_files)} entidades a Shared.Models/Entities/")
            for file in entity_files:
                destination = self.entities_path / file.name
                shutil.move(str(file), str(destination))
                print(f"      ✅ {file.name}")
                
        # Mover DbContext a Backend.Utils/Data
        if temp_data_path.exists():
            context_files = list(temp_data_path.glob("*.cs"))
            print(f"   🗄️  Moviendo {len(context_files)} archivo(s) DbContext a Backend.Utils/Data/")
            for file in context_files:
                destination = self.data_path / file.name
                shutil.move(str(file), str(destination))
                print(f"      ✅ {file.name}")
        
        # Limpiar directorios temporales
        if temp_entities_path.exists():
            shutil.rmtree(temp_entities_path)
        if temp_data_path.exists():
            shutil.rmtree(temp_data_path)
            
        print("   ✅ Organización completada")
    
    def fix_namespaces(self):
        """Ajusta los namespaces de los archivos generados"""
        print("\n🔧 AJUSTANDO NAMESPACES")
        print("-" * 40)
        
        # Ajustar namespaces en entidades
        entity_files = list(self.entities_path.glob("*.cs"))
        print(f"   📝 Ajustando namespaces en {len(entity_files)} entidades...")
        
        for file in entity_files:
            content = file.read_text(encoding='utf-8')
            # Cambiar namespace
            content = re.sub(r'namespace Backend\.Utils\.TempEntities', 'namespace Shared.Models.Entities', content)
            content = re.sub(r'namespace Backend\.Utils\.TempEntities;', 'namespace Shared.Models.Entities;', content)
            file.write_text(content, encoding='utf-8')
        
        # Ajustar namespaces en DbContext
        context_files = list(self.data_path.glob("*.cs"))
        print(f"   🗄️  Ajustando namespaces en {len(context_files)} archivo(s) DbContext...")
        
        for file in context_files:
            content = file.read_text(encoding='utf-8')
            # Cambiar namespace
            content = re.sub(r'namespace Backend\.Utils\.TempData', 'namespace Backend.Utils.Data', content)
            content = re.sub(r'namespace Backend\.Utils\.TempData;', 'namespace Backend.Utils.Data;', content)
            # Cambiar using statements
            content = re.sub(r'using Backend\.Utils\.TempEntities;', 'using Shared.Models.Entities;', content)
            content = re.sub(r'using Backend\.Utils\.TempEntities', 'using Shared.Models.Entities', content)
            file.write_text(content, encoding='utf-8')
            
        print("   ✅ Namespaces ajustados correctamente")
    
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
        print(f"   2. Configura tu Backend para usar: Backend.Utils.Data.AppDbContext")
        print(f"   3. Registra el DbContext en Program.cs del Backend")
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
            
        # 2. Limpiar archivos anteriores
        self.clean_previous_files()
        
        # 3. Generar desde BD
        if not self.generate_from_database(connection_string):
            return False
            
        # 4. Organizar archivos
        self.organize_files()
        
        # 5. Ajustar namespaces
        self.fix_namespaces()
        
        # 6. Compilar para verificar
        self.compile_solution()
        
        # 7. Mostrar resumen
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
        print("\n\n⏹️  Proceso cancelado por el usuario")
        sys.exit(1)
    except Exception as e:
        print(f"\n❌ ERROR inesperado: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()