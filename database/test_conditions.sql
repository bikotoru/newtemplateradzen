-- Test data para evaluar condiciones de campos personalizados
-- Para probar la FASE 3: Condiciones

USE AgendaGes;

-- Crear un campo con condiciones que se muestra solo si otro campo tiene un valor específico
INSERT INTO system_custom_field_definitions
(Id, EntityName, FieldName, DisplayName, Description, FieldType, IsRequired, DefaultValue, SortOrder,
ValidationConfig, UIConfig, ConditionsConfig, IsEnabled, Version, FechaCreacion, FechaModificacion, Active, OrganizationId)
VALUES
(NEWID(), 'Empleado', 'test_campo_condicional', 'Campo Condicional', 'Solo se muestra si nivel de inglés es avanzado', 'text', 0, '', 400,
'{"MaxLength":100}',
'{"Placeholder":"Solo visible si inglés avanzado","HelpText":"Campo que aparece condicionalmente"}',
'{"ShowIf":[{"FieldName":"test_nivel_ingles","Operator":"equals","Value":"avanzado","FieldType":"select"}],"LogicalOperator":"AND"}',
1, 1, GETUTCDATE(), GETUTCDATE(), 1, NULL);

-- Crear un campo que se vuelve requerido si el salario es mayor a 1M
INSERT INTO system_custom_field_definitions
(Id, EntityName, FieldName, DisplayName, Description, FieldType, IsRequired, DefaultValue, SortOrder,
ValidationConfig, UIConfig, ConditionsConfig, IsEnabled, Version, FechaCreacion, FechaModificacion, Active, OrganizationId)
VALUES
(NEWID(), 'Empleado', 'test_certificaciones', 'Certificaciones', 'Requerido si salario > 1,000,000', 'textarea', 0, '', 401,
'{}',
'{"Placeholder":"Describe tus certificaciones","Rows":3,"HelpText":"Requerido para salarios altos"}',
'{"RequiredIf":[{"FieldName":"test_salario_deseado","Operator":"greater_than","Value":"1000000","FieldType":"number"}],"LogicalOperator":"AND"}',
1, 1, GETUTCDATE(), GETUTCDATE(), 1, NULL);

-- Crear un campo que se vuelve readonly si tiene vehículo propio
INSERT INTO system_custom_field_definitions
(Id, EntityName, FieldName, DisplayName, Description, FieldType, IsRequired, DefaultValue, SortOrder,
ValidationConfig, UIConfig, ConditionsConfig, IsEnabled, Version, FechaCreacion, FechaModificacion, Active, OrganizationId)
VALUES
(NEWID(), 'Empleado', 'test_transporte_publico', 'Usa Transporte Público', 'Solo lectura si tiene vehículo', 'boolean', 0, 'true', 402,
'{}',
'{"Style":"switch","TrueLabel":"Sí","FalseLabel":"No","HelpText":"Se desactiva si tiene vehículo"}',
'{"ReadOnlyIf":[{"FieldName":"test_vehiculo_propio","Operator":"equals","Value":"true","FieldType":"boolean"}],"LogicalOperator":"AND"}',
1, 1, GETUTCDATE(), GETUTCDATE(), 1, NULL);

-- Verificar los datos insertados
SELECT
    FieldName,
    DisplayName,
    FieldType,
    ConditionsConfig
FROM system_custom_field_definitions
WHERE EntityName = 'Empleado' AND FieldName LIKE 'test_%' AND ConditionsConfig IS NOT NULL
ORDER BY SortOrder;

PRINT 'Datos de prueba para condiciones insertados exitosamente';