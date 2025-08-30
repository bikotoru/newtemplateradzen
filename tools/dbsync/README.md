# 🐍 Database Sync Tools

Herramientas para sincronización automática Database-First con .NET + Blazor

## 🚀 Uso Rápido

```bash
# Desde la raíz del proyecto
python tools/dbsync/generate-models.py

# Con otro proyecto
python tools/dbsync/generate-models.py --project ../OtroBackend
```

## 📋 Prerequisitos

- Python 3.6+
- .NET 9.0+
- dotnet-ef (herramientas de EF Core)
- SQL Server / SQL Express / LocalDB

## ⚙️ Configuración

### 1. launchSettings.json
El script lee automáticamente desde `Backend/Properties/launchSettings.json`:

```json
{
  "profiles": {
    "https": {
      "commandName": "Project",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "SQL": "Server=localhost;Database=MiTienda;Trusted_Connection=true;"
      }
    }
  }
}
```

### 2. Estructura del Proyecto
```
NuevoProyecto/
├── Backend/                    # 🚀 Tu API
│   └── Properties/
│       └── launchSettings.json # 🔗 Connection string aquí
├── Backend.Utils/              # 📦 DbContext reutilizable
│   └── Data/                   # ✅ Se genera aquí
│       └── AppDbContext.cs
├── Shared.Models/              # 📦 Modelos compartidos
│   └── Entities/               # ✅ Se generan aquí
└── tools/dbsync/               # 🐍 Herramientas Python
    └── generate-models.py
```

## 🎯 Lo que hace el script

1. **Lee** connection string desde `launchSettings.json`
2. **Limpia** archivos anteriores
3. **Ejecuta** `dotnet ef dbcontext scaffold`
4. **Mueve** entidades → `Shared.Models/Entities/`
5. **Mueve** DbContext → `Backend.Utils/Data/`
6. **Ajusta** namespaces automáticamente
7. **Compila** para verificar que todo funciona

## 🔧 Comandos

```bash
# Instalar EF Core tools (una sola vez)
dotnet tool install --global dotnet-ef

# Generar modelos (usar cada vez que cambies la BD)
python tools/dbsync/generate-models.py

# Con proyecto específico
python tools/dbsync/generate-models.py --project Backend2
```

## ✅ Ventajas

- **Database-First real** - tu BD es la fuente de verdad
- **Multiplataforma** - funciona en Windows/Linux/Mac  
- **Zero-config** - solo un comando
- **Reutilizable** - DbContext en Backend.Utils
- **Type-safe** - genera todo automáticamente

## 🚨 Importante

- El script **NO modifica** tu base de datos
- Solo **lee** la estructura y **genera** código
- Puedes correrlo las veces que quieras
- Siempre respalda tus cambios personalizados antes de regenerar