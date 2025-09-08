#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
🎯 Entity Generator - Generador Completo de Entidades (MODULAR)
Automatiza la creación de entidades CRUD con opciones flexibles:

OPCIONES MODULARES:
🗄️  --target db        = Solo Base de Datos (tabla + sync EF Core + permisos)
🎨  --target interfaz   = Solo Interfaz (backend + frontend completo)
🚀  --target todo       = Todo junto (DB + Interfaz completo)

VERIFICACIONES INTELIGENTES:
- ✅ Detecta permisos existentes automáticamente
- 🔄 Solo crea permisos faltantes
- 📊 Reporta estado actual

Usage:
    # Solo base de datos
    python tools/forms/entity-generator.py --entity "Marca" --module "Inventario.Core" --target db
    
    # Solo interfaz (requiere tabla existente)
    python tools/forms/entity-generator.py --entity "Marca" --module "Inventario.Core" --target interfaz
    
    # Todo completo
    python tools/forms/entity-generator.py --entity "Marca" --module "Inventario.Core" --target todo
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
        from shared.entity_configurator import EntityConfigurator
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
        self.configurator = EntityConfigurator()
    
    def print_header(self, phase):
        print("=" * 70)
        print(f"🎯 ENTITY GENERATOR - FASE {phase}")
        print("=" * 70)
        print()
    
    def target_db(self, config):
        """TARGET DB: Crear tabla en base de datos, sincronizar modelos y generar permisos"""
        self.print_header("DB")
        print(f"🗄️ CREANDO BASE DE DATOS para: {config.entity_name}")
        print()
        
        # Paso 1: Crear tabla
        print("📊 PASO 1: Creando tabla en base de datos...")
        table_name = config.entity_name.lower()
        
        # Convertir configuración a formato table.py
        fields_for_table = []
        for field in config.regular_fields:
            field_str = f"{field.name}:{field.field_type.value}"
            if field.size:
                field_str += f":{field.size}"
            fields_for_table.append(field_str)
        
        fks_for_table = []
        for fk in config.foreign_keys:
            fk_str = f"{fk.field}:{fk.ref_table}"
            fks_for_table.append(fk_str)
        
        success = self.db_generator.run(
            table_name=table_name,
            fields=fields_for_table,
            foreign_keys=fks_for_table,
            unique_fields=None,
            execute=True,
            preview=False,
            autosync=True,
            add_fields_mode=False
        )
        
        if not success:
            print()
            print("❌ ERROR CREANDO TABLA")
            print("💡 Revisa los errores anteriores antes de continuar")
            return False
        
        print(f"✅ Tabla '{table_name}' creada en base de datos")
        print(f"✅ Modelos EF Core sincronizados")
        print(f"✅ Entidad {config.entity_name} disponible para QueryService")
        print()
        
        # Paso 2: Generar permisos con verificación
        print("🔐 PASO 2: Verificando y generando permisos...")
        permissions_success = self.generate_permissions_smart(
            config.entity_name, 
            config.entity_plural, 
            is_nn_relation=getattr(config, 'is_nn_relation', False)
        )
        
        # Paso 3: Si es relación NN, verificar y actualizar GlobalUsings
        if getattr(config, 'is_nn_relation', False):
            print("🔗 PASO 3: Verificando GlobalUsings para entidades NN...")
            self.ensure_nn_global_usings()
        
        print()
        print("🎉 TARGET DB COMPLETADO EXITOSAMENTE!")
        if not getattr(config, 'is_nn_relation', False):
            print("📋 SIGUIENTE PASO (opcional):")
            print(f"   python tools/forms/entity-generator.py --entity \"{config.entity_name}\" --module \"{config.module}\" --target interfaz")
        else:
            print("📁 Modelo NN se organizará automáticamente en Shared.Models/Entities/NN/ al ejecutar sync")
        
        return success and permissions_success
    
    def generate_permissions_smart(self, entity_name, entity_plural=None, is_nn_relation=False):
        """Generar permisos con verificación inteligente"""
        try:
            if not entity_plural:
                entity_plural = f"{entity_name}s"
            
            # Verificar permisos existentes primero
            print(f"🔍 Verificando permisos existentes para {entity_name}...")
            
            # Si se marcó explícitamente como relación NN, forzar formato correcto
            if is_nn_relation and not entity_name.lower().startswith('nn_'):
                print(f"⚠️ ADVERTENCIA: Se marcó como tabla NN pero el nombre '{entity_name}' no sigue el formato 'nn_tabla1_tabla2'")
                print(f"💡 Se recomienda usar formato: nn_{entity_name.lower().replace('nn', '').replace('_productos', '').replace('venta', 'venta_productos')}")
            
            # Usar el permissions generator para verificar y crear
            permissions_success = self.permissions_generator.generate_permissions(
                entity_name=entity_name,
                entity_plural=entity_plural,
                preview=False,
                force_nn=is_nn_relation
            )
            
            return permissions_success
            
        except Exception as e:
            print(f"⚠️ Error generando permisos para {entity_name}: {e}")
            print("💡 Los permisos se pueden generar manualmente:")
            print(f"   python tools/permissions/permissions_generator.py --entity {entity_name}")
            return False
    
    def ensure_nn_global_usings(self):
        """Verificar y agregar 'using Shared.Models.Entities.NN;' a GlobalUsings.cs si no existe"""
        try:
            backend_global = self.root_path / "Backend" / "GlobalUsings.cs"
            frontend_global = self.root_path / "Frontend" / "GlobalUsings.cs" 
            shared_global = self.root_path / "Shared.Models" / "GlobalUsings.cs"
            utils_global = self.root_path / "Backend.Utils" / "GlobalUsings.cs"
            nn_using = "global using Shared.Models.Entities.NN;"
            
            updated_files = []
            
            # Verificar y actualizar Backend/GlobalUsings.cs
            if backend_global.exists():
                with open(backend_global, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                if nn_using not in content:
                    # Buscar la línea de Shared.Models.Entities para insertar después
                    lines = content.split('\n')
                    new_lines = []
                    inserted = False
                    
                    for line in lines:
                        new_lines.append(line)
                        if "global using Shared.Models.Entities;" in line and not inserted:
                            new_lines.append(nn_using)
                            inserted = True
                    
                    if inserted:
                        with open(backend_global, 'w', encoding='utf-8') as f:
                            f.write('\n'.join(new_lines))
                        updated_files.append("Backend/GlobalUsings.cs")
            
            # Verificar y actualizar Frontend/GlobalUsings.cs
            if frontend_global.exists():
                with open(frontend_global, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                if nn_using not in content:
                    # Buscar la línea de Shared.Models.Entities para insertar después
                    lines = content.split('\n')
                    new_lines = []
                    inserted = False
                    
                    for line in lines:
                        new_lines.append(line)
                        if "global using Shared.Models.Entities;" in line and not inserted:
                            new_lines.append(nn_using)
                            inserted = True
                    
                    if inserted:
                        with open(frontend_global, 'w', encoding='utf-8') as f:
                            f.write('\n'.join(new_lines))
                        updated_files.append("Frontend/GlobalUsings.cs")
            
            # Verificar y actualizar Shared.Models/GlobalUsings.cs
            if shared_global.exists():
                with open(shared_global, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                if nn_using not in content:
                    # Buscar la línea de Shared.Models.Entities para insertar después
                    lines = content.split('\n')
                    new_lines = []
                    inserted = False
                    
                    for line in lines:
                        new_lines.append(line)
                        if "global using Shared.Models.Entities;" in line and not inserted:
                            new_lines.append(nn_using)
                            inserted = True
                    
                    if inserted:
                        with open(shared_global, 'w', encoding='utf-8') as f:
                            f.write('\n'.join(new_lines))
                        updated_files.append("Shared.Models/GlobalUsings.cs")
            
            # Verificar y actualizar Backend.Utils/GlobalUsings.cs
            if utils_global.exists():
                with open(utils_global, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                if nn_using not in content:
                    # Buscar la línea de Shared.Models.Entities para insertar después
                    lines = content.split('\n')
                    new_lines = []
                    inserted = False
                    
                    for line in lines:
                        new_lines.append(line)
                        if "global using Shared.Models.Entities;" in line and not inserted:
                            new_lines.append(nn_using)
                            inserted = True
                    
                    if inserted:
                        with open(utils_global, 'w', encoding='utf-8') as f:
                            f.write('\n'.join(new_lines))
                        updated_files.append("Backend.Utils/GlobalUsings.cs")
            
            if updated_files:
                print(f"✅ GlobalUsings actualizados: {', '.join(updated_files)}")
                print(f"   → Agregado: {nn_using}")
            else:
                print("ℹ️  GlobalUsings ya contienen el namespace NN")
                
        except Exception as e:
            print(f"⚠️ Error actualizando GlobalUsings: {e}")
            print("💡 Puedes agregar manualmente: global using Shared.Models.Entities.NN;")
    
    def find_model_namespace(self, entity_name):
        """Buscar el modelo en Shared.Models y extraer su namespace"""
        try:
            shared_models_path = self.root_path / "Shared.Models"
            model_file_name = f"{entity_name}.cs"
            
            print(f"🔍 Buscando modelo: {model_file_name} en Shared.Models...")
            
            # Buscar recursivamente en Shared.Models
            for cs_file in shared_models_path.rglob("*.cs"):
                if cs_file.name == model_file_name:
                    # Leer el archivo y extraer el namespace
                    with open(cs_file, 'r', encoding='utf-8') as f:
                        content = f.read()
                    
                    # Buscar línea de namespace
                    for line in content.split('\n'):
                        line = line.strip()
                        if line.startswith('namespace ') and line.endswith(';'):
                            namespace = line.replace('namespace ', '').replace(';', '').strip()
                            print(f"✅ Modelo encontrado: {cs_file.relative_to(self.root_path)}")
                            print(f"📦 Namespace detectado: {namespace}")
                            return namespace
            
            # Si no se encuentra, usar namespace por defecto
            print(f"⚠️ Modelo {model_file_name} no encontrado en Shared.Models")
            print(f"💡 Usando namespace por defecto: Shared.Models.Entities")
            return "Shared.Models.Entities"
            
        except Exception as e:
            print(f"⚠️ Error buscando modelo: {e}")
            return "Shared.Models.Entities"
    
    def target_interfaz(self, config):
        """TARGET INTERFAZ: Generar solo backend + frontend (requiere tabla existente)"""
        self.print_header("INTERFAZ")
        print(f"🎨 GENERANDO INTERFAZ para: {config.entity_name}")
        print()
        
        # Paso 1: Buscar namespace del modelo existente
        print("🔍 PASO 1: Detectando namespace del modelo...")
        model_namespace = self.find_model_namespace(config.entity_name)
        print()
        
        try:
            # Paso 2: Generar Backend con namespace dinámico
            print("🔧 PASO 2: Generando Backend...")
            if not self.backend_generator.generate(config.entity_name, config.module, model_namespace):
                return False
            
            if not self.backend_registry.update(config.entity_name, config.module):
                return False
            
            print("✅ Backend completado")
            print()
            
            # Paso 2: Generar Frontend completo
            print("🎨 PASO 2: Generando Frontend completo...")
            if not self.frontend_generator.generate_frontend_with_formulario(config.entity_name, config.module):
                return False
            
            if not self.frontend_registry.update(config.entity_name, config.module):
                return False
            
            print()
            print("🎉 TARGET INTERFAZ COMPLETADO EXITOSAMENTE!")
            print()
            print("📁 BACKEND GENERADO:")
            print(f"✅ {config.entity_name}Service.cs")
            print(f"✅ {config.entity_name}Controller.cs") 
            print(f"✅ Backend ServiceRegistry actualizado")
            print()
            print("📁 FRONTEND GENERADO:")
            print(f"✅ Frontend {config.entity_name}Service.cs")
            print(f"✅ Frontend {config.entity_name}ViewManager.cs")
            print(f"✅ Frontend {config.entity_name}List.razor + .cs")
            print(f"✅ Frontend {config.entity_name}Fast.razor + .cs")
            print(f"✅ Frontend {config.entity_name}Formulario.razor + .cs")
            print(f"✅ Frontend ServiceRegistry actualizado")
            print()
            print("🌐 URLS DISPONIBLES:")
            print(f"   Lista: /{''.join(config.module.lower().split('.'))}/{config.entity_name.lower()}/list")
            print(f"   Formulario: /{''.join(config.module.lower().split('.'))}/{config.entity_name.lower()}/formulario")
            
            return True
            
        except Exception as e:
            print(f"❌ ERROR en TARGET INTERFAZ: {e}")
            return False
    
    def target_todo(self, config):
        """TARGET TODO: Generar todo completo (DB + Interfaz)"""
        self.print_header("TODO")
        print(f"🚀 GENERACIÓN COMPLETA para: {config.entity_name}")
        print()
        
        # Paso 1: Base de datos
        print("🗄️ ETAPA 1: Base de datos...")
        if not self.target_db(config):
            return False
        
        print()
        print("=" * 50)
        print()
        
        # Paso 2: Interfaz
        print("🎨 ETAPA 2: Interfaz completa...")
        if not self.target_interfaz(config):
            return False
        
        print()
        print("🎊🎊 TARGET TODO COMPLETADO EXITOSAMENTE! 🎊🎊")
        print("🌟 ENTIDAD CRUD COMPLETAMENTE FUNCIONAL!")
        print("✅ Base de datos creada y permisos configurados")
        print("✅ Backend y Frontend completamente generados")
        print("🔗 Con soporte automático para lookups")
        print("⚡ Incluye creación rápida como componente independiente")
        
        return True

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
    
    def run(self, args):
        """Ejecutar el target especificado con configuración avanzada"""
        try:
            # Crear y validar configuración completa
            config = self.configurator.configure_from_args(args)
            
            # Mostrar resumen de configuración
            self.configurator.print_configuration_summary(config)
            
            # Ejecutar el target correspondiente
            if config.target == 'db':
                return self.target_db(config)
            elif config.target == 'interfaz':
                return self.target_interfaz(config)
            elif config.target == 'todo':
                return self.target_todo(config)
            else:
                print(f"❌ ERROR: Target '{config.target}' no válido. Opciones: db, interfaz, todo")
                return False
            
        except Exception as e:
            print(f"\n❌ ERROR: {e}")
            return False

def main():
    parser = argparse.ArgumentParser(
        description='🎯 Entity Generator - Generador Avanzado de Entidades CRUD',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Ejemplo completo:
  python3 tools/forms/entity-generator.py \\
    --entity "Producto" --plural "Productos" --module "Inventario.Core" --target todo \\
    --fields "nombre:string:255" "codigo:string:50" "precio:decimal:18,2" \\
    --fk "categoria_id:categorias" \\
    --form-fields "nombre:required:placeholder=Nombre del producto" "precio:required:min=0" \\
    --grid-fields "nombre:200px:left:sf" "codigo:120px:left:s" "precio:120px:right:sf" \\
    --lookups "categoria_id:categorias:Nombre:required:cache" \\
    --search-fields "nombre,codigo"
        """)
    
    # Argumentos básicos - Entidad normal
    parser.add_argument('--entity',
                       help='Nombre de la entidad (ej: Producto)')
    parser.add_argument('--plural', 
                       help='Plural de la entidad (ej: Productos)')
    
    # Argumentos para relaciones NN (muchos-a-muchos)
    parser.add_argument('--source',
                       help='Tabla source para relación NN (ej: venta)')
    parser.add_argument('--to',
                       help='Tabla target para relación NN (ej: productos)')
    parser.add_argument('--alias',
                       help='Alias opcional para relación NN (ej: promocion)')
    
    # Argumentos comunes
    parser.add_argument('--module', required=True,
                       help='Módulo donde crear la entidad (ej: Inventario.Core)')
    parser.add_argument('--target', choices=['db', 'interfaz', 'todo'], required=True,
                       help='Target: db=Solo BD, interfaz=Solo interfaz, todo=Completo')
    
    # Configuración de base de datos
    parser.add_argument('--fields', nargs='*', 
                       help='Campos de BD: "nombre:tipo:tamaño"')
    parser.add_argument('--fk', nargs='*',
                       help='Foreign Keys: "campo:tabla_referencia"')
    
    # Configuración de UI
    parser.add_argument('--form-fields', nargs='*',
                       help='Config formulario: "campo:required:placeholder=..."')
    parser.add_argument('--grid-fields', nargs='*', 
                       help='Config grilla: "campo:ancho:align:opciones"')
    parser.add_argument('--readonly-fields', nargs='*',
                       help='Campos solo lectura: "campo:tipo:label=..."')
    parser.add_argument('--lookups', nargs='*',
                       help='Lookups: "campo:tabla:campo_display:opciones"')
    
    # Configuración adicional
    parser.add_argument('--search-fields', 
                       help='Campos de búsqueda: "campo1,campo2,campo3"')
    
    # Parámetros legacy (mantenidos por compatibilidad)
    parser.add_argument('--nn-relation-entity', action='store_true',
                       help='[DEPRECATED] Usa --source --to en su lugar')
    
    args = parser.parse_args()
    
    # Validaciones de modo de operación
    is_nn_mode = bool(args.source and args.to)
    is_entity_mode = bool(args.entity)
    
    if not is_nn_mode and not is_entity_mode:
        print("❌ ERROR: Debes especificar:")
        print("   • Entidad normal: --entity NombreEntidad")  
        print("   • Relación NN: --source tabla1 --to tabla2 [--alias nombre]")
        sys.exit(1)
    
    if is_nn_mode and is_entity_mode:
        print("❌ ERROR: No puedes usar --entity junto con --source --to")
        print("💡 Usa una de estas opciones:")
        print("   • Entidad normal: --entity NombreEntidad")  
        print("   • Relación NN: --source tabla1 --to tabla2")
        sys.exit(1)
    
    if is_nn_mode:
        if not args.source or not args.to:
            print("❌ ERROR: Para relaciones NN necesitas --source y --to")
            print("💡 Ejemplo: --source venta --to productos")
            sys.exit(1)
            
        # En modo NN, forzar target db (las relaciones NN no tienen interfaz)
        if args.target != 'db':
            print("❌ ERROR: Las relaciones NN solo soportan --target db")
            print("💡 Usa: --source venta --to productos --target db")
            sys.exit(1)
    
    # Validaciones básicas para target db/todo
    if args.target in ['db', 'todo']:
        if not args.fields and not args.fk:
            print("❌ ERROR: --fields o --fk requerido para targets 'db' y 'todo'")
            print("💡 Ejemplo: --fields \"nombre:string:100\" --fk \"categoria_id:categorias\"")
            sys.exit(1)
    
    generator = EntityGenerator()
    
    try:
        success = generator.run(args)
        sys.exit(0 if success else 1)
    except KeyboardInterrupt:
        print("\n\n⏹️ Proceso cancelado por el usuario")
        sys.exit(1)
    except Exception as e:
        print(f"\n❌ ERROR inesperado: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()