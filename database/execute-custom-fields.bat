@echo off
echo ========================================
echo 🔧  EJECUTANDO SCRIPT CUSTOM FIELDS
echo ========================================

set SERVER=localhost,1333
set DATABASE=AgendaGes
set USERNAME=sa
set PASSWORD=Soporte.2019

echo 🔗 Conexión: %SERVER%
echo 📄 Base de datos: %DATABASE%
echo 👤 Usuario: %USERNAME%
echo.

echo Ejecutando system_custom_fields_CREATE.sql...
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -i system_custom_fields_CREATE.sql

if %errorlevel% neq 0 (
    echo.
    echo ❌ ERROR ejecutando el script
    echo Verifica:
    echo   - SQL Server está ejecutándose
    echo   - La base de datos AgendaGes existe
    echo   - Usuario y contraseña correctos
    echo   - Permisos para crear tablas
    pause
    exit /b 1
) else (
    echo.
    echo ✅ CUSTOM FIELDS INSTALADO EXITOSAMENTE
    echo.
    echo 🎯 Próximos pasos:
    echo   1. Ejecutar: python tools/dbsync/generate-models.py
    echo   2. Crear Forms.Models y Forms.Logic
    echo   3. Crear CustomFields.API
    echo.
)

pause