using Frontend.Components.Base.Tables;
using Frontend.Services;
using Radzen;

namespace Frontend.Modules.Core.Localidades.Comunas
{
    public class ComunaViewManager
    {
        public List<ViewConfiguration<Shared.Models.Entities.Comuna>> ViewConfigurations { get; private set; } = new();
        private readonly QueryService? _queryService;

        public ComunaViewManager(QueryService? queryService = null)
        {
            _queryService = queryService;
            InitializeDefaultViews();
        }

        private void InitializeDefaultViews()
        {
            ViewConfigurations.Add(new ViewConfiguration<Shared.Models.Entities.Comuna>
            {
                DisplayName = "Vista Completa",
                QueryBuilder = _queryService?.For<Shared.Models.Entities.Comuna>()?
                    .Where(x => x.Active == true).Include(x=>x.Region)
                    .OrderBy(x => x.Nombre),
                ColumnConfigs = new List<ColumnConfig<Shared.Models.Entities.Comuna>>
                {
                    new ColumnConfig<Shared.Models.Entities.Comuna>
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
                    new ColumnConfig<Shared.Models.Entities.Comuna>
                    {
                        Property = "Region.Nombre",
                        Title = "Region",
                        Width = "150px",
                        Sortable = false,
                        Filterable = true,
                        TextAlign = TextAlign.Left,
                        Visible = true,
                        Order = 2
                    }
                }
            });
        }

        public ViewConfiguration<Shared.Models.Entities.Comuna>? GetViewByName(string displayName) => 
            ViewConfigurations.FirstOrDefault(v => v.DisplayName == displayName);

        public ViewConfiguration<Shared.Models.Entities.Comuna> GetDefaultView() => 
            ViewConfigurations.FirstOrDefault() ?? 
            new ViewConfiguration<Shared.Models.Entities.Comuna>("Default", _queryService?.For<Shared.Models.Entities.Comuna>() ?? null!);
    }
}