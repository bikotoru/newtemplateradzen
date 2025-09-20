# Test UIConfig Persistence

## Pasos para probar la persistencia de UIConfig

1. **Abrir FormDesigner**
   - Navegar a `/admin/form-designer`
   - Seleccionar o crear una entidad

2. **Agregar una sección**
   - Hacer clic en "Agregar sección"
   - Darle un nombre como "Test Section"

3. **Agregar un campo Switch/Boolean**
   - Seleccionar la sección creada
   - Hacer clic en el campo "Boolean/Switch" del panel izquierdo
   - Configurar el campo en el panel de propiedades:
     - Display Name: "Test Switch"
     - Texto cuando es Verdadero: "Activado"
     - Texto cuando es Falso: "Desactivado"

4. **Guardar el layout**
   - Hacer clic en "Guardar Layout"
   - Verificar en la consola del navegador los logs:
     ```
     [FormDesigner] SaveLayout called
     [FormDesigner] Field: TestSwitch
     [FormDesigner]   UIConfig - TrueLabel: Activado, FalseLabel: Desactivado
     [FormDesigner] SaveLayout response received...
     ```

5. **Recargar la página**
   - Refrescar la página (F5)
   - Verificar en la consola los logs de LoadFormLayout:
     ```
     [FormDesigner] LoadFormLayout called for entity: [EntityName]
     [FormDesigner] Field: TestSwitch
     [FormDesigner]   UIConfig - TrueLabel: [valor], FalseLabel: [valor]
     [FormDesigner] LoadFormLayout completed
     ```

6. **Verificar en el preview**
   - Confirmar que el switch muestra los valores configurados
   - Si no se ven los valores, revisar los logs para identificar dónde se pierde la información

## Comandos curl para prueba directa

```bash
# Test GET - verificar estructura actual
curl -X GET "http://localhost:5000/api/form-designer/formulario/layout/[EntityName]" \
  -H "accept: application/json"

# Test POST - guardar con UIConfig
curl -X POST "http://localhost:5000/api/form-designer/formulario/layout" \
  -H "accept: application/json" \
  -H "Content-Type: application/json" \
  -d '{
    "entityName": "[EntityName]",
    "sections": [
      {
        "title": "Test Section",
        "fields": [
          {
            "fieldName": "TestSwitch",
            "displayName": "Test Switch",
            "fieldType": "boolean",
            "isRequired": false,
            "isVisible": true,
            "isReadOnly": false,
            "gridSize": 6,
            "uiConfig": {
              "trueLabel": "Activado",
              "falseLabel": "Desactivado"
            }
          }
        ]
      }
    ]
  }'
```

## Qué buscar en los logs

1. **Durante el guardado**: Verificar que los valores de UIConfig se están enviando correctamente
2. **Durante la carga**: Verificar que los valores de UIConfig se están recibiendo correctamente
3. **Diferencias**: Comparar los valores enviados vs los valores recibidos para identificar dónde se pierden

## Problemas conocidos a verificar

- UIConfig puede ser null y no estar siendo inicializado
- Los valores pueden estar siendo sobrescritos en algún punto del flujo
- La serialización/deserialización puede estar afectando los valores
- El backend puede no estar guardando correctamente la estructura UIConfig