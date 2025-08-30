@echo off
echo ========================================
echo 🗄️  EJECUTANDO SCRIPT BASE DE DATOS
echo ========================================

set SERVER=localhost
set DATABASE=NewPOC
set USERNAME=sa
set PASSWORD=Soporte.2019

echo 🔗 Conexión: %SERVER%
echo 📄 Base de datos: %DATABASE%
echo 👤 Usuario: %USERNAME%
echo.

echo Ejecutando Base.sql...
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d master -i Base.sql

if %errorlevel% neq 0 (
    echo.
    echo ❌ ERROR ejecutando el script
    echo Verifica:
    echo   - SQL Server está ejecutándose
    echo   - Usuario y contraseña correctos
    echo   - Permisos para crear base de datos
    pause
    exit /b 1
) else (
    echo.
    echo ✅ SCRIPT EJECUTADO EXITOSAMENTE
    echo.
    echo 🎯 Datos de acceso:
    echo Usuario: admin@admin.cl
    echo Password: Soporte.2019
    echo Organización: Organización Base
    echo.
)

pause