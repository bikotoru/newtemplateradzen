using Frontend.Components.Base.Tables;
using Frontend.Services;
using Radzen;

namespace Frontend.Pages.AdvancedQuery
{
    public class SavedQueryViewManager
    {
        public List<ViewConfiguration<SavedQueryDto>> ViewConfigurations { get; private set; } = new();
        private readonly QueryService? _queryService;

        public SavedQueryViewManager(QueryService? queryService = null)
        {
            _queryService = queryService;
            InitializeDefaultViews();
        }

        private void InitializeDefaultViews()
        {
            ViewConfigurations.Add(new ViewConfiguration<SavedQueryDto>
            {
                DisplayName = "Todas las Búsquedas",
                QueryBuilder = _queryService?.For<SavedQueryDto>()?
                    .Where(x => x.Active == true)
                    .OrderBy(x => x.Name),
                ColumnConfigs = new List<ColumnConfig<SavedQueryDto>>
                {
                    new ColumnConfig<SavedQueryDto>
                    {
                        Property = "Name",
                        Title = "Nombre",
                        Width = "200px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Left,
                        Visible = true,
                        Order = 1
                    },
                    new ColumnConfig<SavedQueryDto>
                    {
                        Property = "EntityName",
                        Title = "Entidad",
                        Width = "150px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Left,
                        Visible = true,
                        Order = 2
                    },
                    new ColumnConfig<SavedQueryDto>
                    {
                        Property = "Description",
                        Title = "Descripción",
                        Width = "250px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Left,
                        Visible = true,
                        Order = 3
                    },
                    new ColumnConfig<SavedQueryDto>
                    {
                        Property = "FechaCreacion",
                        Title = "Fecha Creación",
                        Width = "130px",
                        Sortable = true,
                        Filterable = false,
                        TextAlign = TextAlign.Left,
                        Visible = true,
                        Order = 4
                    },
                    new ColumnConfig<SavedQueryDto>
                    {
                        Property = "IsPublic",
                        Title = "Pública",
                        Width = "80px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Center,
                        Visible = true,
                        Order = 5
                    },
                    new ColumnConfig<SavedQueryDto>
                    {
                        Property = "SharedCount",
                        Title = "Compartidos",
                        Width = "100px",
                        Sortable = true,
                        Filterable = false,
                        TextAlign = TextAlign.Center,
                        Visible = true,
                        Order = 6
                    }
                }
            });

            ViewConfigurations.Add(new ViewConfiguration<SavedQueryDto>
            {
                DisplayName = "Mis Búsquedas",
                QueryBuilder = _queryService?.For<SavedQueryDto>()?
                    .Where(x => x.Active == true && x.CreadorId != null)
                    .OrderBy(x => x.Name),
                ColumnConfigs = ViewConfigurations.First().ColumnConfigs
            });

            ViewConfigurations.Add(new ViewConfiguration<SavedQueryDto>
            {
                DisplayName = "Búsquedas Públicas",
                QueryBuilder = _queryService?.For<SavedQueryDto>()?
                    .Where(x => x.Active == true && x.IsPublic == true)
                    .OrderBy(x => x.Name),
                ColumnConfigs = ViewConfigurations.First().ColumnConfigs
            });
        }

        public ViewConfiguration<SavedQueryDto>? GetViewByName(string displayName) => 
            ViewConfigurations.FirstOrDefault(v => v.DisplayName == displayName);

        public ViewConfiguration<SavedQueryDto> GetDefaultView() => 
            ViewConfigurations.FirstOrDefault() ?? 
            new ViewConfiguration<SavedQueryDto>("Default", _queryService?.For<SavedQueryDto>() ?? null!);
    }
}