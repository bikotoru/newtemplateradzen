-- Script para agregar permisos de búsquedas guardadas
-- ================================================================

-- Verificar si existe una tabla de permisos (buscar posible estructura)
-- Si no existe, estos permisos deberán ser agregados manualmente por el administrador

-- Permisos requeridos para el módulo de búsquedas guardadas:
-- SAVEDQUERIES.VIEW   - Ver y obtener búsquedas guardadas
-- SAVEDQUERIES.CREATE - Crear y duplicar búsquedas guardadas

-- ================================================================
-- IMPORTANTE: 
-- Este script no puede ejecutarse automáticamente porque no se 
-- encontró la estructura de tabla de permisos en la base de datos.
-- 
-- Los siguientes permisos deben ser agregados manualmente:
-- 
-- 1. SAVEDQUERIES.VIEW
--    Descripción: Permite ver y obtener búsquedas guardadas
--    
-- 2. SAVEDQUERIES.CREATE  
--    Descripción: Permite crear y duplicar búsquedas guardadas
--
-- Los permisos de actualización y eliminación se manejan a través
-- de la lógica de propietario (CreadorId) y compartidos (SystemSavedQueryShares)
-- ================================================================

PRINT 'PERMISOS REQUERIDOS PARA BÚSQUEDAS GUARDADAS:';
PRINT '1. SAVEDQUERIES.VIEW - Ver y obtener búsquedas guardadas';
PRINT '2. SAVEDQUERIES.CREATE - Crear y duplicar búsquedas guardadas';
PRINT '';
PRINT 'NOTA: Estos permisos deben ser agregados manualmente al sistema de permisos.';