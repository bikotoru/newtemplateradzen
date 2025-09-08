using Frontend.Components.Base.Tables;
using Frontend.Services;
using Radzen;

namespace Frontend.Modules.Admin.SystemPermissions
{
    public class SystemPermissionViewManager
    {
        public List<ViewConfiguration<Shared.Models.Entities.SystemEntities.SystemPermissions>> ViewConfigurations { get; private set; } = new();
        private readonly QueryService? _queryService;

        public SystemPermissionViewManager(QueryService? queryService = null)
        {
            _queryService = queryService;
            InitializeDefaultViews();
        }

        private void InitializeDefaultViews()
        {
            ViewConfigurations.Add(new ViewConfiguration<Shared.Models.Entities.SystemEntities.SystemPermissions>
            {
                DisplayName = "Vista Completa",
                QueryBuilder = _queryService?.For<Shared.Models.Entities.SystemEntities.SystemPermissions>()?
                    .Where(x => x.Active == true)
                    .OrderBy(x => x.Nombre),
                ColumnConfigs = new List<ColumnConfig<Shared.Models.Entities.SystemEntities.SystemPermissions>>
                {
                    new ColumnConfig<Shared.Models.Entities.SystemEntities.SystemPermissions>
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
                    new ColumnConfig<Shared.Models.Entities.SystemEntities.SystemPermissions>
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

        public ViewConfiguration<Shared.Models.Entities.SystemEntities.SystemPermissions>? GetViewByName(string displayName) => 
            ViewConfigurations.FirstOrDefault(v => v.DisplayName == displayName);

        public ViewConfiguration<Shared.Models.Entities.SystemEntities.SystemPermissions> GetDefaultView() => 
            ViewConfigurations.FirstOrDefault() ?? 
            new ViewConfiguration<Shared.Models.Entities.SystemEntities.SystemPermissions>("Default", _queryService?.For<Shared.Models.Entities.SystemEntities.SystemPermissions>() ?? null!);
    }
}