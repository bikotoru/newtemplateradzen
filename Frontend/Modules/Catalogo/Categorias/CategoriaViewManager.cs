using Frontend.Components.Base.Tables;
using Frontend.Services;
using Radzen;

namespace Frontend.Modules.Catalogo.Categorias
{
    public class CategoriaViewManager
    {
        public List<ViewConfiguration<Shared.Models.Entities.Categoria>> ViewConfigurations { get; private set; } = new();
        private readonly QueryService? _queryService;

        public CategoriaViewManager(QueryService? queryService = null)
        {
            _queryService = queryService;
            InitializeDefaultViews();
        }

        private void InitializeDefaultViews()
        {
            ViewConfigurations.Add(new ViewConfiguration<Shared.Models.Entities.Categoria>
            {
                DisplayName = "Vista Completa",
                QueryBuilder = _queryService?.For<Shared.Models.Entities.Categoria>()?
                    .Where(x => x.Active == true)
                    .OrderBy(x => x.Nombre),
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

        public ViewConfiguration<Shared.Models.Entities.Categoria>? GetViewByName(string displayName) => 
            ViewConfigurations.FirstOrDefault(v => v.DisplayName == displayName);

        public ViewConfiguration<Shared.Models.Entities.Categoria> GetDefaultView() => 
            ViewConfigurations.FirstOrDefault() ?? 
            new ViewConfiguration<Shared.Models.Entities.Categoria>("Default", _queryService?.For<Shared.Models.Entities.Categoria>() ?? null!);
    }
}