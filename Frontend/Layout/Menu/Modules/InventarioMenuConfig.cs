using Frontend.Components.Base;

namespace Frontend.Layout.Menu.Modules;

public static class InventarioMenuConfig
{
    public static MenuModule GetMenuModule()
    {
        return new MenuModule
        {
            Text = "📊 Inventario",
            Icon = "inventory",
            MenuItems = new List<MenuItem>
            {
                new MenuItem
                {
                    Text = "Marca",
                    Icon = "list",
                    Path = "/inventario/core/marca/list",
                    Permissions = new List<string>{ "MARCA.VIEWMENU"}
                }
            }
        };
    }
}