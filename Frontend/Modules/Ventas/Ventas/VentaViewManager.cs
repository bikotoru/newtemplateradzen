using Frontend.Components.Base.Tables;
using Frontend.Services;
using Radzen;

namespace Frontend.Modules.Ventas.Ventas
{
    public class VentaViewManager
    {
        public List<ViewConfiguration<Shared.Models.Entities.Venta>> ViewConfigurations { get; private set; } = new();
        private readonly QueryService? _queryService;

        public VentaViewManager(QueryService? queryService = null)
        {
            _queryService = queryService;
            InitializeDefaultViews();
        }

        private void InitializeDefaultViews()
        {
            ViewConfigurations.Add(new ViewConfiguration<Shared.Models.Entities.Venta>
            {
                DisplayName = "Vista Completa",
                QueryBuilder = _queryService?.For<Shared.Models.Entities.Venta>()?
                    .Where(x => x.Active == true)
                    .OrderBy(x => x.Numventa),
                ColumnConfigs = new List<ColumnConfig<Shared.Models.Entities.Venta>>
                {
                    new ColumnConfig<Shared.Models.Entities.Venta>
                    {
                        Property = "Numventa",
                        Title = "Numventa",
                        Width = "100px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Left,
                        Visible = true,
                        Order = 1
                    },
                    new ColumnConfig<Shared.Models.Entities.Venta>
                    {
                        Property = "Montototal",
                        Title = "Montototal",
                        Width = "100px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Left,
                        Visible = true,
                        Order = 2
                    }
                }
            });
        }

        public ViewConfiguration<Shared.Models.Entities.Venta>? GetViewByName(string displayName) => 
            ViewConfigurations.FirstOrDefault(v => v.DisplayName == displayName);

        public ViewConfiguration<Shared.Models.Entities.Venta> GetDefaultView() => 
            ViewConfigurations.FirstOrDefault() ?? 
            new ViewConfiguration<Shared.Models.Entities.Venta>("Default", _queryService?.For<Shared.Models.Entities.Venta>() ?? null!);
    }
}