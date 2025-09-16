#!/bin/bash

echo "========================================"
echo "🔧  EJECUTANDO SCRIPT CUSTOM FIELDS"
echo "========================================"

SERVER="localhost,1333"
DATABASE="AgendaGes"
USERNAME="sa"
PASSWORD="Soporte.2019"

echo "🔗 Conexión: $SERVER"
echo "📄 Base de datos: $DATABASE"
echo "👤 Usuario: $USERNAME"
echo

echo "Ejecutando system_custom_fields_CREATE.sql..."
sqlcmd -S $SERVER -U $USERNAME -P $PASSWORD -d $DATABASE -i system_custom_fields_CREATE.sql

if [ $? -ne 0 ]; then
    echo
    echo "❌ ERROR ejecutando el script"
    echo "Verifica:"
    echo "  - SQL Server está ejecutándose"
    echo "  - La base de datos AgendaGes existe"
    echo "  - Usuario y contraseña correctos"
    echo "  - Permisos para crear tablas"
    exit 1
else
    echo
    echo "✅ CUSTOM FIELDS INSTALADO EXITOSAMENTE"
    echo
    echo "🎯 Próximos pasos:"
    echo "  1. Ejecutar: python tools/dbsync/generate-models.py"
    echo "  2. Crear Forms.Models y Forms.Logic"
    echo "  3. Crear CustomFields.API"
    echo
fi