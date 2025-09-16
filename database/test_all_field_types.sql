-- Test data para todos los tipos de campo personalizados
-- Para probar la FASE 2: Tipos Básicos + Validaciones

USE AgendaGes;

-- Limpiar datos de prueba anteriores
DELETE FROM system_custom_field_definitions WHERE EntityName = 'Empleado' AND FieldName LIKE 'test_%';

-- 1. Campo Text con validaciones
INSERT INTO system_custom_field_definitions
(Id, EntityName, FieldName, DisplayName, Description, FieldType, IsRequired, DefaultValue, SortOrder,
ValidationConfig, UIConfig, IsEnabled, Version, FechaCreacion, FechaModificacion, Active, OrganizationId)
VALUES
(NEWID(), 'Empleado', 'test_texto', 'Texto de Prueba', 'Campo de texto con validaciones', 'text', 1, '', 100,
'{"Required":true,"MinLength":5,"MaxLength":50,"Pattern":"^[A-Za-z\\s]+$","PatternMessage":"Solo letras y espacios"}',
'{"Placeholder":"Ingrese texto aquí","HelpText":"Mínimo 5 caracteres, solo letras"}',
1, 1, GETUTCDATE(), GETUTCDATE(), 1, NULL);

-- 2. Campo TextArea
INSERT INTO system_custom_field_definitions
(Id, EntityName, FieldName, DisplayName, Description, FieldType, IsRequired, DefaultValue, SortOrder,
ValidationConfig, UIConfig, IsEnabled, Version, FechaCreacion, FechaModificacion, Active, OrganizationId)
VALUES
(NEWID(), 'Empleado', 'test_comentarios', 'Comentarios', 'Área de texto para comentarios', 'textarea', 0, '', 101,
'{"MaxLength":500}',
'{"Placeholder":"Escriba sus comentarios aquí","Rows":4,"HelpText":"Máximo 500 caracteres"}',
1, 1, GETUTCDATE(), GETUTCDATE(), 1, NULL);

-- 3. Campo Number con validaciones
INSERT INTO system_custom_field_definitions
(Id, EntityName, FieldName, DisplayName, Description, FieldType, IsRequired, DefaultValue, SortOrder,
ValidationConfig, UIConfig, IsEnabled, Version, FechaCreacion, FechaModificacion, Active, OrganizationId)
VALUES
(NEWID(), 'Empleado', 'test_salario_deseado', 'Salario Deseado', 'Salario esperado en CLP', 'number', 1, '350000', 102,
'{"Required":true,"Min":300000,"Max":5000000,"Step":50000}',
'{"Prefix":"$","Suffix":"CLP","HelpText":"Entre $300,000 y $5,000,000"}',
1, 1, GETUTCDATE(), GETUTCDATE(), 1, NULL);

-- 4. Campo Date con validaciones
INSERT INTO system_custom_field_definitions
(Id, EntityName, FieldName, DisplayName, Description, FieldType, IsRequired, DefaultValue, SortOrder,
ValidationConfig, UIConfig, IsEnabled, Version, FechaCreacion, FechaModificacion, Active, OrganizationId)
VALUES
(NEWID(), 'Empleado', 'test_fecha_disponibilidad', 'Fecha Disponibilidad', 'Fecha de disponibilidad para trabajar', 'date', 1, '', 103,
'{"Required":true,"MinDate":"2024-01-01","MaxDate":"2025-12-31"}',
'{"Format":"dd/MM/yyyy","ShowCalendar":true,"HelpText":"Fecha entre 2024 y 2025"}',
1, 1, GETUTCDATE(), GETUTCDATE(), 1, NULL);

-- 5. Campo Boolean
INSERT INTO system_custom_field_definitions
(Id, EntityName, FieldName, DisplayName, Description, FieldType, IsRequired, DefaultValue, SortOrder,
ValidationConfig, UIConfig, IsEnabled, Version, FechaCreacion, FechaModificacion, Active, OrganizationId)
VALUES
(NEWID(), 'Empleado', 'test_vehiculo_propio', 'Vehículo Propio', '¿Tiene vehículo propio?', 'boolean', 0, 'false', 104,
'{}',
'{"Style":"switch","TrueLabel":"Sí","FalseLabel":"No","HelpText":"Indique si posee vehículo"}',
1, 1, GETUTCDATE(), GETUTCDATE(), 1, NULL);

-- 6. Campo Select
INSERT INTO system_custom_field_definitions
(Id, EntityName, FieldName, DisplayName, Description, FieldType, IsRequired, DefaultValue, SortOrder,
ValidationConfig, UIConfig, IsEnabled, Version, FechaCreacion, FechaModificacion, Active, OrganizationId)
VALUES
(NEWID(), 'Empleado', 'test_nivel_ingles', 'Nivel de Inglés', 'Nivel de competencia en inglés', 'select', 1, 'basico', 105,
'{"Required":true,"AllowEmpty":false}',
'{"Options":[{"Value":"basico","Label":"Básico","Description":"Conocimientos básicos"},{"Value":"intermedio","Label":"Intermedio","Description":"Conversación fluida"},{"Value":"avanzado","Label":"Avanzado","Description":"Dominio profesional"}],"HelpText":"Seleccione su nivel"}',
1, 1, GETUTCDATE(), GETUTCDATE(), 1, NULL);

-- 7. Campo MultiSelect
INSERT INTO system_custom_field_definitions
(Id, EntityName, FieldName, DisplayName, Description, FieldType, IsRequired, DefaultValue, SortOrder,
ValidationConfig, UIConfig, IsEnabled, Version, FechaCreacion, FechaModificacion, Active, OrganizationId)
VALUES
(NEWID(), 'Empleado', 'test_habilidades', 'Habilidades Técnicas', 'Habilidades técnicas del empleado', 'multiselect', 0, '', 106,
'{"MinSelections":1,"MaxSelections":5}',
'{"Options":[{"Value":"excel","Label":"Excel"},{"Value":"word","Label":"Word"},{"Value":"powerpoint","Label":"PowerPoint"},{"Value":"sql","Label":"SQL"},{"Value":"python","Label":"Python"},{"Value":"javascript","Label":"JavaScript"}],"ShowSelectAll":true,"HelpText":"Seleccione entre 1 y 5 habilidades"}',
1, 1, GETUTCDATE(), GETUTCDATE(), 1, NULL);

-- Verificar los datos insertados
SELECT
    FieldName,
    DisplayName,
    FieldType,
    IsRequired,
    DefaultValue,
    ValidationConfig,
    UIConfig
FROM system_custom_field_definitions
WHERE EntityName = 'Empleado' AND FieldName LIKE 'test_%'
ORDER BY SortOrder;

PRINT 'Datos de prueba para todos los tipos de campo insertados exitosamente';