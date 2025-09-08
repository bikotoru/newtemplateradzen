using Frontend.Components.Base.Tables;
using Frontend.Services;
using Radzen;

namespace Frontend.Modules.Admin.SystemRoles
{
    public class SystemRoleViewManager
    {
        public List<ViewConfiguration<Shared.Models.Entities.SystemEntities.SystemRoles>> ViewConfigurations { get; private set; } = new();
        private readonly QueryService? _queryService;

        public SystemRoleViewManager(QueryService? queryService = null)
        {
            _queryService = queryService;
            InitializeDefaultViews();
        }

        private void InitializeDefaultViews()
        {
            ViewConfigurations.Add(new ViewConfiguration<Shared.Models.Entities.SystemEntities.SystemRoles>
            {
                DisplayName = "Vista Completa",
                QueryBuilder = _queryService?.For<Shared.Models.Entities.SystemEntities.SystemRoles>()?
                    .Where(x => x.Active == true)
                    .OrderBy(x => x.Nombre),
                ColumnConfigs = new List<ColumnConfig<Shared.Models.Entities.SystemEntities.SystemRoles>>
                {
                    new ColumnConfig<Shared.Models.Entities.SystemEntities.SystemRoles>
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
                    new ColumnConfig<Shared.Models.Entities.SystemEntities.SystemRoles>
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

        public ViewConfiguration<Shared.Models.Entities.SystemEntities.SystemRoles>? GetViewByName(string displayName) => 
            ViewConfigurations.FirstOrDefault(v => v.DisplayName == displayName);

        public ViewConfiguration<Shared.Models.Entities.SystemEntities.SystemRoles> GetDefaultView() => 
            ViewConfigurations.FirstOrDefault() ?? 
            new ViewConfiguration<Shared.Models.Entities.SystemEntities.SystemRoles>("Default", _queryService?.For<Shared.Models.Entities.SystemEntities.SystemRoles>() ?? null!);
    }
}