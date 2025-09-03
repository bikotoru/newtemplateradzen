using Frontend.Components.Base.Tables;
using Frontend.Services;
using Radzen;

namespace Frontend.Modules.Categoria
{
    public class CategoriaViewManager
    {
        /// <summary>
        /// Lista de configuraciones de vista predefinidas
        /// </summary>
        public List<ViewConfiguration<Shared.Models.Entities.Categoria>> ViewConfigurations { get; private set; } = new();
        
        private readonly QueryService? _queryService;

        public CategoriaViewManager(QueryService? queryService = null)
        {
            _queryService = queryService;
            InitializeDefaultViews();
        }

        /// <summary>
        /// Inicializa las vistas por defecto
        /// </summary>
        private void InitializeDefaultViews()
        {
            // Vista completa por defecto
            ViewConfigurations.Add(new ViewConfiguration<Shared.Models.Entities.Categoria>
            {
                DisplayName = "Vista Completa",
                QueryBuilder = _queryService?.For<Shared.Models.Entities.Categoria>()?
                    .Where(c => c.Active == true)
                    .OrderBy(c => c.Nombre) ?? null!,
                ColumnConfigs = new List<ColumnConfig<Shared.Models.Entities.Categoria>>
                {
                    new ColumnConfig<Shared.Models.Entities.Categoria>
                    {
                        Property = "Nombre",
                        Title = "Nombre",
                        Width = "200px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Left,
                        Visible = true,
                        Order = 1
                    },
                  
                    new ColumnConfig<Shared.Models.Entities.Categoria>
                    {
                        Property = "Active",
                        Title = "Estado",
                        Width = "120px",
                        Sortable = true,
                        Filterable = false,
                        TextAlign = TextAlign.Center,
                        Visible = true,
                        Order = 3
                    },
                      new ColumnConfig<Shared.Models.Entities.Categoria>
                    {
                        Property = "Descripcion",
                        Title = "Descripción",
                        Width = "300px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Left,
                        Visible = true,
                        Order = 2
                    },
                }
            });

            // Vista compacta solo con nombre
            ViewConfigurations.Add(new ViewConfiguration<Shared.Models.Entities.Categoria>
            {
                DisplayName = "Vista Compacta",
                QueryBuilder = _queryService?.For<Shared.Models.Entities.Categoria>()?
                    .Where(c => c.Active == true)
                    .OrderBy(c => c.Nombre) ?? null!,
                ColumnConfigs = new List<ColumnConfig<Shared.Models.Entities.Categoria>>
                {
                    new ColumnConfig<Shared.Models.Entities.Categoria>
                    {
                        Property = "Nombre",
                        Title = "Categoría",
                        Width = "250px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Left,
                        Visible = true,
                        Order = 1
                    }
                }
            });

            // Vista solo activos con descripción
            ViewConfigurations.Add(new ViewConfiguration<Shared.Models.Entities.Categoria>
            {
                DisplayName = "Solo Activos Detallado",
                QueryBuilder = _queryService?.For<Shared.Models.Entities.Categoria>()?
                    .Where(c => c.Active == true)
                    .OrderBy(c => c.FechaCreacion, true) ?? null!, // Más recientes primero
                ColumnConfigs = new List<ColumnConfig<Shared.Models.Entities.Categoria>>
                {
                    new ColumnConfig<Shared.Models.Entities.Categoria>
                    {
                        Property = "Nombre",
                        Title = "Nombre",
                        Width = "180px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Left,
                        Visible = true,
                        Order = 1
                    },
                    new ColumnConfig<Shared.Models.Entities.Categoria>
                    {
                        Property = "Descripcion",
                        Title = "Descripción",
                        Width = "350px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Left,
                        Visible = true,
                        Order = 2
                    },
                    new ColumnConfig<Shared.Models.Entities.Categoria>
                    {
                        Property = "Active",
                        Title = "Estado",
                        Width = "120px",
                        Sortable = true,
                        Filterable = false,
                        TextAlign = TextAlign.Center,
                        Visible = true,
                        Order = 3
                    },
                    new ColumnConfig<Shared.Models.Entities.Categoria>
                    {
                        Property = "FechaCreacion",
                        Title = "Creado",
                        Width = "120px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Center,
                        Visible = true,
                        Order = 4
                    }
                }
            });

            // Vista personalizada adicional para demostrar el selector
            ViewConfigurations.Add(new ViewConfiguration<Shared.Models.Entities.Categoria>
            {
                DisplayName = "Solo Nombres",
                QueryBuilder = _queryService?.For<Shared.Models.Entities.Categoria>()?
                    .Where(c => c.Active == true)
                    .OrderBy(c => c.Nombre) ?? null!,
                ColumnConfigs = new List<ColumnConfig<Shared.Models.Entities.Categoria>>
                {
                    new ColumnConfig<Shared.Models.Entities.Categoria>
                    {
                        Property = "Nombre",
                        Title = "Nombre de Categoría",
                        Width = "400px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Left,
                        Visible = true,
                        Order = 1
                    }
                }
            });
            
            // Vista con todas las columnas incluyendo fechas
            ViewConfigurations.Add(new ViewConfiguration<Shared.Models.Entities.Categoria>
            {
                DisplayName = "Vista Administrativa",
                QueryBuilder = _queryService?.For<Shared.Models.Entities.Categoria>()?
                    .OrderBy(c => c.FechaModificacion, true) ?? null!, // Últimos modificados primero
                ColumnConfigs = new List<ColumnConfig<Shared.Models.Entities.Categoria>>
                {
                    new ColumnConfig<Shared.Models.Entities.Categoria>
                    {
                        Property = "Nombre",
                        Title = "Nombre",
                        Width = "150px",
                        Sortable = true,
                        Filterable = true,
                        Visible = true,
                        Order = 1
                    },
                    new ColumnConfig<Shared.Models.Entities.Categoria>
                    {
                        Property = "Descripcion",
                        Title = "Descripción",
                        Width = "200px",
                        Sortable = true,
                        Filterable = true,
                        Visible = true,
                        Order = 2
                    },
                    new ColumnConfig<Shared.Models.Entities.Categoria>
                    {
                        Property = "Active",
                        Title = "Estado",
                        Width = "80px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Center,
                        Visible = true,
                        Order = 3
                    },
                    new ColumnConfig<Shared.Models.Entities.Categoria>
                    {
                        Property = "FechaCreacion",
                        Title = "Creado",
                        Width = "110px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Center,
                        Visible = true,
                        Order = 4
                    },
                    new ColumnConfig<Shared.Models.Entities.Categoria>
                    {
                        Property = "FechaModificacion",
                        Title = "Modificado",
                        Width = "110px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Center,
                        Visible = true,
                        Order = 5
                    }
                }
            });
        }

        /// <summary>
        /// Obtiene una configuración por nombre
        /// </summary>
        public ViewConfiguration<Shared.Models.Entities.Categoria>? GetViewByName(string displayName)
        {
            return ViewConfigurations.FirstOrDefault(v => v.DisplayName == displayName);
        }

        /// <summary>
        /// Obtiene la vista por defecto
        /// </summary>
        public ViewConfiguration<Shared.Models.Entities.Categoria> GetDefaultView()
        {
            return ViewConfigurations.FirstOrDefault() ?? new ViewConfiguration<Shared.Models.Entities.Categoria>("Default", _queryService?.For<Shared.Models.Entities.Categoria>() ?? null!);
        }
    }
}