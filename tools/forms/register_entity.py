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

    def register_entity(self, entity_name, module_path, display_property="Name", search_fields=None):
        """
        Registrar una nueva entidad en el sistema

        Args:
            entity_name: Nombre de la entidad (ej: "Empleado")
            module_path: Ruta del módulo (ej: "Core.RRHH")
            display_property: Propiedad para mostrar (ej: "NombreCompleto")
            search_fields: Lista de campos searchables
        """
        try:
            payload = {
                "entityName": entity_name,
                "modulePath": module_path,
                "displayProperty": display_property,
                "searchFields": search_fields or ["Name", "Nombre"]
            }

            print(f"🔗 Registrando entidad '{entity_name}' en Custom Fields...")
            print(f"   📁 Módulo: {module_path}")
            print(f"   👁️ Display Property: {display_property}")
            print(f"   🔍 Search Fields: {', '.join(payload['searchFields'])}")

            # En un entorno real, esto haría una llamada HTTP a la API
            # Por ahora, simularemos el registro escribiendo a un archivo de configuración

            return self._write_to_config_file(payload)

        except Exception as e:
            print(f"❌ Error registrando entidad '{entity_name}': {e}")
            return False

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
        print("💡 Uso: python register_entity.py <entity_name> <module_path> [display_property] [search_fields...]")
        print("📝 Ejemplo: python register_entity.py Empleado Core.RRHH NombreCompleto Nombre Apellido Email")
        sys.exit(1)

    entity_name = sys.argv[1]
    module_path = sys.argv[2]
    display_property = sys.argv[3] if len(sys.argv) > 3 else "Name"
    search_fields = sys.argv[4:] if len(sys.argv) > 4 else None

    # Crear registrador
    registrator = EntityRegistrationAPI()

    # Registrar entidad
    success = registrator.register_entity(
        entity_name=entity_name,
        module_path=module_path,
        display_property=display_property,
        search_fields=search_fields
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