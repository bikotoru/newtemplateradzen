#!/usr/bin/env python3
"""
🎯 Custom Validator Tool for Entity Metadata
Manages custom attributes for entities without touching EF Core generated code

Usage:
    python customvalidator.py sistema_usuarios Nombre SoloCrear
    python customvalidator.py categoria Descripcion SoloCrear
    python customvalidator.py system_organization_test Rut SoloCrear

Features:
- Converts table names to EF Core entity names (system_users -> SystemUsers)
- Creates .Metadata.cs files if they don't exist
- Adds attributes to existing metadata files
- Prevents duplicate attributes
- Supports multiple attributes per field
"""

import os
import sys
import re
import argparse
import json
from pathlib import Path
from typing import List, Dict, Optional, Tuple
try:
    import pyodbc
    PYODBC_AVAILABLE = True
except ImportError:
    PYODBC_AVAILABLE = False

class EntityMetadataManager:
    def __init__(self):
        self.root_path = Path.cwd()
        self.shared_models_path = self.root_path / "Shared.Models"
        self.entities_path = self.shared_models_path / "Entities"
        self.attributes_namespace = "Shared.Models.Attributes"
        self.connection_string = self.get_connection_string()
        
        # Mapeo de atributos disponibles
        self.available_attributes = {
            "SoloCrear": {
                "class_name": "SoloCrearAttribute",
                "usage": "[SoloCrear]",
                "description": "Campo solo modificable durante creación"
            },
            "AutoIncremental": {
                "class_name": "AutoIncrementalAttribute",
                "usage": "[AutoIncremental]",
                "description": "Campo con numeración automática incremental"
            },
            "NoSelect": {
                "class_name": "NoSelectAttribute",
                "usage": "[NoSelect]",
                "description": "Campo que se devuelve como null en consultas (para datos sensibles)"
            },
            "FieldPermission": {
                "class_name": "FieldPermissionAttribute",
                "usage": "[FieldPermission(CREATE=\"ENTIDAD.CAMPO.CREATE\", UPDATE=\"ENTIDAD.CAMPO.EDIT\", VIEW=\"ENTIDAD.CAMPO.VIEW\")]",
                "description": "Campo protegido por permisos granulares para CREATE/UPDATE/VIEW"
            },
            "Auditar": {
                "class_name": "AuditarAttribute",
                "usage": "[Auditar]",
                "description": "Campo que será auditado automáticamente - cambios se registran en system_auditoria"
            }
        }
    
    def print_header(self):
        print("=" * 60)
        print("🎯  CUSTOM VALIDATOR TOOL")
        print("   Entity Metadata Manager")
        print("=" * 60)
        print()
    
    def table_name_to_entity_name(self, table_name: str) -> str:
        """
        Convierte nombre de tabla a nombre de entidad siguiendo convenciones de EF Core
        
        Examples:
            system_users -> SystemUsers
            categoria -> Categoria
            system_organization_test -> SystemOrganizationTest
        """
        # Dividir por guiones bajos
        parts = table_name.split('_')
        
        # Capitalizar cada parte
        capitalized_parts = [part.capitalize() for part in parts]
        
        # Unir todas las partes
        entity_name = ''.join(capitalized_parts)
        
        return entity_name
    
    def get_metadata_file_path(self, entity_name: str) -> Path:
        """Obtiene la ruta del archivo .Metadata.cs"""
        # Buscar el archivo de entidad para determinar la ubicación
        entity_file = self.get_entity_file_path(entity_name)
        
        # Si el archivo de entidad existe, crear el metadata en el mismo directorio
        if entity_file.exists():
            return entity_file.parent / f"{entity_name}.Metadata.cs"
        
        # Si no existe, usar el directorio raíz de entidades
        return self.entities_path / f"{entity_name}.Metadata.cs"
    
    def get_entity_file_path(self, entity_name: str) -> Path:
        """Obtiene la ruta del archivo de entidad generado por EF Core"""
        # Buscar primero en el directorio raíz de entidades
        entity_file = self.entities_path / f"{entity_name}.cs"
        if entity_file.exists():
            return entity_file
        
        # Buscar en subdirectorios
        for subdirectory in self.entities_path.iterdir():
            if subdirectory.is_dir():
                entity_file = subdirectory / f"{entity_name}.cs"
                if entity_file.exists():
                    return entity_file
        
        # Si no se encuentra, devolver la ruta por defecto
        return self.entities_path / f"{entity_name}.cs"
    
    def entity_exists(self, entity_name: str) -> bool:
        """Verifica si la entidad existe (archivo generado por EF Core)"""
        return self.get_entity_file_path(entity_name).exists()
    
    def parse_existing_metadata(self, metadata_file: Path) -> Dict:
        """Parse archivo metadata existente para extraer información"""
        if not metadata_file.exists():
            return {"fields": {}, "imports": set(), "existing_content": ""}
        
        content = metadata_file.read_text(encoding='utf-8')
        
        # Extraer imports existentes
        imports = set()
        import_pattern = r'using\s+([^;]+);'
        for match in re.finditer(import_pattern, content):
            imports.add(match.group(1).strip())
        
        # Extraer campos con atributos existentes
        fields = {}
        
        # Buscar la clase metadata
        metadata_class_pattern = r'public\s+class\s+\w+Metadata\s*\{([^}]*)\}'
        metadata_match = re.search(metadata_class_pattern, content, re.DOTALL)
        
        if metadata_match:
            metadata_content = metadata_match.group(1)
            
            # Extraer campos con sus atributos
            field_pattern = r'(\[[^\]]+\]\s*)*\s*public\s+\w+\s+(\w+);'
            
            for match in re.finditer(field_pattern, metadata_content, re.MULTILINE):
                attributes_str = match.group(1) or ""
                field_name = match.group(2)
                
                # Extraer atributos individuales
                attr_pattern = r'\[([^\]]+)\]'
                attributes = re.findall(attr_pattern, attributes_str)
                
                fields[field_name] = attributes
        
        return {
            "fields": fields,
            "imports": imports,
            "existing_content": content
        }
    
    def create_metadata_file(self, entity_name: str, field_name: str, attribute: str) -> bool:
        """Crea un nuevo archivo .Metadata.cs"""
        metadata_file = self.get_metadata_file_path(entity_name)
        
        # Verificar que la entidad existe
        if not self.entity_exists(entity_name):
            print(f"❌ ERROR: La entidad {entity_name} no existe en {self.entities_path}")
            print(f"   Archivos disponibles:")
            for file in self.entities_path.glob("*.cs"):
                if not file.name.endswith(".Metadata.cs"):
                    print(f"   - {file.stem}")
            return False
        
        attribute_info = self.available_attributes.get(attribute)
        if not attribute_info:
            print(f"❌ ERROR: Atributo '{attribute}' no disponible")
            print(f"   Atributos disponibles: {list(self.available_attributes.keys())}")
            return False
        
        # Determinar el namespace basado en la ubicación del archivo
        entity_file = self.get_entity_file_path(entity_name)
        namespace = "Shared.Models.Entities"
        
        if entity_file.exists():
            # Leer el archivo de entidad para extraer el namespace
            entity_content = entity_file.read_text(encoding='utf-8')
            namespace_match = re.search(r'namespace\s+([\w.]+);', entity_content)
            if namespace_match:
                namespace = namespace_match.group(1)

        # Contenido del archivo
        content = f"""using System;
using System.ComponentModel.DataAnnotations;
using {self.attributes_namespace};

namespace {namespace}
{{
    [MetadataType(typeof({entity_name}Metadata))]
    public partial class {entity_name} {{ }}

    public class {entity_name}Metadata
    {{
        [{attribute}]
        public string {field_name};
    }}
}}
"""
        
        # Crear el archivo
        metadata_file.write_text(content, encoding='utf-8')
        
        print(f"✅ Archivo metadata creado: {metadata_file.name}")
        print(f"   🎯 Entidad: {entity_name}")
        print(f"   📝 Campo: {field_name}")
        print(f"   🏷️  Atributo: [{attribute}]")
        
        return True
    
    def update_metadata_file(self, entity_name: str, field_name: str, attribute: str) -> bool:
        """Actualiza un archivo .Metadata.cs existente"""
        metadata_file = self.get_metadata_file_path(entity_name)
        
        # Parse contenido existente
        existing_data = self.parse_existing_metadata(metadata_file)
        
        # Verificar si el campo ya tiene este atributo
        existing_attributes = existing_data["fields"].get(field_name, [])
        if attribute in existing_attributes:
            print(f"⚠️  El campo '{field_name}' ya tiene el atributo [{attribute}]")
            print(f"   Atributos actuales: {existing_attributes}")
            return False
        
        # Leer contenido actual
        content = metadata_file.read_text(encoding='utf-8')
        
        # Buscar la clase metadata
        metadata_class_pattern = r'(public\s+class\s+\w+Metadata\s*\{)([^}]*)(^\s*\})'
        match = re.search(metadata_class_pattern, content, re.MULTILINE | re.DOTALL)
        
        if not match:
            print(f"❌ ERROR: No se pudo encontrar la clase Metadata en {metadata_file.name}")
            return False
        
        class_start = match.group(1)
        class_content = match.group(2)
        class_end = match.group(3)
        
        # Verificar si el campo ya existe
        field_pattern = rf'(\[[^\]]+\]\s*)*\s*public\s+\w+\s+{field_name};'
        field_match = re.search(field_pattern, class_content)
        
        if field_match:
            # Campo existe, agregar atributo
            existing_field_line = field_match.group(0)
            new_field_line = f"        [{attribute}]\n        {existing_field_line.strip()}"
            
            # Reemplazar en el contenido de la clase
            new_class_content = class_content.replace(existing_field_line, new_field_line)
        else:
            # Campo no existe, agregarlo
            new_field = f"\n        [{attribute}]\n        public string {field_name};\n"
            new_class_content = class_content + new_field
        
        # Reconstruir contenido completo
        new_content = content.replace(match.group(0), class_start + new_class_content + class_end)
        
        # Escribir archivo actualizado
        metadata_file.write_text(new_content, encoding='utf-8')
        
        print(f"✅ Archivo metadata actualizado: {metadata_file.name}")
        print(f"   🎯 Entidad: {entity_name}")
        print(f"   📝 Campo: {field_name}")
        print(f"   🏷️  Atributo: [{attribute}] (agregado)")
        
        return True
    
    def add_attribute(self, table_name: str, field_name: str, attribute: str) -> bool:
        """Punto de entrada principal para agregar atributo (modo simple)"""
        
        # Convertir nombre de tabla a entidad
        entity_name = self.table_name_to_entity_name(table_name)
        metadata_file = self.get_metadata_file_path(entity_name)
        
        print(f"🔄 Procesando:")
        print(f"   📊 Tabla: {table_name}")
        print(f"   🏗️  Entidad: {entity_name}")
        print(f"   📝 Campo: {field_name}")
        print(f"   🏷️  Atributo: [{attribute}]")
        print()
        
        # Verificar si el atributo existe
        if attribute not in self.available_attributes:
            print(f"❌ ERROR: Atributo '{attribute}' no disponible")
            print(f"   Atributos disponibles:")
            for attr_name, attr_info in self.available_attributes.items():
                print(f"   - {attr_name}: {attr_info['description']}")
            return False
        
        # Decidir si crear o actualizar
        if metadata_file.exists():
            return self.update_metadata_file(entity_name, field_name, attribute)
        else:
            return self.create_metadata_file(entity_name, field_name, attribute)

    def parse_field_definitions(self, field_args: List[str]) -> Dict[str, Dict[str, List[str]]]:
        """
        Parse argumentos con formato: entidad:campo:atributo1|atributo2 entidad2:campo2:atributo3
        
        Args:
            field_args: ["categoria:Nombre:SoloCrear|Required", "system_users:Email:SoloCrear", "categoria:Descripcion:SoloCrear"]
        
        Returns:
            entities_dict: {
                "categoria": {"Nombre": ["SoloCrear", "Required"], "Descripcion": ["SoloCrear"]},
                "system_users": {"Email": ["SoloCrear"]}
            }
        """
        if not field_args:
            return {}
        
        entities_dict = {}
        
        for field_arg in field_args:
            parts = field_arg.split(':', 2)  # Máximo 3 partes: entidad, campo, atributos
            
            if len(parts) < 3:
                print(f"⚠️  Formato inválido en: {field_arg}")
                print("💡 Formato esperado: entidad:campo:atributo1|atributo2")
                continue
            
            entity_name = parts[0].strip()
            field_name = parts[1].strip()
            attributes_str = parts[2].strip()
            
            # Parse atributos
            attributes = [attr.strip() for attr in attributes_str.split('|') if attr.strip()]
            
            if not entity_name or not field_name or not attributes:
                print(f"⚠️  Datos incompletos en: {field_arg}")
                continue
            
            # Agregar a la estructura
            if entity_name not in entities_dict:
                entities_dict[entity_name] = {}
            
            entities_dict[entity_name][field_name] = attributes
        
        return entities_dict

    def process_multiple_entities(self, entities_dict: Dict[str, Dict[str, List[str]]]) -> bool:
        """Procesar múltiples entidades con sus campos y atributos"""
        
        if not entities_dict:
            print("❌ ERROR: No se pudieron procesar las entidades")
            print("💡 Formato: entidad:campo:atributo1|atributo2 entidad2:campo2:atributo3")
            print("💡 Ejemplo: categoria:Nombre:SoloCrear system_users:Email:SoloCrear")
            return False
        
        print(f"🔄 Procesando múltiples entidades:")
        print(f"   📊 Entidades: {len(entities_dict)}")
        total_fields = sum(len(fields) for fields in entities_dict.values())
        total_operations = sum(sum(len(attrs) for attrs in fields.values()) for fields in entities_dict.values())
        print(f"   📝 Total campos: {total_fields}")
        print(f"   🏷️  Total operaciones: {total_operations}")
        print()
        
        # Validar todos los atributos antes de procesar
        all_attributes = set()
        for entity_name, fields in entities_dict.items():
            for field_name, attributes in fields.items():
                all_attributes.update(attributes)
        
        invalid_attributes = all_attributes - set(self.available_attributes.keys())
        if invalid_attributes:
            print(f"❌ ERROR: Atributos no válidos: {invalid_attributes}")
            print(f"   Atributos disponibles: {list(self.available_attributes.keys())}")
            return False
        
        success_count = 0
        processed_entities = 0
        
        # Procesar cada entidad
        for table_name, fields_dict in entities_dict.items():
            entity_name = self.table_name_to_entity_name(table_name)
            
            print(f"🏗️  Procesando entidad: {table_name} -> {entity_name}")
            
            # Verificar que la entidad existe
            if not self.entity_exists(entity_name):
                print(f"   ❌ ERROR: La entidad {entity_name} no existe")
                continue
            
            entity_success_count = 0
            entity_total = sum(len(attrs) for attrs in fields_dict.values())
            
            # Procesar cada campo de la entidad
            for field_name, attributes in fields_dict.items():
                print(f"   📝 Campo: {field_name}")
                
                for attribute in attributes:
                    # Manejar FieldPermission de manera especial
                    if attribute == "FieldPermission":
                        print(f"      🔐 Configurando FieldPermission interactivamente...")
                        interactive_result = self.handle_field_permission_interactive(entity_name, field_name)
                        
                        if interactive_result:
                            # Usar el resultado interactivo como atributo
                            metadata_file = self.get_metadata_file_path(entity_name)
                            
                            if metadata_file.exists():
                                success = self.update_metadata_file_single_custom(entity_name, field_name, interactive_result)
                            else:
                                success = self.create_metadata_file_single_custom(entity_name, field_name, interactive_result)
                            
                            if success:
                                success_count += 1
                                entity_success_count += 1
                                print(f"         ✅ {field_name}.FieldPermission configurado")
                            else:
                                print(f"         ⚠️  {field_name}.FieldPermission no pudo ser configurado")
                        else:
                            print(f"         ❌ {field_name}.FieldPermission cancelado")
                    else:
                        # Procesar atributos normales
                        print(f"      🏷️  Agregando [{attribute}]...")
                        
                        metadata_file = self.get_metadata_file_path(entity_name)
                        
                        if metadata_file.exists():
                            success = self.update_metadata_file_single(entity_name, field_name, attribute)
                        else:
                            success = self.create_metadata_file_single(entity_name, field_name, attribute)
                        
                        if success:
                            success_count += 1
                            entity_success_count += 1
                            print(f"         ✅ {field_name}.{attribute} agregado")
                        else:
                            print(f"         ⚠️  {field_name}.{attribute} no agregado (ya existe)")
            
            print(f"   📊 Entidad completada: {entity_success_count}/{entity_total}")
            print(f"   📁 Archivo: {self.get_metadata_file_path(entity_name).name}")
            print()
            processed_entities += 1
        
        print(f"📊 RESUMEN FINAL:")
        print(f"   🏗️  Entidades procesadas: {processed_entities}/{len(entities_dict)}")
        print(f"   ✅ Operaciones exitosas: {success_count}/{total_operations}")
        print(f"   📁 Archivos modificados: {processed_entities}")
        
        return success_count > 0

    def create_metadata_file_single(self, entity_name: str, field_name: str, attribute: str) -> bool:
        """Crea archivo metadata para un solo campo/atributo (sin prints)"""
        metadata_file = self.get_metadata_file_path(entity_name)
        
        if metadata_file.exists():
            return self.update_metadata_file_single(entity_name, field_name, attribute)
        
        # Determinar el namespace basado en la ubicación del archivo
        entity_file = self.get_entity_file_path(entity_name)
        namespace = "Shared.Models.Entities"
        
        if entity_file.exists():
            # Leer el archivo de entidad para extraer el namespace
            entity_content = entity_file.read_text(encoding='utf-8')
            namespace_match = re.search(r'namespace\s+([\w.]+);', entity_content)
            if namespace_match:
                namespace = namespace_match.group(1)

        # Contenido del archivo
        content = f"""using System;
using System.ComponentModel.DataAnnotations;
using {self.attributes_namespace};

namespace {namespace}
{{
    [MetadataType(typeof({entity_name}Metadata))]
    public partial class {entity_name} {{ }}

    public class {entity_name}Metadata
    {{
        [{attribute}]
        public string {field_name};
    }}
}}
"""
        
        metadata_file.write_text(content, encoding='utf-8')
        return True

    def update_metadata_file_single(self, entity_name: str, field_name: str, attribute: str) -> bool:
        """Actualiza archivo metadata para un solo campo/atributo (sin prints)"""
        metadata_file = self.get_metadata_file_path(entity_name)
        
        # Parse contenido existente
        existing_data = self.parse_existing_metadata(metadata_file)
        
        # Verificar si el campo ya tiene este atributo
        existing_attributes = existing_data["fields"].get(field_name, [])
        if attribute in existing_attributes:
            return False  # Ya existe
        
        # Leer contenido actual
        content = metadata_file.read_text(encoding='utf-8')
        
        # Buscar la clase metadata
        metadata_class_pattern = r'(public\s+class\s+\w+Metadata\s*\{)([^}]*)(^\s*\})'
        match = re.search(metadata_class_pattern, content, re.MULTILINE | re.DOTALL)
        
        if not match:
            return False
        
        class_start = match.group(1)
        class_content = match.group(2)
        class_end = match.group(3)
        
        # Verificar si el campo ya existe
        field_pattern = rf'(\[[^\]]+\]\s*)*\s*public\s+\w+\s+{field_name};'
        field_match = re.search(field_pattern, class_content)
        
        if field_match:
            # Campo existe, agregar atributo
            existing_field_line = field_match.group(0)
            new_field_line = f"        [{attribute}]\n        {existing_field_line.strip()}"
            new_class_content = class_content.replace(existing_field_line, new_field_line)
        else:
            # Campo no existe, agregarlo
            new_field = f"\n        [{attribute}]\n        public string {field_name};\n"
            new_class_content = class_content + new_field
        
        # Reconstruir contenido completo
        new_content = content.replace(match.group(0), class_start + new_class_content + class_end)
        
        # Escribir archivo actualizado
        metadata_file.write_text(new_content, encoding='utf-8')
        return True

    def create_metadata_file_single_custom(self, entity_name: str, field_name: str, custom_attribute: str) -> bool:
        """Crea archivo metadata para un solo campo/atributo con contenido personalizado (sin prints)"""
        metadata_file = self.get_metadata_file_path(entity_name)
        
        if metadata_file.exists():
            return self.update_metadata_file_single_custom(entity_name, field_name, custom_attribute)
        
        # Determinar el namespace basado en la ubicación del archivo
        entity_file = self.get_entity_file_path(entity_name)
        namespace = "Shared.Models.Entities"
        
        if entity_file.exists():
            # Leer el archivo de entidad para extraer el namespace
            entity_content = entity_file.read_text(encoding='utf-8')
            namespace_match = re.search(r'namespace\s+([\w.]+);', entity_content)
            if namespace_match:
                namespace = namespace_match.group(1)

        # Contenido del archivo
        content = f"""using System;
using System.ComponentModel.DataAnnotations;
using {self.attributes_namespace};

namespace {namespace}
{{
    [MetadataType(typeof({entity_name}Metadata))]
    public partial class {entity_name} {{ }}

    public class {entity_name}Metadata
    {{
        [{custom_attribute}]
        public string {field_name};
    }}
}}
"""
        
        metadata_file.write_text(content, encoding='utf-8')
        return True

    def update_metadata_file_single_custom(self, entity_name: str, field_name: str, custom_attribute: str) -> bool:
        """Actualiza archivo metadata para un solo campo/atributo con contenido personalizado (sin prints)"""
        metadata_file = self.get_metadata_file_path(entity_name)
        
        # Leer contenido actual
        content = metadata_file.read_text(encoding='utf-8')
        
        # Buscar la clase metadata
        metadata_class_pattern = r'(public\s+class\s+\w+Metadata\s*\{)([^}]*)(^\s*\})'
        match = re.search(metadata_class_pattern, content, re.MULTILINE | re.DOTALL)
        
        if not match:
            return False
        
        class_start = match.group(1)
        class_content = match.group(2)
        class_end = match.group(3)
        
        # Buscar si el campo ya existe con FieldPermission
        field_pattern = rf'(\[FieldPermission[^\]]*\]\s*)*\s*public\s+\w+\s+{field_name};'
        field_match = re.search(field_pattern, class_content)
        
        if field_match:
            # Campo existe con FieldPermission, reemplazarlo
            existing_field_line = field_match.group(0)
            new_field_line = f"        [{custom_attribute}]\n        public string {field_name};"
            new_class_content = class_content.replace(existing_field_line, new_field_line)
        else:
            # Verificar si el campo existe sin FieldPermission
            simple_field_pattern = rf'(\[[^\]]+\]\s*)*\s*public\s+\w+\s+{field_name};'
            simple_field_match = re.search(simple_field_pattern, class_content)
            
            if simple_field_match:
                # Campo existe sin FieldPermission, agregar el atributo
                existing_field_line = simple_field_match.group(0)
                new_field_line = f"        [{custom_attribute}]\n        {existing_field_line.strip()}"
                new_class_content = class_content.replace(existing_field_line, new_field_line)
            else:
                # Campo no existe, agregarlo
                new_field = f"\n        [{custom_attribute}]\n        public string {field_name};\n"
                new_class_content = class_content + new_field
        
        # Reconstruir contenido completo
        new_content = content.replace(match.group(0), class_start + new_class_content + class_end)
        
        # Escribir archivo actualizado
        metadata_file.write_text(new_content, encoding='utf-8')
        return True
    
    def list_entities(self):
        """Lista todas las entidades disponibles"""
        print("📋 ENTIDADES DISPONIBLES:")
        print("-" * 40)
        
        if not self.entities_path.exists():
            print("❌ No se encontró el directorio Shared.Models/Entities/")
            return
        
        entity_files = []
        # Buscar en directorio raíz
        for file in self.entities_path.glob("*.cs"):
            if not file.name.endswith(".Metadata.cs"):
                entity_files.append(file.stem)
        
        # Buscar en subdirectorios
        for subdirectory in self.entities_path.iterdir():
            if subdirectory.is_dir():
                for file in subdirectory.glob("*.cs"):
                    if not file.name.endswith(".Metadata.cs"):
                        entity_files.append(file.stem)
        
        if not entity_files:
            print("❌ No se encontraron entidades")
            return
        
        for entity in sorted(entity_files):
            metadata_file = self.get_metadata_file_path(entity)
            has_metadata = "✅" if metadata_file.exists() else "⭕"
            print(f"   {has_metadata} {entity}")
        
        print(f"\n📊 Total: {len(entity_files)} entidades")
        print("✅ = Tiene archivo .Metadata.cs")
        print("⭕ = Sin archivo .Metadata.cs")

    def get_connection_string(self) -> Optional[str]:
        """Obtiene connection string desde launchSettings.json"""
        try:
            launch_settings_path = self.root_path / "Backend" / "Properties" / "launchSettings.json"
            if not launch_settings_path.exists():
                return None
                
            with open(launch_settings_path, 'r') as f:
                launch_settings = json.load(f)
            
            # Buscar en el perfil https
            https_profile = launch_settings.get("profiles", {}).get("https", {})
            env_vars = https_profile.get("environmentVariables", {})
            sql_connection = env_vars.get("SQL")
            
            return sql_connection
        except Exception as ex:
            print(f"⚠️  No se pudo obtener connection string: {ex}")
            return None

    def check_permission_exists(self, action_key: str) -> bool:
        """Verifica si un permiso existe en la base de datos"""
        if not PYODBC_AVAILABLE or not self.connection_string:
            return False
            
        try:
            with pyodbc.connect(self.connection_string) as conn:
                cursor = conn.cursor()
                cursor.execute("SELECT COUNT(*) FROM system_permissions WHERE ActionKey = ?", (action_key,))
                count = cursor.fetchone()[0]
                return count > 0
        except Exception as ex:
            print(f"⚠️  Error verificando permiso {action_key}: {ex}")
            return False

    def create_permission_in_db(self, action_key: str, nombre: str, grupo: str, descripcion: str) -> bool:
        """Crea un permiso en la base de datos"""
        if not PYODBC_AVAILABLE or not self.connection_string:
            print("⚠️  pyodbc no disponible o sin connection string")
            return False
            
        try:
            with pyodbc.connect(self.connection_string) as conn:
                cursor = conn.cursor()
                cursor.execute("""
                    INSERT INTO system_permissions (Nombre, Descripcion, ActionKey, GroupKey, GrupoNombre, Active, FechaCreacion, FechaModificacion)
                    VALUES (?, ?, ?, ?, ?, 1, GETUTCDATE(), GETUTCDATE())
                """, (nombre, descripcion, action_key, grupo, grupo))
                conn.commit()
                return True
        except Exception as ex:
            print(f"❌ Error creando permiso {action_key}: {ex}")
            return False

    def handle_field_permission_interactive(self, entity_name: str, field_name: str) -> Optional[str]:
        """Maneja FieldPermission de manera interactiva"""
        
        print(f"\n🔐 CONFIGURACIÓN FIELDPERMISSION")
        print(f"   🏗️  Entidad: {entity_name}")
        print(f"   📝 Campo: {field_name}")
        print("-" * 50)
        
        # Generar nombres de permisos sugeridos
        entity_upper = entity_name.upper()
        field_upper = field_name.upper()
        
        permission_suggestions = {
            "CREATE": f"{entity_upper}.{field_upper}.CREATE",
            "UPDATE": f"{entity_upper}.{field_upper}.EDIT", 
            "VIEW": f"{entity_upper}.{field_upper}.VIEW"
        }
        
        print("🎯 Selecciona qué permisos aplicar (puedes elegir múltiples):")
        print("   1. CREATE - Controla creación de registros con este campo")
        print("   2. UPDATE - Controla modificación del campo en registros existentes")
        print("   3. VIEW   - Controla si el campo es visible en consultas")
        print("   4. Todos los anteriores")
        print("   0. Cancelar")
        
        selected_permissions = {}
        
        while True:
            try:
                choice = input("\n🔹 Tu elección (1,2,3,4 o 0): ").strip()
                
                if choice == "0":
                    print("❌ Cancelado")
                    return None
                elif choice == "1":
                    selected_permissions["CREATE"] = permission_suggestions["CREATE"]
                    break
                elif choice == "2":
                    selected_permissions["UPDATE"] = permission_suggestions["UPDATE"]
                    break
                elif choice == "3":
                    selected_permissions["VIEW"] = permission_suggestions["VIEW"]
                    break
                elif choice == "4":
                    selected_permissions = permission_suggestions.copy()
                    break
                else:
                    print("❌ Opción inválida. Usa 1, 2, 3, 4 o 0")
                    continue
                    
            except KeyboardInterrupt:
                print("\n❌ Cancelado por usuario")
                return None
        
        # Mostrar permisos seleccionados
        print(f"\n📋 Permisos seleccionados:")
        for perm_type, action_key in selected_permissions.items():
            print(f"   {perm_type}: {action_key}")
        
        # Verificar cuáles existen en BD
        existing_permissions = {}
        missing_permissions = {}
        
        print(f"\n🔍 Verificando permisos en base de datos...")
        
        for perm_type, action_key in selected_permissions.items():
            if self.check_permission_exists(action_key):
                existing_permissions[perm_type] = action_key
                print(f"   ✅ {action_key} existe")
            else:
                missing_permissions[perm_type] = action_key
                print(f"   ❌ {action_key} NO existe")
        
        # Crear permisos faltantes si el usuario lo desea
        if missing_permissions:
            print(f"\n🔨 Se encontraron {len(missing_permissions)} permisos faltantes.")
            create_choice = input("¿Deseas crearlos automáticamente? (s/N): ").strip().lower()
            
            if create_choice in ['s', 'si', 'sí', 'y', 'yes']:
                grupo_name = entity_name.upper()
                
                for perm_type, action_key in missing_permissions.items():
                    # Generar nombres descriptivos
                    action_names = {
                        "CREATE": f"Crear {field_name} en {entity_name}",
                        "UPDATE": f"Modificar {field_name} en {entity_name}",
                        "VIEW": f"Ver {field_name} en {entity_name}"
                    }
                    
                    nombre = action_names.get(perm_type, f"{perm_type} {field_name}")
                    descripcion = f"Permiso para {perm_type.lower()} el campo {field_name} de la entidad {entity_name}"
                    
                    print(f"   🔨 Creando {action_key}...")
                    if self.create_permission_in_db(action_key, nombre, grupo_name, descripcion):
                        print(f"   ✅ {action_key} creado exitosamente")
                        existing_permissions[perm_type] = action_key
                    else:
                        print(f"   ❌ Error creando {action_key}")
            else:
                print("⚠️  Los permisos faltantes NO fueron creados")
        
        # Generar atributo FieldPermission
        if existing_permissions:
            parts = []
            for perm_type in ["CREATE", "UPDATE", "VIEW"]:
                if perm_type in existing_permissions:
                    parts.append(f'{perm_type}="{existing_permissions[perm_type]}"')
            
            attribute_content = f"FieldPermission({', '.join(parts)})"
            
            print(f"\n✅ Atributo generado:")
            print(f"   [{attribute_content}]")
            
            return attribute_content
        else:
            print("❌ No se pudieron configurar los permisos")
            return None

def main():
    parser = argparse.ArgumentParser(
        description="🎯 Custom Validator Tool - Entity Metadata Manager",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Ejemplos de uso:
  # Un campo de una entidad
  python customvalidator.py categoria:Nombre:SoloCrear
  
  # Múltiples campos de la misma entidad
  python customvalidator.py categoria:Nombre:SoloCrear categoria:Descripcion:SoloCrear
  
  # Múltiples entidades y campos
  python customvalidator.py categoria:Nombre:SoloCrear system_users:Email:SoloCrear
  
  # Campo con múltiples atributos (futuro)
  python customvalidator.py categoria:Nombre:SoloCrear|Required
  
  # Listar entidades disponibles
  python customvalidator.py --list

Formato de argumentos:
  entidad:campo:atributo1|atributo2 [entidad2:campo2:atributo3]
  
Convención de nombres de entidades:
  categoria              -> Categoria
  system_users           -> SystemUsers  
  user_profile_data      -> UserProfileData
  system_organization    -> SystemOrganization

Atributos disponibles:
  SoloCrear             - Campo solo modificable durante creación
  AutoIncremental       - Campo con numeración automática incremental
  NoSelect              - Campo que se devuelve como null en consultas (para datos sensibles)
        """
    )
    
    parser.add_argument("field_definitions", nargs='*', 
                       help="Definiciones de campos en formato tabla:campo:atributo1|atributo2")
    parser.add_argument("--list", action="store_true", help="Listar entidades disponibles")
    
    args = parser.parse_args()
    
    manager = EntityMetadataManager()
    manager.print_header()
    
    if args.list:
        manager.list_entities()
        return
    
    if not args.field_definitions:
        print("❌ ERROR: Se requieren definiciones de campos")
        print("\n💡 Formato: python customvalidator.py entidad:campo:atributo [entidad2:campo2:atributo2]")
        print("💡 Un campo: python customvalidator.py categoria:Nombre:SoloCrear")
        print("💡 Múltiples: python customvalidator.py categoria:Nombre:SoloCrear categoria:Descripcion:SoloCrear")
        print("💡 Multi-entidad: python customvalidator.py categoria:Nombre:SoloCrear system_users:Email:SoloCrear")
        print("💡 Ayuda: python customvalidator.py --help")
        sys.exit(1)
    
    # Parse argumentos con el nuevo formato
    entities_dict = manager.parse_field_definitions(args.field_definitions)
    
    if not entities_dict:
        print("❌ ERROR: No se pudieron procesar las definiciones de campos")
        sys.exit(1)
    
    success = manager.process_multiple_entities(entities_dict)
    
    if success:
        print("\n🎉 ¡Operación completada exitosamente!")
        print("💡 Los cambios estarán disponibles al compilar el proyecto")
    else:
        print("\n❌ La operación no se pudo completar")
        sys.exit(1)

if __name__ == "__main__":
    main()