using Frontend.Components.Base.Tables;
using Frontend.Services;
using Radzen;

namespace Frontend.Modules.Core.Localidades.Regions
{
    public class RegionViewManager
    {
        public List<ViewConfiguration<Shared.Models.Entities.Region>> ViewConfigurations { get; private set; } = new();
        private readonly QueryService? _queryService;

        public RegionViewManager(QueryService? queryService = null)
        {
            _queryService = queryService;
            InitializeDefaultViews();
        }

        private void InitializeDefaultViews()
        {
            ViewConfigurations.Add(new ViewConfiguration<Shared.Models.Entities.Region>
            {
                DisplayName = "Vista Completa",
                QueryBuilder = _queryService?.For<Shared.Models.Entities.Region>()?
                    .Where(x => x.Active == true)
                    .OrderBy(x => x.Nombre),
                ColumnConfigs = new List<ColumnConfig<Shared.Models.Entities.Region>>
                {
                    new ColumnConfig<Shared.Models.Entities.Region>
                    {
                        Property = "Nombre",
                        Title = "Nombre",
                        Width = "200px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Left,
                        Visible = true,
                        Order = 1
                    }
                }
            });
        }

        public ViewConfiguration<Shared.Models.Entities.Region>? GetViewByName(string displayName) => 
            ViewConfigurations.FirstOrDefault(v => v.DisplayName == displayName);

        public ViewConfiguration<Shared.Models.Entities.Region> GetDefaultView() => 
            ViewConfigurations.FirstOrDefault() ?? 
            new ViewConfiguration<Shared.Models.Entities.Region>("Default", _queryService?.For<Shared.Models.Entities.Region>() ?? null!);
    }
}