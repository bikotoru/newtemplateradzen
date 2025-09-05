using Frontend.Components.Base;

namespace Frontend.Layout.Menu.Modules;

public static class PrincipalMenuConfig
{
    public static MenuModule GetMenuModule()
    {
        return new MenuModule
        {
            Text = "📊 Módulo Principal",
            Icon = "dashboard",
            MenuItems = new List<MenuItem>
            {
                new MenuItem
                {
                    Text = "Categorías",
                    Icon = "category",
                    Path = "/categoria/list",
                    Permissions = new List<string>{ "CATEGORIA.VIEWMENU"}
                }
            }
        };
    }
}