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

    def register_entity(self, entity_name, module_path, display_property="Name", search_fields=None, backend_api=None):
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
            # Determinar backend API automáticamente si no se especifica
            if backend_api is None:
                backend_api = self._determine_backend_api(module_path)

            payload = {
                "entityName": entity_name,
                "modulePath": module_path,
                "displayProperty": display_property,
                "searchFields": search_fields or ["Name", "Nombre"],
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
        Escribir configuración a archivo que puede ser leído por EntityRegistrationService
        """
        try:
            config_file = Path(__file__).parent.parent.parent / "Frontend" / "Services" / "entity-registration.json"

            # Crear directorio si no existe
            config_file.parent.mkdir(parents=True, exist_ok=True)

            # Leer configuración existente
            existing_config = []
            if config_file.exists():
                try:
                    with open(config_file, 'r', encoding='utf-8') as f:
                        existing_config = json.load(f)
                except json.JSONDecodeError:
                    existing_config = []

            # Verificar si la entidad ya existe
            entity_name = payload["entityName"]
            existing_index = -1
            for i, config in enumerate(existing_config):
                if config.get("entityName") == entity_name:
                    existing_index = i
                    break

            # Actualizar o agregar
            if existing_index >= 0:
                existing_config[existing_index] = payload
                print(f"✅ Configuración actualizada para '{entity_name}'")
            else:
                existing_config.append(payload)
                print(f"✅ Nueva configuración agregada para '{entity_name}'")

            # Escribir archivo actualizado
            with open(config_file, 'w', encoding='utf-8') as f:
                json.dump(existing_config, f, indent=2, ensure_ascii=False)

            print(f"📄 Configuración guardada en: {config_file}")
            return True

        except Exception as e:
            print(f"❌ Error escribiendo archivo de configuración: {e}")
            return False

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