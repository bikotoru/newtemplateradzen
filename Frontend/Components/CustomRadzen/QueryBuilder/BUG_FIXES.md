# CustomDataFilter - Correcciones de Bugs

## 🐛 Bug: Los filtros se borran al cambiar operadores

### **Problema Identificado**
Al seleccionar un campo y luego cambiar de operador (por ejemplo, de "Igual" a "No Igual"), el filtro completo se eliminaba de la lista en lugar de simplemente cambiar el operador.

### **Causa Raíz**
1. **Conversión Incorrecta**: La comparación de filtros en `RemoveFilter()` usaba conversión incorrecta entre `CustomCompositeFilterDescriptor` y `CompositeFilterDescriptor`
2. **Lógica de Aplicación**: El método `OnOperatorChange` aplicaba filtros automáticamente incluso cuando `Auto="false"`
3. **Referencias Perdidas**: Las referencias de objetos se perdían durante las conversiones

### **Soluciones Aplicadas**

#### 1. **Corrección en RemoveFilter() - CustomDataFilterItem.razor.cs:228**
```csharp
// ANTES (problemático)
DataFilter.Filters = DataFilter.Filters.Where(f => CustomCompositeFilterDescriptor.FromRadzenFilter(f) != Filter).ToList();

// DESPUÉS (corregido)
var radzenFilter = Filter.ToRadzenFilter();
DataFilter.Filters = DataFilter.Filters.Where(f => f != radzenFilter).ToList();
```

#### 2. **Mejora en OnOperatorChange() - CustomDataFilterItem.razor.cs:158**
```csharp
async Task OnOperatorChange(object p)
{
    // Logging para debug
    Console.WriteLine($"🔄 OnOperatorChange: Old={Filter.FilterOperator}, New={p}, Property={Filter.Property}");

    // Asignar explícitamente el nuevo operador
    var newOperator = (FilterOperator)p;
    Filter.FilterOperator = newOperator;

    // Solo limpiar el valor para operadores que no requieren valor
    if (IsOperatorNullOrEmpty())
    {
        Console.WriteLine($"🧹 Clearing FilterValue for operator {newOperator}");
        Filter.FilterValue = null;
    }
    else
    {
        Console.WriteLine($"✅ Keeping FilterValue for operator {newOperator}, current value: {Filter.FilterValue}");
    }

    // Forzar actualización del estado sin disparar el Auto filter
    await ChangeState();

    // Solo aplicar filtro si Auto está habilitado
    if (DataFilter.Configuration.Auto)
    {
        await ApplyFilter();
    }
}
```

#### 3. **Logging Temporal para Debug**
Se agregaron logs temporales para facilitar el debugging:
- `🔄` Cambios de operador
- `🧹` Limpieza de valores
- `✅` Conservación de valores
- `🗑️` Eliminación de filtros
- `📁` Operaciones en grupos
- `🏠` Operaciones en DataFilter principal

### **Comportamiento Mejorado**

#### **Antes del Fix**
1. Usuario selecciona campo "Nombre"
2. Usuario ingresa valor "Juan"
3. Usuario cambia operador de "Igual" a "No Igual"
4. ❌ **El filtro completo desaparece**

#### **Después del Fix**
1. Usuario selecciona campo "Nombre"
2. Usuario ingresa valor "Juan"
3. Usuario cambia operador de "Igual" a "No Igual"
4. ✅ **El filtro mantiene el campo y valor, solo cambia el operador**

### **Operadores Especiales**
Los siguientes operadores automáticamente limpian el valor (comportamiento correcto):
- `IsNull` - No requiere valor
- `IsNotNull` - No requiere valor
- `IsEmpty` - No requiere valor
- `IsNotEmpty` - No requiere valor

### **Compatibilidad**
- ✅ Mantiene compatibilidad completa con RadzenDataFilter
- ✅ Respeta configuración `Auto="false"`
- ✅ Funciona con todos los tipos de datos
- ✅ Compatible con filtros anidados y grupos

### **Testing**
Para probar la corrección:

1. **Test básico**:
   - Agregar filtro con campo "Nombre"
   - Cambiar operador de "Igual" a "Contiene"
   - Verificar que el filtro se mantiene

2. **Test con valores**:
   - Agregar filtro "Edad" = "25"
   - Cambiar a "Mayor que"
   - Verificar que el valor "25" se mantiene

3. **Test operadores especiales**:
   - Cambiar a "Es nulo"
   - Verificar que el valor se limpia automáticamente

### **Logs de Debug**
Los logs temporales se pueden ver en la consola del navegador (F12):
```
🔄 OnOperatorChange: Old=Equals, New=NotEquals, Property=Name
✅ Keeping FilterValue for operator NotEquals, current value: Juan
```

### **Próximos Pasos**
- Los logs de debug se pueden limpiar en producción
- Monitorear que no haya efectos secundarios
- Considerar agregar tests unitarios para estos casos

---

**Fecha**: 2025-09-29
**Estado**: ✅ Implementado y Probado
**Impacto**: Alto - Corrige funcionalidad crítica del filtro