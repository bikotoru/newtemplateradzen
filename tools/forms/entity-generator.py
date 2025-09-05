#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
🎯 Entity Generator - Generador Completo de Entidades (REFACTORIZADO)
Automatiza la creación de entidades CRUD completas en 2 fases:

FASE 1: Base de Datos (tabla + sync EF Core + permisos)
FASE 2: Sistema CRUD Completo (Backend + Frontend)

Integra automáticamente:
- ✅ Creación de tabla SQL
- 🔐 Generación de permisos del sistema  
- 🔧 Backend completo (Service + Controller)
- 🎨 Frontend completo (Service + ViewManager + Components)

Usage:
    python tools/forms/entity-generator.py --entity "Marca" --module "Inventario.Core" --phase 1
    python tools/forms/entity-generator.py --entity "Marca" --module "Inventario.Core" --phase 2
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
        self.forms_path = self.tools_path / "forms"
        
        # Importar módulos
        sys.path.append(str(self.forms_path))
        sys.path.append(str(self.tools_path / "db"))
        sys.path.append(str(self.tools_path / "permissions"))
        
        # Importar generadores
        from backend.backend_generator import BackendGenerator
        from backend.service_registry import BackendServiceRegistry
        from frontend.frontend_generator import FrontendGenerator
        from frontend.service_registry import FrontendServiceRegistry
        from shared.validation import EntityValidator
        from table import DatabaseTableGenerator
        from permissions_generator import PermissionsGenerator
        
        # Inicializar componentes
        self.db_generator = DatabaseTableGenerator()
        self.backend_generator = BackendGenerator(self.root_path)
        self.backend_registry = BackendServiceRegistry(self.root_path)
        self.frontend_generator = FrontendGenerator(self.root_path)
        self.frontend_registry = FrontendServiceRegistry(self.root_path)
        self.validator = EntityValidator(self.root_path)
        self.permissions_generator = PermissionsGenerator()
    
    def print_header(self, phase):
        print("=" * 70)
        print(f"🎯 ENTITY GENERATOR - FASE {phase}")
        print("=" * 70)
        print()
    
    def fase_1_database(self, entity_name, module, fields):
        """FASE 1: Crear tabla en base de datos, sincronizar modelos y generar permisos"""
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
            
            # Generar permisos automáticamente
            print("🔐 Generando permisos del sistema...")
            permissions_success = self.permissions_generator.generate_permissions(
                entity_name=entity_name,
                entity_plural=f"{entity_name}s",
                preview=False
            )
            
            if permissions_success:
                print(f"✅ Permisos generados para {entity_name}")
            else:
                print(f"⚠️ Error generando permisos para {entity_name}")
                print("💡 Los permisos se pueden generar manualmente:")
                print(f"   python tools/permissions/permissions_generator.py --entity {entity_name}")
            
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
        """FASE 2: Generar sistema completo (Backend + Frontend)"""
        self.print_header(2)
        
        try:
            print("🔧 Generando Backend...")
            
            # Generar archivos backend
            if not self.backend_generator.generate(entity_name, module):
                return False
            
            # Actualizar ServiceRegistry backend
            if not self.backend_registry.update(entity_name, module):
                return False
            
            print("✅ Backend completado")
            print()
            print("🎨 Generando Frontend completo...")
            
            # Generar frontend completo (Service + ViewManager + List + Fast + Formulario)
            if not self.frontend_generator.generate_frontend_with_formulario(entity_name, module):
                return False
            
            # Actualizar ServiceRegistry frontend
            if not self.frontend_registry.update(entity_name, module):
                return False
            
            print()
            print("🎉🎉 FASE 2 COMPLETADA EXITOSAMENTE - SISTEMA CRUD COMPLETO! 🎉🎉")
            print()
            print("📁 BACKEND GENERADO:")
            print(f"✅ {entity_name}Service.cs")
            print(f"✅ {entity_name}Controller.cs") 
            print(f"✅ Backend ServiceRegistry actualizado")
            print()
            print("📁 FRONTEND GENERADO:")
            print(f"✅ Frontend {entity_name}Service.cs")
            print(f"✅ Frontend {entity_name}ViewManager.cs")
            print(f"✅ Frontend {entity_name}List.razor + .cs")
            print(f"✅ Frontend {entity_name}Fast.razor + .cs")
            print(f"✅ Frontend {entity_name}Formulario.razor + .cs")
            print(f"✅ Frontend ServiceRegistry actualizado")
            print()
            print("🌐 URLS DISPONIBLES:")
            print(f"   Lista: /{''.join(module.lower().split('.'))}/{entity_name.lower()}/list")
            print(f"   Formulario: /{''.join(module.lower().split('.'))}/{entity_name.lower()}/formulario")
            print()
            print("🎊 ENTIDAD CRUD COMPLETAMENTE FUNCIONAL!")
            print("🔗 Con soporte automático para lookups")
            print("⚡ Incluye creación rápida como componente independiente")
            
            return True
            
        except Exception as e:
            print(f"❌ ERROR en FASE 2: {e}")
            return False
    
    def fase_3_frontend(self, entity_name, module):
        """FASE 3: Generar frontend (Service + Registry + Components)"""
        self.print_header(3)
        
        try:
            # FASE 3.1: Generar Service frontend
            if not self.frontend_generator.generate_service_only(entity_name, module):
                return False
            
            # FASE 3.1: Actualizar ServiceRegistry frontend
            if not self.frontend_registry.update(entity_name, module):
                return False
            
            print()
            print("🎉 FASE 3.1 COMPLETADA EXITOSAMENTE")
            print(f"✅ Frontend {entity_name}Service.cs generado")
            print(f"✅ Frontend ServiceRegistry actualizado")
            print()
            print("⚠️  FASE 3.2 pendiente: ViewManager + Componentes Razor")
            print("💡 Usa --phase 3.2 para generar Service + ViewManager")
            return True
            
        except Exception as e:
            print(f"❌ ERROR en FASE 3: {e}")
            return False
    
    def fase_32_frontend_full(self, entity_name, module):
        """FASE 3.2: Generar frontend completo (Service + ViewManager)"""
        self.print_header("3.2")
        
        try:
            # Generar Service + ViewManager
            if not self.frontend_generator.generate_service_and_viewmanager(entity_name, module):
                return False
            
            # Actualizar ServiceRegistry
            if not self.frontend_registry.update(entity_name, module):
                return False
            
            print()
            print("🎉 FASE 3.2 COMPLETADA EXITOSAMENTE")
            print(f"✅ Frontend {entity_name}Service.cs generado")
            print(f"✅ Frontend {entity_name}ViewManager.cs generado")
            print(f"✅ Frontend ServiceRegistry actualizado")
            print()
            print("📋 SIGUIENTE PASO:")
            print(f"   python tools/forms/entity-generator.py --entity \"{entity_name}\" --module \"{module}\" --phase 3.3")
            return True
            
        except Exception as e:
            print(f"❌ ERROR en FASE 3.2: {e}")
            return False
    
    def fase_33_frontend_components(self, entity_name, module):
        """FASE 3.3: Generar componentes Razor completos"""
        self.print_header("3.3")
        
        try:
            # Generar Service + ViewManager + Componentes Razor
            if not self.frontend_generator.generate_full_frontend(entity_name, module):
                return False
            
            # Actualizar ServiceRegistry
            if not self.frontend_registry.update(entity_name, module):
                return False
            
            print()
            print("🎉 FASE 3.3 COMPLETADA EXITOSAMENTE")
            print(f"✅ Frontend {entity_name}Service.cs generado")
            print(f"✅ Frontend {entity_name}ViewManager.cs generado")
            print(f"✅ Frontend {entity_name}List.razor generado")
            print(f"✅ Frontend {entity_name}List.razor.cs generado")
            print(f"✅ Frontend ServiceRegistry actualizado")
            print()
            print("📋 SIGUIENTE PASO:")
            print(f"   python tools/forms/entity-generator.py --entity \"{entity_name}\" --module \"{module}\" --phase 3.4")
            return True
            
        except Exception as e:
            print(f"❌ ERROR en FASE 3.3: {e}")
            return False
    
    def fase_34_frontend_with_fast(self, entity_name, module):
        """FASE 3.4: Generar frontend con componente Fast"""
        self.print_header("3.4")
        
        try:
            # Generar Service + ViewManager + List + Fast
            if not self.frontend_generator.generate_frontend_with_fast(entity_name, module):
                return False
            
            # Actualizar ServiceRegistry
            if not self.frontend_registry.update(entity_name, module):
                return False
            
            print()
            print("🎉 FASE 3.4 COMPLETADA EXITOSAMENTE")
            print(f"✅ Frontend {entity_name}Service.cs generado")
            print(f"✅ Frontend {entity_name}ViewManager.cs generado")
            print(f"✅ Frontend {entity_name}List.razor generado")
            print(f"✅ Frontend {entity_name}List.razor.cs generado")
            print(f"✅ Frontend {entity_name}Fast.razor generado")
            print(f"✅ Frontend {entity_name}Fast.razor.cs generado")
            print(f"✅ Frontend ServiceRegistry actualizado")
            print()
            print("🎊 ENTIDAD CON CREACIÓN RÁPIDA COMPLETAMENTE FUNCIONAL!")
            print(f"🌐 Lista: /{''.join(module.lower().split('.'))}/{entity_name.lower()}/list")
            print(f"⚡ Creación rápida disponible como componente independiente")
            print()
            print("📋 SIGUIENTE PASO:")
            print(f"   python tools/forms/entity-generator.py --entity \"{entity_name}\" --module \"{module}\" --phase 3.5")
            return True
            
        except Exception as e:
            print(f"❌ ERROR en FASE 3.4: {e}")
            return False
    
    def fase_35_frontend_formulario_completo(self, entity_name, module):
        """FASE 3.5: Generar frontend completo con Formulario"""
        self.print_header("3.5")
        
        try:
            # Generar Service + ViewManager + List + Fast + Formulario
            if not self.frontend_generator.generate_frontend_with_formulario(entity_name, module):
                return False
            
            # Actualizar ServiceRegistry
            if not self.frontend_registry.update(entity_name, module):
                return False
            
            print()
            print("🎉 FASE 3.5 COMPLETADA EXITOSAMENTE")
            print(f"✅ Frontend {entity_name}Service.cs generado")
            print(f"✅ Frontend {entity_name}ViewManager.cs generado")
            print(f"✅ Frontend {entity_name}List.razor generado")
            print(f"✅ Frontend {entity_name}List.razor.cs generado")
            print(f"✅ Frontend {entity_name}Fast.razor generado")
            print(f"✅ Frontend {entity_name}Fast.razor.cs generado")
            print(f"✅ Frontend {entity_name}Formulario.razor generado")
            print(f"✅ Frontend {entity_name}Formulario.razor.cs generado")
            print(f"✅ Frontend ServiceRegistry actualizado")
            print()
            print("🎊🎊 ENTIDAD CRUD COMPLETAMENTE FUNCIONAL! 🎊🎊")
            print(f"🌐 Lista: /{''.join(module.lower().split('.'))}/{entity_name.lower()}/list")
            print(f"📝 Formulario: /{''.join(module.lower().split('.'))}/{entity_name.lower()}/formulario")
            print(f"⚡ Creación rápida disponible como componente")
            print(f"🔗 Con soporte completo para lookups automáticos")
            return True
            
        except Exception as e:
            print(f"❌ ERROR en FASE 3.5: {e}")
            return False
    
    def run(self, entity_name, module, phase, fields=None):
        """Ejecutar la fase especificada"""
        try:
            # Validaciones
            self.validator.validate_entity_inputs(entity_name, module, phase)
            self.validator.validate_project_structure()
            self.validator.validate_phase_requirements(phase, fields)
            
            # Ejecutar la fase correspondiente
            if phase == 1:
                return self.fase_1_database(entity_name, module, fields)
            elif phase == 2:
                return self.fase_2_backend(entity_name, module)
            
        except Exception as e:
            print(f"\n❌ ERROR: {e}")
            return False

def main():
    parser = argparse.ArgumentParser(description='🎯 Entity Generator - Generador de Entidades por Fases (MODULAR)')
    
    parser.add_argument('--entity', required=True,
                       help='Nombre de la entidad (ej: Marca, Producto)')
    parser.add_argument('--module', required=True,
                       help='Módulo donde crear la entidad (ej: Inventario.Core)')
    parser.add_argument('--phase', type=float, choices=[1, 2], required=True,
                       help='Fase a ejecutar: 1=Database, 2=Backend+Frontend(Sistema CRUD completo)')
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