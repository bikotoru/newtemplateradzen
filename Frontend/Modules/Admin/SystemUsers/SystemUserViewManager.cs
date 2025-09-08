using Frontend.Components.Base.Tables;
using Frontend.Services;
using Radzen;

namespace Frontend.Modules.Admin.SystemUsers
{
    public class SystemUserViewManager
    {
        public List<ViewConfiguration<Shared.Models.Entities.SystemEntities.SystemUsers>> ViewConfigurations { get; private set; } = new();
        private readonly QueryService? _queryService;

        public SystemUserViewManager(QueryService? queryService = null)
        {
            _queryService = queryService;
            InitializeDefaultViews();
        }

        private void InitializeDefaultViews()
        {
            ViewConfigurations.Add(new ViewConfiguration<Shared.Models.Entities.SystemEntities.SystemUsers>
            {
                DisplayName = "Vista Completa",
                QueryBuilder = _queryService?.For<Shared.Models.Entities.SystemEntities.SystemUsers>()?
                    .Where(x => x.Active == true)
                    .OrderBy(x => x.Nombre),
                ColumnConfigs = new List<ColumnConfig<Shared.Models.Entities.SystemEntities.SystemUsers>>
                {
                    new ColumnConfig<Shared.Models.Entities.SystemEntities.SystemUsers>
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
                    new ColumnConfig<Shared.Models.Entities.SystemEntities.SystemUsers>
                    {
                        Property = "Descripcion",
                        Title = "Descripción",
                        Width = "300px",
                        Sortable = false,
                        Filterable = true,
                        TextAlign = TextAlign.Left,
                        Visible = true,
                        Order = 2
                    }
                }
            });
        }

        public ViewConfiguration<Shared.Models.Entities.SystemEntities.SystemUsers>? GetViewByName(string displayName) => 
            ViewConfigurations.FirstOrDefault(v => v.DisplayName == displayName);

        public ViewConfiguration<Shared.Models.Entities.SystemEntities.SystemUsers> GetDefaultView() => 
            ViewConfigurations.FirstOrDefault() ?? 
            new ViewConfiguration<Shared.Models.Entities.SystemEntities.SystemUsers>("Default", _queryService?.For<Shared.Models.Entities.SystemEntities.SystemUsers>() ?? null!);
    }
}