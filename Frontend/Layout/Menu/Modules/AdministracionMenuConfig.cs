using Frontend.Components.Base;

namespace Frontend.Layout.Menu.Modules;

public static class AdministracionMenuConfig
{
    public static MenuModule GetMenuModule()
    {
        return new MenuModule
        {
            Text = "⚙️ Administración",
            Icon = "settings",
            MenuItems = new List<MenuItem>
            {
                new MenuItem
                {
                    Text = "Usuarios",
                    Icon = "account_box",
                    Path = "/admin/users",
                    Permissions = new List<string> { "Admin", "SuperAdmin" }
                },
                new MenuItem
                {
                    Text = "Roles",
                    Icon = "admin_panel_settings",
                    Path = "/admin/roles",
                    Permissions = new List<string> { "Admin", "SuperAdmin" }
                },
                new MenuItem
                {
                    Text = "Permisos",
                    Icon = "security",
                    Path = "/admin/permissions",
                    Permissions = new List<string> { "Admin", "SuperAdmin" }
                }
            }
        };
    }
}