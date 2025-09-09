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
                    .Include(x => x.Organization)
                    .Include(x => x.Creador)
                    .OrderBy(x => x.Nombre),
                ColumnConfigs = new List<ColumnConfig<Shared.Models.Entities.SystemEntities.SystemRoles>>
                {
                    new ColumnConfig<Shared.Models.Entities.SystemEntities.SystemRoles>
                    {
                        Property = "Nombre",
                        Title = "Nombre del Rol",
                        Width = "200px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Left,
                        Visible = true,
                        Order = 1
                    },
                    new ColumnConfig<Shared.Models.Entities.SystemEntities.SystemRoles>
                    {
                        Property = "TypeRole",
                        Title = "Tipo",
                        Width = "120px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Center,
                        Visible = true,
                        Order = 2,
                        Template = role => builder =>
                        {
                            builder.OpenElement(0, "span");
                            builder.AddAttribute(1, "class", 
                                $"badge {(role.TypeRole == "Admin" ? "badge-danger" : "badge-primary")}");
                            builder.AddContent(2, role.TypeRole);
                            builder.CloseElement();
                        }
                    },
                    new ColumnConfig<Shared.Models.Entities.SystemEntities.SystemRoles>
                    {
                        Property = "Organization.Nombre",
                        Title = "Organización",
                        Width = "180px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Left,
                        Visible = true,
                        Order = 3,
                        FormatExpression = r => r.Organization?.Nombre ?? "Global"
                    },
                    new ColumnConfig<Shared.Models.Entities.SystemEntities.SystemRoles>
                    {
                        Property = "Descripcion",
                        Title = "Descripción",
                        Width = "250px",
                        Sortable = false,
                        Filterable = true,
                        TextAlign = TextAlign.Left,
                        Visible = true,
                        Order = 4,
                        FormatExpression = r => string.IsNullOrEmpty(r.Descripcion) ? "Sin descripción" : r.Descripcion
                    },
                    new ColumnConfig<Shared.Models.Entities.SystemEntities.SystemRoles>
                    {
                        Property = "Active",
                        Title = "Estado",
                        Width = "100px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Center,
                        Visible = true,
                        Order = 5,
                        Template = role => builder =>
                        {
                            builder.OpenElement(0, "span");
                            builder.AddAttribute(1, "class", 
                                $"badge {(role.Active ? "badge-success" : "badge-secondary")}");
                            builder.AddContent(2, role.Active ? "Activo" : "Inactivo");
                            builder.CloseElement();
                        }
                    },
                    new ColumnConfig<Shared.Models.Entities.SystemEntities.SystemRoles>
                    {
                        Property = "FechaCreacion",
                        Title = "Fecha Creación",
                        Width = "140px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Center,
                        Visible = true,
                        Order = 6,
                        FormatExpression = r => r.FechaCreacion.ToString("dd/MM/yyyy")
                    },
                    new ColumnConfig<Shared.Models.Entities.SystemEntities.SystemRoles>
                    {
                        Property = "Creador.Nombre",
                        Title = "Creado por",
                        Width = "150px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Left,
                        Visible = false, // Oculta por defecto pero disponible
                        Order = 7,
                        FormatExpression = r => r.Creador?.Nombre ?? "Sistema"
                    }
                }
            });

            // Vista simplificada para pantallas pequeñas
            ViewConfigurations.Add(new ViewConfiguration<Shared.Models.Entities.SystemEntities.SystemRoles>
            {
                DisplayName = "Vista Compacta",
                QueryBuilder = _queryService?.For<Shared.Models.Entities.SystemEntities.SystemRoles>()?
                    .Where(x => x.Active == true)
                    .Include(x => x.Organization)
                    .OrderBy(x => x.Nombre),
                ColumnConfigs = new List<ColumnConfig<Shared.Models.Entities.SystemEntities.SystemRoles>>
                {
                    new ColumnConfig<Shared.Models.Entities.SystemEntities.SystemRoles>
                    {
                        Property = "Nombre",
                        Title = "Rol",
                        Width = "250px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Left,
                        Visible = true,
                        Order = 1
                    },
                    new ColumnConfig<Shared.Models.Entities.SystemEntities.SystemRoles>
                    {
                        Property = "TypeRole",
                        Title = "Tipo",
                        Width = "100px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Center,
                        Visible = true,
                        Order = 2,
                        Template = role => builder =>
                        {
                            builder.OpenElement(0, "span");
                            builder.AddAttribute(1, "class", 
                                $"badge {(role.TypeRole == "Admin" ? "badge-danger" : "badge-primary")}");
                            builder.AddContent(2, role.TypeRole);
                            builder.CloseElement();
                        }
                    },
                    new ColumnConfig<Shared.Models.Entities.SystemEntities.SystemRoles>
                    {
                        Property = "Organization.Nombre",
                        Title = "Organización",
                        Width = "150px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Left,
                        Visible = true,
                        Order = 3,
                        FormatExpression = r => r.Organization?.Nombre ?? "Global"
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