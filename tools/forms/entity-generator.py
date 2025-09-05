#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
🎯 Entity Generator - Generador Completo de Entidades
Automatiza la creación de entidades CRUD completas en 3 fases:

FASE 1: Base de Datos (tabla + sync EF Core)
FASE 2: Backend (Service + Controller + Registry)
FASE 3: Frontend (Service + Registry)

Usage:
    python tools/forms/entity-generator.py --entity "Marca" --module "Inventario.Core" --phase 1
    python tools/forms/entity-generator.py --entity "Marca" --module "Inventario.Core" --phase 2
    python tools/forms/entity-generator.py --entity "Marca" --module "Inventario.Core" --phase 3
"""

import sys
import os
import argparse
from pathlib import Path

# Configurar encoding UTF-8 para Windows
if sys.platform == "win32":
    import codecs
    sys.stdout = codecs.getwriter('utf-8')(sys.stdout.buffer)
    sys.stderr = codecs.getwriter('utf-8')(sys.stderr.buffer)

class EntityGenerator:
    def __init__(self):
        self.root_path = Path.cwd()
        self.tools_path = self.root_path / "tools"
        
        # Importar DatabaseTableGenerator como librería
        sys.path.append(str(self.tools_path / "db"))
        from table import DatabaseTableGenerator
        self.db_generator = DatabaseTableGenerator()
    
    def print_header(self, phase):
        print("=" * 70)
        print(f"🎯 ENTITY GENERATOR - FASE {phase}")
        print("=" * 70)
        print()
    
    def fase_1_database(self, entity_name, module, fields):
        """FASE 1: Crear tabla en base de datos y sincronizar modelos"""
        self.print_header(1)
        print(f"📊 Creando tabla para entidad: {entity_name}")
        print(f"📁 Módulo: {module}")
        print(f"📝 Campos: {len(fields)}")
        for field in fields:
            print(f"   • {field}")
        print()
        
        # Usar DatabaseTableGenerator como librería
        table_name = entity_name.lower()
        
        success = self.db_generator.run(
            table_name=table_name,
            fields=fields,
            foreign_keys=None,
            unique_fields=None,
            execute=True,
            preview=False,
            autosync=True,
            add_fields_mode=False
        )
        
        if success:
            print()
            print("🎉 FASE 1 COMPLETADA EXITOSAMENTE")
            print(f"✅ Tabla '{table_name}' creada en base de datos")
            print(f"✅ Modelos EF Core sincronizados")
            print(f"✅ Entidad {entity_name} disponible para QueryService")
            print()
            print("📋 SIGUIENTE PASO:")
            print(f"   python tools/forms/entity-generator.py --entity \"{entity_name}\" --module \"{module}\" --phase 2")
            return True
        else:
            print()
            print("❌ FASE 1 FALLÓ")
            print("💡 Revisa los errores anteriores antes de continuar")
            return False
    
    def fase_2_backend(self, entity_name, module):
        """FASE 2: Generar backend (Service + Controller + Registry)"""
        self.print_header(2)
        print(f"🔧 Generando backend para entidad: {entity_name}")
        print(f"📁 Módulo: {module}")
        print()
        
        try:
            # 1. Crear directorio del módulo (manejar subdirectorios con punto)
            module_parts = module.split('.')
            backend_module_path = self.root_path / "Backend" / "Modules"
            for part in module_parts:
                backend_module_path = backend_module_path / part
            backend_module_path.mkdir(parents=True, exist_ok=True)
            print(f"📁 Directorio creado: {backend_module_path}")
            
            # 2. Generar Service
            if not self.generate_backend_service(entity_name, module, backend_module_path):
                return False
            
            # 3. Generar Controller
            if not self.generate_backend_controller(entity_name, module, backend_module_path):
                return False
            
            # 4. Actualizar ServiceRegistry
            if not self.update_backend_service_registry(entity_name, module):
                return False
            
            print()
            print("🎉 FASE 2 COMPLETADA EXITOSAMENTE")
            print(f"✅ {entity_name}Service.cs generado")
            print(f"✅ {entity_name}Controller.cs generado")
            print(f"✅ ServiceRegistry actualizado")
            print()
            print("📋 SIGUIENTE PASO:")
            print(f"   python tools/forms/entity-generator.py --entity \"{entity_name}\" --module \"{module}\" --phase 3")
            return True
            
        except Exception as e:
            print(f"❌ ERROR en FASE 2: {e}")
            return False
    
    def fase_3_frontend(self, entity_name, module):
        """FASE 3: Generar frontend (Service + Registry)"""
        self.print_header(3)
        print(f"🎨 Generando frontend para entidad: {entity_name}")
        print(f"📁 Módulo: {module}")
        print()
        print("⚠️  FASE 3 aún no implementada")
        print("💡 Proximamente: generación de Service frontend y registro en ServiceRegistry")
        return False
    
    def validate_inputs(self, entity_name, module, phase):
        """Validaciones básicas de entrada"""
        if not entity_name:
            raise ValueError("Entity name es requerido")
        
        if not module:
            raise ValueError("Module es requerido")
        
        if phase not in [1, 2, 3]:
            raise ValueError("Phase debe ser 1, 2 o 3")
        
        # Validar que la entidad no sea una palabra reservada
        reserved_words = ['user', 'order', 'table', 'index', 'key', 'value']
        if entity_name.lower() in reserved_words:
            raise ValueError(f"'{entity_name}' es una palabra reservada")
        
        return True
    
    def generate_backend_service(self, entity_name, module, module_path):
        """Generar archivo Service del backend"""
        service_content = f"""using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Backend.Utils.Data;
using Backend.Utils.Services;
using Shared.Models.Entities;

namespace Backend.Modules.{module}
{{
    public class {entity_name}Service : BaseQueryService<Shared.Models.Entities.{entity_name}>
    {{
        public {entity_name}Service(AppDbContext context, ILogger<{entity_name}Service> logger) 
            : base(context, logger)
        {{
        }}

    }}
}}"""

        service_file = module_path / f"{entity_name}Service.cs"
        service_file.write_text(service_content, encoding='utf-8')
        print(f"✅ {entity_name}Service.cs generado")
        return True
    
    def generate_backend_controller(self, entity_name, module, module_path):
        """Generar archivo Controller del backend"""
        controller_content = f"""using Microsoft.AspNetCore.Mvc;
using Backend.Utils.Services;
using Shared.Models.Entities;
using Backend.Controllers;

namespace Backend.Modules.{module}
{{
    [Route("api/[controller]")]
    public class {entity_name}Controller : BaseQueryController<Shared.Models.Entities.{entity_name}>
    {{
        private readonly {entity_name}Service _{entity_name.lower()}Service;

        public {entity_name}Controller({entity_name}Service {entity_name.lower()}Service, ILogger<{entity_name}Controller> logger, IServiceProvider serviceProvider)
            : base({entity_name.lower()}Service, logger, serviceProvider)
        {{
            _{entity_name.lower()}Service = {entity_name.lower()}Service;
        }}

    }}
}}"""

        controller_file = module_path / f"{entity_name}Controller.cs"
        controller_file.write_text(controller_content, encoding='utf-8')
        print(f"✅ {entity_name}Controller.cs generado")
        return True
    
    def update_backend_service_registry(self, entity_name, module):
        """Actualizar ServiceRegistry del backend"""
        registry_file = self.root_path / "Backend" / "Services" / "ServiceRegistry.cs"
        
        if not registry_file.exists():
            print(f"❌ ServiceRegistry no encontrado: {registry_file}")
            return False
        
        # Leer contenido actual
        content = registry_file.read_text(encoding='utf-8')
        
        # 1. Agregar using al inicio
        using_line = f"using Backend.Modules.{module};"
        if using_line not in content:
            # Buscar donde insertar el using
            lines = content.split('\n')
            insert_index = 0
            for i, line in enumerate(lines):
                if line.startswith('using Backend.Modules.'):
                    insert_index = i + 1
                elif line.startswith('using Backend.Utils.') and insert_index == 0:
                    insert_index = i
                    break
            
            lines.insert(insert_index, using_line)
            content = '\n'.join(lines)
            print(f"✅ Using agregado: {using_line}")
        
        # 2. Agregar registro del servicio
        service_registration = f"        services.AddScoped<{entity_name}Service>();"
        if service_registration not in content:
            # Buscar donde insertar el servicio
            lines = content.split('\n')
            for i, line in enumerate(lines):
                if "// Module Services" in line:
                    # Insertar después del comentario
                    lines.insert(i + 1, service_registration)
                    content = '\n'.join(lines)
                    print(f"✅ Servicio registrado: {entity_name}Service")
                    break
        
        # Escribir archivo actualizado
        registry_file.write_text(content, encoding='utf-8')
        print(f"✅ ServiceRegistry actualizado")
        return True
    
    def run(self, entity_name, module, phase, fields=None):
        """Ejecutar la fase especificada"""
        try:
            # Validaciones
            self.validate_inputs(entity_name, module, phase)
            
            # Para FASE 1, campos son obligatorios
            if phase == 1 and not fields:
                raise ValueError("FASE 1 requiere especificar --fields")
            
            # Campos por defecto solo para fases 2 y 3 (que no usan campos)
            if not fields:
                fields = []
            
            # Ejecutar la fase correspondiente
            if phase == 1:
                return self.fase_1_database(entity_name, module, fields)
            elif phase == 2:
                return self.fase_2_backend(entity_name, module)
            elif phase == 3:
                return self.fase_3_frontend(entity_name, module)
            
        except Exception as e:
            print(f"\n❌ ERROR: {e}")
            return False

def main():
    parser = argparse.ArgumentParser(description='🎯 Entity Generator - Generador de Entidades por Fases')
    
    parser.add_argument('--entity', required=True,
                       help='Nombre de la entidad (ej: Marca, Producto)')
    parser.add_argument('--module', required=True,
                       help='Módulo donde crear la entidad (ej: Inventario.Core)')
    parser.add_argument('--phase', type=int, choices=[1, 2, 3], required=True,
                       help='Fase a ejecutar: 1=Database, 2=Backend, 3=Frontend')
    parser.add_argument('--fields', nargs='*', default=None,
                       help='Campos personalizados: "nombre:tipo:tamaño"')
    
    args = parser.parse_args()
    
    generator = EntityGenerator()
    
    try:
        success = generator.run(
            entity_name=args.entity,
            module=args.module,
            phase=args.phase,
            fields=args.fields
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