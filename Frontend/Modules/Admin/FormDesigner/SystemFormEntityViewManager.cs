using Frontend.Components.Base.Tables;
using Frontend.Services;
using Radzen;
using Shared.Models.Entities.SystemEntities;

namespace Frontend.Modules.Admin.FormDesigner
{
    public class SystemFormEntitiesViewManager
    {
        public List<ViewConfiguration<SystemFormEntities>> ViewConfigurations { get; private set; } = new();
        private readonly QueryService? _queryService;

        public SystemFormEntitiesViewManager(QueryService? queryService = null)
        {
            _queryService = queryService;
            InitializeDefaultViews();
        }

        private void InitializeDefaultViews()
        {
            ViewConfigurations.Add(new ViewConfiguration<SystemFormEntities>
            {
                DisplayName = "Vista Completa",
                QueryBuilder = _queryService?.For<SystemFormEntities>()?
                    .Where(x => x.Active == true)
                    .OrderBy(x => x.DisplayName),
                ColumnConfigs = new List<ColumnConfig<SystemFormEntities>>
                {
                    new ColumnConfig<SystemFormEntities>
                    {
                        Property = "DisplayName",
                        Title = "Entidad",
                        Width = "200px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Left,
                        Visible = true,
                        Order = 1
                    },
                    new ColumnConfig<SystemFormEntities>
                    {
                        Property = "EntityName",
                        Title = "Nombre Técnico",
                        Width = "150px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Left,
                        Visible = true,
                        Order = 2
                    },
                    new ColumnConfig<SystemFormEntities>
                    {
                        Property = "Category",
                        Title = "Categoría",
                        Width = "120px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Center,
                        Visible = true,
                        Order = 3,
                        Template = entity => builder =>
                        {
                            var badgeClass = GetCategoryBadgeClass(entity.Category);
                            builder.OpenElement(0, "span");
                            builder.AddAttribute(1, "class", $"badge {badgeClass}");
                            builder.AddContent(2, entity.Category ?? "Sin categoría");
                            builder.CloseElement();
                        }
                    },
                    new ColumnConfig<SystemFormEntities>
                    {
                        Property = "Description",
                        Title = "Descripción",
                        Width = "250px",
                        Sortable = false,
                        Filterable = true,
                        TextAlign = TextAlign.Left,
                        Visible = true,
                        Order = 4,
                        FormatExpression = e => string.IsNullOrEmpty(e.Description) ? "Sin descripción" : e.Description
                    },
                    new ColumnConfig<SystemFormEntities>
                    {
                        Property = "AllowCustomFields",
                        Title = "Campos Personalizados",
                        Width = "140px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Center,
                        Visible = true,
                        Order = 5,
                        Template = entity => builder =>
                        {
                            builder.OpenElement(0, "span");
                            builder.AddAttribute(1, "class",
                                $"badge {(entity.AllowCustomFields ? "badge-success" : "badge-secondary")}");
                            builder.AddContent(2, entity.AllowCustomFields ? "Sí" : "No");
                            builder.CloseElement();
                        }
                    },
                    new ColumnConfig<SystemFormEntities>
                    {
                        Property = "OrganizationId",
                        Title = "Ámbito",
                        Width = "100px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Center,
                        Visible = true,
                        Order = 6,
                        Template = entity => builder =>
                        {
                            builder.OpenElement(0, "span");
                            builder.AddAttribute(1, "class",
                                $"badge {(entity.OrganizationId == null ? "badge-info" : "badge-primary")}");
                            builder.AddContent(2, entity.OrganizationId == null ? "Sistema" : "Org");
                            builder.CloseElement();
                        }
                    },
                    new ColumnConfig<SystemFormEntities>
                    {
                        Property = "IconName",
                        Title = "Icono",
                        Width = "80px",
                        Sortable = false,
                        Filterable = false,
                        TextAlign = TextAlign.Center,
                        Visible = true,
                        Order = 7,
                        Template = entity => builder =>
                        {
                            builder.OpenElement(0, "i");
                            builder.AddAttribute(1, "class", "material-icons");
                            builder.AddAttribute(2, "style", "font-size: 1.5rem; color: var(--rz-primary);");
                            builder.AddContent(3, entity.IconName ?? "table_view");
                            builder.CloseElement();
                        }
                    },
                    new ColumnConfig<SystemFormEntities>
                    {
                        Property = "FechaCreacion",
                        Title = "Fecha Creación",
                        Width = "140px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Center,
                        Visible = false,
                        Order = 8,
                        FormatExpression = e => e.FechaCreacion.ToString("dd/MM/yyyy")
                    }
                }
            });

            // Vista compacta para pantallas pequeñas
            ViewConfigurations.Add(new ViewConfiguration<SystemFormEntities>
            {
                DisplayName = "Vista Compacta",
                QueryBuilder = _queryService?.For<SystemFormEntities>()?
                    .Where(x => x.Active == true)
                    .OrderBy(x => x.DisplayName),
                ColumnConfigs = new List<ColumnConfig<SystemFormEntities>>
                {
                    new ColumnConfig<SystemFormEntities>
                    {
                        Property = "IconName",
                        Title = "",
                        Width = "60px",
                        Sortable = false,
                        Filterable = false,
                        TextAlign = TextAlign.Center,
                        Visible = true,
                        Order = 1,
                        Template = entity => builder =>
                        {
                            builder.OpenElement(0, "i");
                            builder.AddAttribute(1, "class", "material-icons");
                            builder.AddAttribute(2, "style", "font-size: 1.5rem; color: var(--rz-primary);");
                            builder.AddContent(3, entity.IconName ?? "table_view");
                            builder.CloseElement();
                        }
                    },
                    new ColumnConfig<SystemFormEntities>
                    {
                        Property = "DisplayName",
                        Title = "Entidad",
                        Width = "200px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Left,
                        Visible = true,
                        Order = 2
                    },
                    new ColumnConfig<SystemFormEntities>
                    {
                        Property = "Category",
                        Title = "Categoría",
                        Width = "120px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Center,
                        Visible = true,
                        Order = 3,
                        Template = entity => builder =>
                        {
                            var badgeClass = GetCategoryBadgeClass(entity.Category);
                            builder.OpenElement(0, "span");
                            builder.AddAttribute(1, "class", $"badge {badgeClass}");
                            builder.AddContent(2, entity.Category ?? "Sin categoría");
                            builder.CloseElement();
                        }
                    },
                    new ColumnConfig<SystemFormEntities>
                    {
                        Property = "AllowCustomFields",
                        Title = "Personalizable",
                        Width = "120px",
                        Sortable = true,
                        Filterable = true,
                        TextAlign = TextAlign.Center,
                        Visible = true,
                        Order = 4,
                        Template = entity => builder =>
                        {
                            builder.OpenElement(0, "span");
                            builder.AddAttribute(1, "class",
                                $"badge {(entity.AllowCustomFields ? "badge-success" : "badge-secondary")}");
                            builder.AddContent(2, entity.AllowCustomFields ? "Sí" : "No");
                            builder.CloseElement();
                        }
                    }
                }
            });

            // Vista solo de sistema (entidades globales)
            ViewConfigurations.Add(new ViewConfiguration<SystemFormEntities>
            {
                DisplayName = "Solo Sistema",
                QueryBuilder = _queryService?.For<SystemFormEntities>()?
                    .Where(x => x.Active == true && x.OrganizationId == null)
                    .OrderBy(x => x.Category),
                ColumnConfigs = ViewConfigurations.First().ColumnConfigs // Reutilizar columnas de vista completa
            });
        }

        private static string GetCategoryBadgeClass(string? category)
        {
            return category?.ToLower() switch
            {
                "sistema" or "system" => "badge-info",
                "core" => "badge-primary",
                "rrhh" or "hr" => "badge-success",
                "ventas" or "sales" => "badge-warning",
                "inventario" or "inventory" => "badge-secondary",
                "localidades" or "locations" => "badge-light",
                "testing" => "badge-danger",
                _ => "badge-light"
            };
        }

        public ViewConfiguration<SystemFormEntities>? GetViewByName(string displayName) =>
            ViewConfigurations.FirstOrDefault(v => v.DisplayName == displayName);

        public ViewConfiguration<SystemFormEntities> GetDefaultView() =>
            ViewConfigurations.FirstOrDefault() ??
            new ViewConfiguration<SystemFormEntities>("Default", _queryService?.For<SystemFormEntities>() ?? null!);
    }
}