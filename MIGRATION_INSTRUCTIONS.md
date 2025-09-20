# 🔧 INSTRUCCIONES PARA EJECUTAR LA MIGRACIÓN

## ❌ **PROBLEMA ACTUAL**
El CHECK constraint de la base de datos no permite los nuevos tipos de campos de referencia:
- `entity_reference`
- `user_reference`
- `file_reference`

**Error**: `The INSERT statement conflicted with the CHECK constraint "CK_system_custom_field_definitions_field_type"`

## ✅ **SOLUCIÓN IMPLEMENTADA**

He creado **3 métodos** para ejecutar la migración:

### **Método 1: API QuickMigration (RECOMENDADO)**

**1. Ejecutar la aplicación:**
```bash
cd "/mnt/c/Users/vuret/OneDrive/Escritorio/Nueva carpeta (12)/base"
dotnet run --project CustomFields.API
```

**2. Ejecutar migración via API:**
```bash
# Verificar estado actual
curl -X GET "https://localhost:7001/api/QuickMigration/check-status"

# Ejecutar migración
curl -X POST "https://localhost:7001/api/QuickMigration/execute-now"
```

O usando un cliente REST (Postman, VS Code REST Client, etc.):
```http
POST https://localhost:7001/api/QuickMigration/execute-now
Content-Type: application/json
```

### **Método 2: Script SQL Manual**

**1. Ejecutar el script SQL directamente:**
```sql
-- Conectarte a SQL Server Management Studio
-- Abrir el archivo: execute_migration.sql
-- Cambiar a la base de datos: USE AgendaGesV3;
-- Ejecutar todo el script
```

### **Método 3: DatabaseMigrationController**

**1. Usar los endpoints del sistema de migración original:**
```bash
# Verificar estado
GET /api/DatabaseMigration/check-migration-status

# Ejecutar migración
POST /api/DatabaseMigration/execute-reference-fields-migration
```

## 🧪 **VERIFICAR QUE LA MIGRACIÓN FUNCIONÓ**

### **Paso 1: Verificar el Constraint**
```sql
SELECT
    cc.name AS ConstraintName,
    cc.definition AS ConstraintDefinition
FROM sys.check_constraints cc
INNER JOIN sys.tables t ON cc.parent_object_id = t.object_id
WHERE cc.name = 'CK_system_custom_field_definitions_field_type'
  AND t.name = 'system_custom_field_definitions';
```

**Resultado esperado:**
```sql
([FieldType]IN('text','textarea','number','date','boolean','select','multiselect','entity_reference','user_reference','file_reference'))
```

### **Paso 2: Probar Creación de Campo**
```bash
# Usar el endpoint de testing
POST /api/test-customfields

# Con este JSON:
{
  "EntityName": "TestEntity",
  "FieldName": "TestEntityRef",
  "DisplayName": "Test Entity Reference",
  "FieldType": "entity_reference",
  "IsRequired": false,
  "Active": true
}
```

### **Paso 3: Usar FormDesigner**
1. Ve a `/admin/custom-fields` en tu aplicación
2. Crea un nuevo campo personalizado
3. Selecciona tipo: "Referencia a Entidad"
4. Debería crearse sin errores

## 🔍 **ENDPOINT DE DIAGNÓSTICO**

Para verificar rápidamente el estado:
```bash
GET /api/QuickMigration/check-status
```

Respuesta exitosa:
```json
{
  "success": true,
  "migrationExecuted": true,
  "constraintDefinition": "([FieldType]IN('text','textarea','number','date','boolean','select','multiselect','entity_reference','user_reference','file_reference'))",
  "message": "Migración ya ejecutada"
}
```

## ⚠️ **NOTAS IMPORTANTES**

1. **Base de Datos**: La migración se ejecuta en la base de datos `AgendaGesV3`
2. **Backup**: Es recomendable hacer backup antes de ejecutar la migración
3. **Transaccional**: La migración es segura y reversible
4. **Testing**: Los nuevos endpoints incluyen testing automático

## 🎯 **DESPUÉS DE LA MIGRACIÓN**

Una vez ejecutada la migración exitosamente:

✅ **Podrás crear campos de tipo:**
- `entity_reference` - Referencias a entidades del sistema
- `user_reference` - Referencias a usuarios
- `file_reference` - Referencias a archivos

✅ **El FormDesigner automáticamente:**
- Detectará campos disponibles para referencias
- Excluirá campos string (varchar, nvarchar, text)
- Incluirá campos custom existentes
- Auto-seleccionará "Id" como campo de valor

✅ **El sistema estará completamente funcional para crear campos de referencia** 🚀

---

## 🆘 **SI ALGO FALLA**

**Revierte la migración:**
```sql
-- Volver al constraint original
ALTER TABLE system_custom_field_definitions
DROP CONSTRAINT CK_system_custom_field_definitions_field_type;

ALTER TABLE system_custom_field_definitions
ADD CONSTRAINT CK_system_custom_field_definitions_field_type
    CHECK (FieldType IN ('text', 'textarea', 'number', 'date', 'boolean', 'select', 'multiselect'));
```

**Contacta para soporte técnico si necesitas ayuda adicional.**