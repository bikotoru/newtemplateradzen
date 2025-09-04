using Shared.Models.Entities;
using Frontend.Components.Base.Tables;
using Radzen;

namespace Frontend.Modules.Categoria;

public static class CategoriaConfig
{
    /// <summary>
    /// Configuraciones específicas para Categoria
    /// </summary>
    public static class Settings
    {
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;
        public const string DefaultSortField = "FechaCreacion";
        public const bool DefaultSortDescending = true;
    }

    /// <summary>
    /// Configuraciones para búsqueda
    /// </summary>
    public static class Search
    {
        public static readonly string[] DefaultSearchFields = { "Nombre", "Descripcion" };
        public const int MinSearchLength = 2;
        public const int SearchDelay = 300; // ms
    }

    /// <summary>
    /// Configuraciones para formularios
    /// </summary>
    public static class Forms
    {
        public const string RequiredFieldMessage = "Este campo es obligatorio";
        public const string SaveSuccessMessage = "Categoría guardada exitosamente";
        public const string DeleteSuccessMessage = "Categoría eliminada exitosamente";
        public const string ErrorGenericMessage = "Ha ocurrido un error inesperado";
    }

    /// <summary>
    /// Configuraciones para exportación
    /// </summary>
    public static class Export
    {
        public const string DefaultFileName = "Categorias";
    }

    /// <summary>
    /// 🔥 NUEVAS CONFIGURACIONES PARA FILTROS HÍBRIDOS
    /// </summary>
    public static class HybridFilters
    {
        /// <summary>
        /// Configuración de columnas con filtros híbridos para Categoria
        /// </summary>
        public static List<ColumnConfig<Shared.Models.Entities.Categoria>> GetHybridFilterColumns()
        {
            return new List<ColumnConfig<Shared.Models.Entities.Categoria>>
            {
                new()
                {
                    Property = nameof(Shared.Models.Entities.Categoria.Active),
                    Title = "Estado",
                    Width = "120px",
                    UseCheckBoxFilter = true, // ← CHECKBOX FILTER para bool
                    CheckboxFilterOptions = new CheckboxFilterConfig
                    {
                        MaxItems = 2,
                        EnableSearch = false, // Solo True/False, no necesita búsqueda
                        CacheTimeout = TimeSpan.FromMinutes(10)
                    },
                    Order = 1
                },
                new()
                {
                    Property = nameof(Shared.Models.Entities.Categoria.Nombre),
                    Title = "Nombre",
                    Width = "200px",
                    UseCheckBoxFilter = true, // ← CHECKBOX FILTER para strings con valores únicos
                    CheckboxFilterOptions = new CheckboxFilterConfig
                    {
                        MaxItems = 50,
                        EnableSearch = true, // Permite búsqueda dentro del filtro
                        CacheTimeout = TimeSpan.FromMinutes(5),
                        EnableVirtualization = true
                    },
                    Order = 2
                },
                new()
                {
                    Property = nameof(Shared.Models.Entities.Categoria.Descripcion),
                    Title = "Descripción", 
                    Width = "300px",
                    UseCheckBoxFilter = false, // ← FILTRO TRADICIONAL para texto libre
                    Order = 3
                },
                new()
                {
                    Property = nameof(Shared.Models.Entities.Categoria.FechaCreacion),
                    Title = "Fecha Creación",
                    Width = "150px",
                    UseCheckBoxFilter = false, // ← FILTRO TRADICIONAL para fechas
                    Order = 4
                },
                new()
                {
                    Property = "Organization.Nombre", // ← RELACIÓN con checkbox filter
                    Title = "Organización",
                    Width = "180px",
                    UseCheckBoxFilter = true,
                    RelatedEntityProperty = "Organization",
                    RelatedDisplayProperty = "Nombre",
                    CheckboxFilterOptions = new CheckboxFilterConfig
                    {
                        MaxItems = 20,
                        EnableSearch = true,
                        CacheTimeout = TimeSpan.FromMinutes(15), // Cache más largo para entidades relacionadas
                        IncludeNullValues = true,
                        NullValueText = "(Sin Organización)"
                    },
                    Order = 5
                },
                new()
                {
                    Property = "Creador.UserName", // ← RELACIÓN con checkbox filter
                    Title = "Creado Por",
                    Width = "150px",
                    UseCheckBoxFilter = true,
                    RelatedEntityProperty = "Creador", 
                    RelatedDisplayProperty = "UserName",
                    CheckboxFilterOptions = new CheckboxFilterConfig
                    {
                        MaxItems = 30,
                        EnableSearch = true,
                        CacheTimeout = TimeSpan.FromMinutes(10)
                    },
                    Order = 6
                }
            };
        }

        /// <summary>
        /// Configuración tradicional (sin filtros híbridos) para comparación
        /// </summary>
        public static List<ColumnConfig<Shared.Models.Entities.Categoria>> GetTraditionalColumns()
        {
            return new List<ColumnConfig<Shared.Models.Entities.Categoria>>
            {
                new() { Property = nameof(Shared.Models.Entities.Categoria.Active), Title = "Estado", Width = "120px", Order = 1 },
                new() { Property = nameof(Shared.Models.Entities.Categoria.Nombre), Title = "Nombre", Width = "200px", Order = 2 },
                new() { Property = nameof(Shared.Models.Entities.Categoria.Descripcion), Title = "Descripción", Width = "300px", Order = 3 },
                new() { Property = nameof(Shared.Models.Entities.Categoria.FechaCreacion), Title = "Fecha Creación", Width = "150px", Order = 4 }
            };
        }
    }
}