#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
🔗 Entity Registration Script
Registra automáticamente nuevas entidades en el sistema de Custom Fields
Se llama desde entity-generator.py cuando se crea una nueva entidad con --target todo
"""

import sys
import os
import json
import requests
from pathlib import Path

class EntityRegistrationAPI:
    """API para registrar entidades en el sistema Custom Fields"""

    def __init__(self, base_url="http://localhost:5000"):
        self.base_url = base_url
        self.api_url = f"{base_url}/api/entity-registration"

    def register_entity(self, entity_name, module_path, display_property=None, search_fields=None, backend_api=None):
        """
        Registrar una nueva entidad en el sistema

        Args:
            entity_name: Nombre de la entidad (ej: "Empleado")
            module_path: Ruta del módulo (ej: "Core.RRHH")
            display_property: Propiedad para mostrar (ej: "NombreCompleto")
            search_fields: Lista de campos searchables
            backend_api: API Backend a usar (ej: "MainBackend", "FormBackend")
        """
        try:
            # Auto-detectar display property si no se especifica
            if display_property is None:
                display_property = self._detect_display_property(entity_name)

            # Auto-detectar search fields si no se especifican
            if search_fields is None:
                search_fields = self._detect_search_fields(entity_name, display_property)

            # Determinar backend API automáticamente si no se especifica
            if backend_api is None:
                backend_api = self._determine_backend_api(module_path)

            payload = {
                "entityName": entity_name,
                "modulePath": module_path,
                "displayProperty": display_property,
                "searchFields": search_fields,
                "backendApi": backend_api
            }

            print(f"🔗 Registrando entidad '{entity_name}' en Custom Fields...")
            print(f"   📁 Módulo: {module_path}")
            print(f"   👁️ Display Property: {display_property}")
            print(f"   🔍 Search Fields: {', '.join(payload['searchFields'])}")
            print(f"   🖥️ Backend API: {backend_api}")

            # En un entorno real, esto haría una llamada HTTP a la API
            # Por ahora, simularemos el registro escribiendo a un archivo de configuración

            return self._write_to_config_file(payload)

        except Exception as e:
            print(f"❌ Error registrando entidad '{entity_name}': {e}")
            return False

    def _determine_backend_api(self, module_path):
        """
        Determinar automáticamente qué backend API usar basado en el módulo

        Args:
            module_path: Ruta del módulo (ej: "Core.RRHH", "Forms.Designer")

        Returns:
            str: Backend API recomendado
        """
        # Convertir a minúsculas para comparación
        module_lower = module_path.lower()

        # Reglas de mapeo basadas en el módulo
        if any(keyword in module_lower for keyword in ['form', 'custom', 'designer', 'field']):
            return "FormBackend"
        elif any(keyword in module_lower for keyword in ['system', 'auth', 'user', 'permission', 'role']):
            return "GlobalBackend"  # Cambiar SystemBackend por GlobalBackend
        elif any(keyword in module_lower for keyword in ['core', 'main', 'base']):
            return "GlobalBackend"  # Cambiar MainBackend por GlobalBackend
        else:
            # Default para módulos personalizados
            return "GlobalBackend"   # Cambiar MainBackend por GlobalBackend

    def _write_to_config_file(self, payload):
        """
        Escribir configuración directamente al código C# de EntityRegistrationService
        """
        try:
            service_file = Path(__file__).parent.parent.parent / "Frontend" / "Services" / "EntityRegistrationService.cs"

            if not service_file.exists():
                print(f"❌ Error: No se encontró {service_file}")
                return False

            # Leer archivo actual
            with open(service_file, 'r', encoding='utf-8') as f:
                content = f.read()

            # Generar código C# para la nueva entidad
            entity_name = payload["entityName"]
            entity_lower = entity_name.lower()
            display_property = payload["displayProperty"]
            search_fields = payload["searchFields"]

            # Detectar si la entidad tiene servicio específico o usar genérico
            service_type = self._detect_service_type(entity_name)

            # Generar código de configuración
            config_code = f'''
            // {entity_name}
            var {entity_lower}Config = new EntityConfiguration
            {{
                EntityType = typeof(Shared.Models.Entities.{entity_name}),
                ServiceType = typeof({service_type}),
                DisplayProperty = "{display_property}",
                ValueProperty = "Id",
                SearchFields = new[] {{ {", ".join(f'"{field}"' for field in search_fields)} }},
                EnableCache = true
            }};
            RegisterEntity("{entity_lower}", {entity_lower}Config);'''

            # Buscar el punto de inserción en RegisterKnownEntities
            marker = "// ===== ENTIDADES DEL SISTEMA ====="

            if marker in content:
                # Verificar si ya existe la entidad
                entity_marker = f"// {entity_name}"
                if entity_marker in content:
                    print(f"⚠️ La entidad '{entity_name}' ya está registrada")
                    return True

                # Insertar antes del marcador de entidades del sistema
                content = content.replace(marker, config_code + f"\n\n            {marker}")

                # Escribir archivo actualizado
                with open(service_file, 'w', encoding='utf-8') as f:
                    f.write(content)

                print(f"✅ Entidad '{entity_name}' agregada al EntityRegistrationService.cs")
                return True
            else:
                print(f"❌ Error: No se encontró el marcador de inserción en {service_file}")
                return False

        except Exception as e:
            print(f"❌ Error escribiendo código C#: {e}")
            return False

    def _detect_service_type(self, entity_name):
        """
        Detectar si existe un servicio específico o usar GenericEntityService
        """
        try:
            # Buscar servicio específico en Frontend/Services o Frontend/Modules
            frontend_path = Path(__file__).parent.parent.parent / "Frontend"

            # Patrones de búsqueda para servicios específicos
            service_patterns = [
                f"*{entity_name}Service.cs",
                f"*{entity_name}*Service.cs"
            ]

            for pattern in service_patterns:
                matches = list(frontend_path.rglob(pattern))
                if matches:
                    # Intentar extraer el nombre del servicio del archivo
                    service_file = matches[0]
                    service_name = service_file.stem  # Nombre sin extensión
                    print(f"🔍 Servicio específico encontrado: {service_name}")
                    return service_name

            # Si no se encuentra servicio específico, usar genérico
            print(f"🔧 Usando servicio genérico para {entity_name}")
            return f"GenericEntityService<Shared.Models.Entities.{entity_name}>"

        except Exception as e:
            print(f"⚠️ Error detectando servicio para {entity_name}: {e}")
            return f"GenericEntityService<Shared.Models.Entities.{entity_name}>"

    def _detect_display_property(self, entity_name):
        """
        Auto-detectar la propiedad de display leyendo la entidad real
        """
        try:
            # Leer archivo de entidad
            entity_file = Path(__file__).parent.parent.parent / "Shared.Models" / "Entities" / f"{entity_name}.cs"

            if entity_file.exists():
                with open(entity_file, 'r', encoding='utf-8') as f:
                    content = f.read()

                # Buscar propiedades comunes en orden de preferencia
                display_properties = ["Nombre", "Name", "DisplayName", "Title", "Titulo", "Description", "Descripcion"]

                for prop in display_properties:
                    # Buscar declaración de propiedad
                    if f"public string? {prop}" in content or f"public string {prop}" in content:
                        print(f"🎯 Auto-detectado DisplayProperty: {prop}")
                        return prop

            # Fallback: usar "Name" por defecto
            print(f"⚠️ No se pudo auto-detectar DisplayProperty para {entity_name}, usando 'Name'")
            return "Name"

        except Exception as e:
            print(f"⚠️ Error auto-detectando DisplayProperty: {e}")
            return "Name"

    def _detect_search_fields(self, entity_name, display_property):
        """
        Auto-detectar campos de búsqueda leyendo la entidad real
        """
        try:
            # Leer archivo de entidad
            entity_file = Path(__file__).parent.parent.parent / "Shared.Models" / "Entities" / f"{entity_name}.cs"

            search_fields = []

            if entity_file.exists():
                with open(entity_file, 'r', encoding='utf-8') as f:
                    content = f.read()

                # Buscar propiedades string que podrían ser searchables
                searchable_properties = ["Nombre", "Name", "DisplayName", "Title", "Titulo", "Description", "Descripcion", "Code", "Codigo", "Email"]

                for prop in searchable_properties:
                    if f"public string? {prop}" in content or f"public string {prop}" in content:
                        if prop not in search_fields:
                            search_fields.append(prop)

                # Asegurar que DisplayProperty esté incluido
                if display_property not in search_fields:
                    search_fields.insert(0, display_property)

            # Si no se encontraron, usar defaults
            if not search_fields:
                search_fields = [display_property] if display_property != "Name" else ["Name", "Nombre"]

            print(f"🔍 Auto-detectados SearchFields: {search_fields}")
            return search_fields

        except Exception as e:
            print(f"⚠️ Error auto-detectando SearchFields: {e}")
            return [display_property] if display_property else ["Name", "Nombre"]

def main():
    """Main function para uso desde línea de comandos"""
    if len(sys.argv) < 3:
        print("❌ ERROR: Uso incorrecto")
        print("💡 Uso: python register_entity.py <entity_name> <module_path> [display_property] [backend_api] [search_fields...]")
        print("📝 Ejemplo: python register_entity.py Empleado Core.RRHH NombreCompleto GlobalBackend Nombre Apellido Email")
        print("🤖 Backend API se auto-detecta si no se especifica")
        sys.exit(1)

    entity_name = sys.argv[1]
    module_path = sys.argv[2]
    display_property = sys.argv[3] if len(sys.argv) > 3 else "Name"

    # Verificar si el 4to argumento es un backend API válido
    backend_api = None
    search_fields_start = 4
    if len(sys.argv) > 3:
        potential_backend = sys.argv[3] if len(sys.argv) > 3 else None
        if len(sys.argv) > 4 and sys.argv[4] in ['GlobalBackend', 'FormBackend']:
            backend_api = sys.argv[4]
            search_fields_start = 5

    search_fields = sys.argv[search_fields_start:] if len(sys.argv) > search_fields_start else None

    # Crear registrador
    registrator = EntityRegistrationAPI()

    # Registrar entidad
    success = registrator.register_entity(
        entity_name=entity_name,
        module_path=module_path,
        display_property=display_property,
        search_fields=search_fields,
        backend_api=backend_api
    )

    if success:
        print()
        print("🎉 ENTIDAD REGISTRADA EXITOSAMENTE!")
        print(f"✅ '{entity_name}' está ahora disponible para campos de referencia")
        print("🔄 Reinicia la aplicación para que los cambios tomen efecto")
    else:
        print()
        print("❌ ERROR EN EL REGISTRO")
        print("💡 Revisa los logs anteriores para más detalles")
        sys.exit(1)

if __name__ == "__main__":
    main()