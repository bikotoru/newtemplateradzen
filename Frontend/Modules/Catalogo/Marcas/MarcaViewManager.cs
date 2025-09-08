using Frontend.Components.Base.Tables;
using Frontend.Services;
using Radzen;

namespace Frontend.Modules.Catalogo.Marcas
{
    public class MarcaViewManager
    {
        public List<ViewConfiguration<Shared.Models.Entities.Marca>> ViewConfigurations { get; private set; } = new();
        private readonly QueryService? _queryService;

        public MarcaViewManager(QueryService? queryService = null)
        {
            _queryService = queryService;
            InitializeDefaultViews();
        }

        private void InitializeDefaultViews()
        {
            ViewConfigurations.Add(new ViewConfiguration<Shared.Models.Entities.Marca>
            {
                DisplayName = "Vista Completa",
                QueryBuilder = _queryService?.For<Shared.Models.Entities.Marca>()?
                    .Where(x => x.Active == true)
                    .OrderBy(x => x.Nombre),
                ColumnConfigs = new List<ColumnConfig<Shared.Models.Entities.Marca>>
                {
                    new ColumnConfig<Shared.Models.Entities.Marca>
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
                    new ColumnConfig<Shared.Models.Entities.Marca>
                    {
                        Property = "Codigointerno",
                        Title = "Codigointerno",
                        Width = "200px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Left,
                        Visible = true,
                        Order = 2
                    }
                }
            });
        }

        public ViewConfiguration<Shared.Models.Entities.Marca>? GetViewByName(string displayName) => 
            ViewConfigurations.FirstOrDefault(v => v.DisplayName == displayName);

        public ViewConfiguration<Shared.Models.Entities.Marca> GetDefaultView() => 
            ViewConfigurations.FirstOrDefault() ?? 
            new ViewConfiguration<Shared.Models.Entities.Marca>("Default", _queryService?.For<Shared.Models.Entities.Marca>() ?? null!);
    }
}