using Frontend.Components.Base.Tables;
using Frontend.Services;
using Radzen;

namespace Frontend.Modules.Catalogo.Productos
{
    public class ProductoViewManager
    {
        public List<ViewConfiguration<Shared.Models.Entities.Producto>> ViewConfigurations { get; private set; } = new();
        private readonly QueryService? _queryService;

        public ProductoViewManager(QueryService? queryService = null)
        {
            _queryService = queryService;
            InitializeDefaultViews();
        }

        private void InitializeDefaultViews()
        {
            ViewConfigurations.Add(new ViewConfiguration<Shared.Models.Entities.Producto>
            {
                DisplayName = "Vista Completa",
                QueryBuilder = _queryService?.For<Shared.Models.Entities.Producto>()?
                    .Where(x => x.Active == true)
                    .OrderBy(x => x.Nombre),
                ColumnConfigs = new List<ColumnConfig<Shared.Models.Entities.Producto>>
                {
                    new ColumnConfig<Shared.Models.Entities.Producto>
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
                    new ColumnConfig<Shared.Models.Entities.Producto>
                    {
                        Property = "Codigosku",
                        Title = "Codigosku",
                        Width = "200px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Left,
                        Visible = true,
                        Order = 2
                    },
                    new ColumnConfig<Shared.Models.Entities.Producto>
                    {
                        Property = "Precioventa",
                        Title = "Precioventa",
                        Width = "100px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Left,
                        Visible = true,
                        Order = 3
                    },
                    new ColumnConfig<Shared.Models.Entities.Producto>
                    {
                        Property = "Preciocompra",
                        Title = "Preciocompra",
                        Width = "100px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Left,
                        Visible = true,
                        Order = 4
                    },
                    new ColumnConfig<Shared.Models.Entities.Producto>
                    {
                        Property = "MarcaId",
                        Title = "MarcaId",
                        Width = "120px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Left,
                        Visible = true,
                        Order = 5
                    },
                    new ColumnConfig<Shared.Models.Entities.Producto>
                    {
                        Property = "CategoriaId",
                        Title = "CategoriaId",
                        Width = "120px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Left,
                        Visible = true,
                        Order = 6
                    }
                }
            });
        }

        public ViewConfiguration<Shared.Models.Entities.Producto>? GetViewByName(string displayName) => 
            ViewConfigurations.FirstOrDefault(v => v.DisplayName == displayName);

        public ViewConfiguration<Shared.Models.Entities.Producto> GetDefaultView() => 
            ViewConfigurations.FirstOrDefault() ?? 
            new ViewConfiguration<Shared.Models.Entities.Producto>("Default", _queryService?.For<Shared.Models.Entities.Producto>() ?? null!);
    }
}